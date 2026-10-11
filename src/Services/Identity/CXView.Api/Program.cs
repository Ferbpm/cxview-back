using System.Security.Claims;
using System.Security.Cryptography;
using CXView.Application;
using CXView.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();
DevelopmentKeyBootstrap.EnsureKeys(builder.Environment, builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CXView Security API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", In = ParameterLocation.Header, Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", Description = "Ingrese el JWT sin prefijos adicionales."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOptions<JwtOptions>().BindConfiguration("Jwt")
    .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer) && !string.IsNullOrWhiteSpace(x.Audience) && !string.IsNullOrWhiteSpace(x.PrivateKeyPath) && !string.IsNullOrWhiteSpace(x.PublicKeyPath), "JWT configuration is incomplete.").ValidateOnStart();
builder.Services.AddOptions<OtpOptions>().BindConfiguration("Otp").Validate(x => Convert.TryFromBase64String(x.PepperBase64, new Span<byte>(new byte[128]), out _), "Otp pepper must be Base64.").ValidateOnStart();
builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.Section)
    .Validate(x => x.Provider.Equals("InMemory",StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(x.ConnectionString), "A connection string is required for relational providers.").ValidateOnStart();
builder.Services.AddOptions<EmailOptions>().BindConfiguration("Email")
    .Validate(x => x.Provider.Equals("DevelopmentLog",StringComparison.OrdinalIgnoreCase) ||
        (!string.IsNullOrWhiteSpace(x.Host) && x.Port is > 0 and <= 65535 &&
         !string.IsNullOrWhiteSpace(x.UserName) && !string.IsNullOrWhiteSpace(x.Password) &&
         !string.IsNullOrWhiteSpace(x.FromAddress)),
        "La configuración SMTP está incompleta.").ValidateOnStart();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IOtpHasher, HmacOtpHasher>();
builder.Services.AddSingleton<ITokenIssuer, RsaJwtIssuer>();
var emailProvider = builder.Configuration["Email:Provider"] ?? "Smtp";
if (emailProvider.Equals("DevelopmentLog",StringComparison.OrdinalIgnoreCase))
{
    if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("DevelopmentLog solo puede usarse en Development.");
    builder.Services.AddSingleton<IOtpDelivery, DevelopmentOtpDelivery>();
}
else builder.Services.AddSingleton<IOtpDelivery, SmtpOtpDelivery>();
var databaseProvider = builder.Configuration["Database:Provider"] ?? "InMemory";
if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<InMemorySecurityStore>();
    builder.Services.AddSingleton<IUserRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IFinePermissionRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IFinePermissionRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<ISessionRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IOtpRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<ISecurityAdminRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IUserSiteRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IUserOperationTypeRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IUserBusinessUnitRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IUserModuleRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IUserMenuRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IUserSubmenuRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IUserNavigationAssignmentRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IUserAccessRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IFormOptionMasterRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IUserProfileRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<ICompanyRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<IIntegratedLogisticsRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<ISecurityMasterRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
    builder.Services.AddSingleton<ISecurityMasterUserRepository>(sp => sp.GetRequiredService<InMemorySecurityStore>());
}
else
{
    builder.Services.AddSingleton<IDbConnectionFactory, ConfiguredDbConnectionFactory>();
    builder.Services.AddScoped<DapperSecurityStore>();
    builder.Services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IFinePermissionRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IFinePermissionRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<ISessionRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IOtpRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<ISecurityAdminRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IUserSiteRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IUserOperationTypeRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IUserBusinessUnitRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IUserModuleRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IUserMenuRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IUserSubmenuRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IUserNavigationAssignmentRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IUserAccessRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IFormOptionMasterRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IUserProfileRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<ICompanyRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<IIntegratedLogisticsRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<ISecurityMasterRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
    builder.Services.AddScoped<ISecurityMasterUserRepository>(sp => sp.GetRequiredService<DapperSecurityStore>());
}
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SecurityAdminService>();
builder.Services.AddScoped<UserSiteService>();
builder.Services.AddScoped<UserOperationTypeService>();
builder.Services.AddScoped<UserBusinessUnitService>();
builder.Services.AddScoped<UserModuleService>();
builder.Services.AddScoped<UserMenuService>();
builder.Services.AddScoped<UserSubmenuService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<IntegratedLogisticsService>();
builder.Services.AddScoped<SecurityMasterService>();
builder.Services.AddScoped<SecurityMasterUserService>();
builder.Services.AddScoped<UserNavigationAssignmentService>();
builder.Services.AddScoped<UserAccessService>();
builder.Services.AddScoped<FormOptionMasterService>();
builder.Services.AddScoped<UserProfileService>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is required.");
var publicRsa = RSA.Create();
publicRsa.ImportFromPem(File.ReadAllText(jwt.PublicKeyPath));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Issuer, ValidAudience = jwt.Audience, ValidateIssuer = true, ValidateAudience = true,
        ValidateLifetime = true, ValidateIssuerSigningKey = true, RequireExpirationTime = true, RequireSignedTokens = true,
        IssuerSigningKey = new RsaSecurityKey(publicRsa), ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Token) && context.Request.Cookies.TryGetValue("cxview_session", out var token)) context.Token = token;
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy("users.read", p => p.RequireClaim("permission", "users.read"))
    .AddPolicy("users.write", p => p.RequireClaim("permission", "users.write"));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(context.User.FindFirstValue("sub") ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("refresh", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue("sub") ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 1, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddHealthChecks();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (error is ArgumentException)
    {
        await Results.BadRequest(new { message = error.Message }).ExecuteAsync(context);
        return;
    }

    await Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Ocurrió un error inesperado.").ExecuteAsync(context);
}));
app.UseMiddleware<ValidationExceptionMiddleware>();
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<SessionValidationMiddleware>();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

var auth = app.MapGroup("/api/v1/auth").AllowAnonymous().RequireRateLimiting("auth");
auth.MapPost("/login", (LoginRequest request, AuthService service, CancellationToken ct) => service.LoginAsync(request, ct));
auth.MapPost("/verify-otp", async (VerifyOtpRequest request, HttpContext context, AuthService service, CancellationToken ct) =>
{
    var result = await service.VerifyOtpAsync(request, ct);
    if (result is null) return Results.Unauthorized();
    SetSessionCookie(context, result);
    return Results.Ok(new { result.ExpiresAt });
});
app.MapPost("/api/v1/auth/refresh", async (ClaimsPrincipal principal, HttpContext context, AuthService service, CancellationToken ct) =>
{
    if (!Guid.TryParse(principal.FindFirstValue("sid"), out var sid) || !Guid.TryParse(principal.FindFirstValue("sub"), out var uid)) return Results.Unauthorized();
    var result = await service.RefreshAsync(uid, sid, ct);
    if (result is null) return Results.Forbid();
    SetSessionCookie(context, result);
    return Results.Ok(new { result.ExpiresAt });
}).RequireAuthorization().RequireRateLimiting("refresh");
app.MapPost("/api/v1/auth/logout", async (ClaimsPrincipal principal, ISessionRepository sessions, CancellationToken ct) =>
{
    if (!Guid.TryParse(principal.FindFirstValue("sid"), out var sid) || !Guid.TryParse(principal.FindFirstValue("sub"), out var uid)) return Results.Unauthorized();
    await sessions.RevokeAsync(sid, uid, ct); return Results.NoContent();
});

var usersApi = app.MapGroup("/api/v1/security/users");
usersApi.MapGet("/", [Authorize(Policy="users.read")] (SecurityAdminService service,CancellationToken ct) => service.GetUsersAsync(ct));
usersApi.MapGet("/{userName}/sites", async (string userName, ClaimsPrincipal principal, UserSiteService service, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(userName) || userName.Trim().Length > 20)
        return Results.BadRequest(new { message = "El nombre de usuario es obligatorio y no puede superar 20 caracteres." });

    var normalizedUserName = userName.Trim();
    var authenticatedUserName = principal.FindFirstValue("unique_name");
    var canReadAnyUser = principal.HasClaim("permission", "users.read");
    if (!canReadAnyUser && !string.Equals(authenticatedUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();

    return Results.Ok(await service.GetByUserNameAsync(normalizedUserName, ct));
}).RequireAuthorization();
usersApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateUserRequest request,SecurityAdminService service,CancellationToken ct) => Results.Created($"/api/v1/security/users/{await service.CreateUserAsync(request,ct)}",null));
usersApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id,UpdateUserRequest request,SecurityAdminService service,CancellationToken ct) => await service.UpdateUserAsync(id,request,ct)?Results.NoContent():Results.NotFound());
usersApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id,SecurityAdminService service,CancellationToken ct) => await service.DeactivateUserAsync(id,ct)?Results.NoContent():Results.NotFound());
usersApi.MapPut("/{id:guid}/profile", [Authorize(Policy="users.write")] async (Guid id,AssignProfileRequest request,SecurityAdminService service,CancellationToken ct) => { await service.AssignProfileAsync(id,request,ct); return Results.NoContent(); });
app.MapGet("/api/v1/security/operation-types", async (string userName, ClaimsPrincipal principal, UserOperationTypeService service, CancellationToken ct) =>
{
    var normalizedUserName = userName.Trim();
    if (normalizedUserName.Length is 0 or > 20)
        return Results.BadRequest(new { message = "El nombre de usuario es obligatorio y no puede superar 20 caracteres." });

    var authenticatedUserName = principal.FindFirstValue("unique_name");
    var canReadAnyUser = principal.HasClaim("permission", "users.read");
    if (!canReadAnyUser && !string.Equals(authenticatedUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();

    return Results.Ok(await service.GetByUserNameAsync(normalizedUserName, ct));
}).RequireAuthorization();
app.MapGet("/api/v1/security/business-units", async (string userName, ClaimsPrincipal principal, UserBusinessUnitService service, CancellationToken ct) =>
{
    var normalizedUserName = userName.Trim();
    if (normalizedUserName.Length is 0 or > 20)
        return Results.BadRequest(new { message = "El nombre de usuario es obligatorio y no puede superar 20 caracteres." });

    var authenticatedUserName = principal.FindFirstValue("unique_name");
    var canReadAnyUser = principal.HasClaim("permission", "users.read");
    if (!canReadAnyUser && !string.Equals(authenticatedUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();

    return Results.Ok(await service.GetByUserNameAsync(normalizedUserName, ct));
}).RequireAuthorization();
app.MapGet("/api/v1/security/modules", async (string userName, ClaimsPrincipal principal, UserModuleService service, CancellationToken ct) =>
{
    var normalizedUserName = userName.Trim();
    if (normalizedUserName.Length is 0 or > 20)
        return Results.BadRequest(new { message = "El nombre de usuario es obligatorio y no puede superar 20 caracteres." });

    var authenticatedUserName = principal.FindFirstValue("unique_name");
    var canReadAnyUser = principal.HasClaim("permission", "users.read");
    if (!canReadAnyUser && !string.Equals(authenticatedUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();

    return Results.Ok(await service.GetByUserNameAsync(normalizedUserName, ct));
}).RequireAuthorization();
app.MapGet("/api/v1/security/menus", async (string userName, string idMod, ClaimsPrincipal principal, UserMenuService service, CancellationToken ct) =>
{
    var normalizedUserName = userName.Trim();
    var normalizedModuleId = idMod.Trim();
    if (normalizedUserName.Length is 0 or > 20)
        return Results.BadRequest(new { message = "El nombre de usuario es obligatorio y no puede superar 20 caracteres." });
    if (!Guid.TryParse(normalizedModuleId, out _))
        return Results.BadRequest(new { message = "El identificador del módulo no es válido." });

    var authenticatedUserName = principal.FindFirstValue("unique_name");
    var canReadAnyUser = principal.HasClaim("permission", "users.read");
    if (!canReadAnyUser && !string.Equals(authenticatedUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();

    return Results.Ok(await service.GetByUserNameAndModuleIdAsync(normalizedUserName, normalizedModuleId, ct));
}).RequireAuthorization();
app.MapGet("/api/v1/security/submenus", async (string userName, string idMenu, ClaimsPrincipal principal, UserSubmenuService service, CancellationToken ct) =>
{
    var normalizedUserName = userName.Trim();
    var normalizedMenuId = idMenu.Trim();
    if (normalizedUserName.Length is 0 or > 20)
        return Results.BadRequest(new { message = "El nombre de usuario es obligatorio y no puede superar 20 caracteres." });
    if (!Guid.TryParse(normalizedMenuId, out _))
        return Results.BadRequest(new { message = "El identificador del menú no es válido." });

    var authenticatedUserName = principal.FindFirstValue("unique_name");
    var canReadAnyUser = principal.HasClaim("permission", "users.read");
    if (!canReadAnyUser && !string.Equals(authenticatedUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();

    return Results.Ok(await service.GetByUserNameAndMenuIdAsync(normalizedUserName, normalizedMenuId, ct));
}).RequireAuthorization();
var companiesApi = app.MapGroup("/api/v1/security/companies");
companiesApi.MapGet("/", [Authorize(Policy="users.read")] (string? idEmpresa, string? empresa, string? ruc, string? estado, CompanyService service, CancellationToken ct) =>
    service.SearchAsync(new CompanySearchFilter(idEmpresa, empresa, ruc, estado), ct)).RequireFinePermission("company.filter");
companiesApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateCompanyRequest request, ClaimsPrincipal principal, CompanyService service, CancellationToken ct) =>
{
    var company = await service.CreateAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/security/companies/{company.Id}", company);
}).RequireFinePermission("company.create");
companiesApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdateCompanyRequest request, ClaimsPrincipal principal, CompanyService service, CancellationToken ct) =>
{
    var company = await service.UpdateAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return company is null ? Results.NotFound() : Results.Ok(company);
}).RequireFinePermission("company.update");
companiesApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, CompanyService service, CancellationToken ct) =>
    await service.DeactivateAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("company.delete");

var integratedLogisticsProductsApi = app.MapGroup("/api/v1/integrated-logistics/products");
integratedLogisticsProductsApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? name, string? status, IntegratedLogisticsService service, CancellationToken ct) =>
    service.SearchProductsAsync(new ProductFilter(id, name, status), ct)).RequireFinePermission("products.filter");
integratedLogisticsProductsApi.MapGet("/active", [Authorize(Policy="users.read")] (IntegratedLogisticsService service, CancellationToken ct) =>
    service.ListActiveProductsAsync(ct)).RequireFinePermission("campaigns.filter");
integratedLogisticsProductsApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateProductRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
{
    var item = await service.CreateProductAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/integrated-logistics/products/{item.Id}", item);
}).RequireFinePermission("products.create");
integratedLogisticsProductsApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdateProductRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.UpdateProductAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("products.update");
integratedLogisticsProductsApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.DeactivateProductAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("products.delete");

var integratedLogisticsCampaignsApi = app.MapGroup("/api/v1/integrated-logistics/campaigns");
integratedLogisticsCampaignsApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? ubigeoId, string? productId, string? process, string? status, IntegratedLogisticsService service, CancellationToken ct) =>
    service.SearchCampaignsAsync(new CampaignFilter(id, ubigeoId, productId, process, status), ct)).RequireFinePermission("campaigns.filter");
integratedLogisticsCampaignsApi.MapGet("/ubigeos/active", [Authorize(Policy="users.read")] (IntegratedLogisticsService service, CancellationToken ct) =>
    service.ListActiveUbigeosAsync(ct)).RequireFinePermission("campaigns.filter");
integratedLogisticsCampaignsApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateCampaignRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
{
    var item = await service.CreateCampaignAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/integrated-logistics/campaigns/{item.Id}", item);
}).RequireFinePermission("campaigns.create");
integratedLogisticsCampaignsApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdateCampaignRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.UpdateCampaignAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("campaigns.update");
integratedLogisticsCampaignsApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.DeactivateCampaignAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("campaigns.delete");

var integratedLogisticsFixedCostsApi = app.MapGroup("/api/v1/integrated-logistics/fixed-costs");
integratedLogisticsFixedCostsApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? siteId, string? description, string? status, IntegratedLogisticsService service, CancellationToken ct) =>
    service.SearchFixedCostsAsync(new FixedCostFilter(id, siteId, description, status), ct)).RequireFinePermission("fixed-costs.filter");
integratedLogisticsFixedCostsApi.MapGet("/sites/active", [Authorize(Policy="users.read")] (IntegratedLogisticsService service, CancellationToken ct) =>
    service.ListActiveSitesAsync(ct)).RequireFinePermission("fixed-costs.filter");
integratedLogisticsFixedCostsApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateFixedCostRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
{
    var item = await service.CreateFixedCostAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/integrated-logistics/fixed-costs/{item.Id}", item);
}).RequireFinePermission("fixed-costs.create");
integratedLogisticsFixedCostsApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdateFixedCostRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.UpdateFixedCostAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("fixed-costs.update");
integratedLogisticsFixedCostsApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.DeactivateFixedCostAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("fixed-costs.delete");

var integratedLogisticsDepositsApi = app.MapGroup("/api/v1/integrated-logistics/deposits");
integratedLogisticsDepositsApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? depositTypeId, string? name, string? status, IntegratedLogisticsService service, CancellationToken ct) =>
    service.SearchDepositsAsync(new DepositFilter(id, depositTypeId, name, status), ct)).RequireFinePermission("deposits.filter");
integratedLogisticsDepositsApi.MapGet("/types/active", [Authorize(Policy="users.read")] (IntegratedLogisticsService service, CancellationToken ct) =>
    service.ListActiveDepositTypesAsync(ct)).RequireFinePermission("deposits.filter");
integratedLogisticsDepositsApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateDepositRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
{
    var item = await service.CreateDepositAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/integrated-logistics/deposits/{item.Id}", item);
}).RequireFinePermission("deposits.create");
integratedLogisticsDepositsApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdateDepositRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.UpdateDepositAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("deposits.update");
integratedLogisticsDepositsApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.DeactivateDepositAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("deposits.delete");

var containerDimensionsApi = app.MapGroup("/api/v1/master-tables/container-dimensions");
containerDimensionsApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? name, string? status, IntegratedLogisticsService service, CancellationToken ct) => service.SearchContainerDimensionsAsync(new MasterTableFilter(id, name, status), ct)).RequireFinePermission("container-dim.filter");
containerDimensionsApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateContainerDimensionRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
{
    var item = await service.CreateContainerDimensionAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/master-tables/container-dimensions/{item.Id}", item);
}).RequireFinePermission("container-dim.create");
containerDimensionsApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdateContainerDimensionRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.UpdateContainerDimensionAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("container-dim.update");
containerDimensionsApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.DeactivateContainerDimensionAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("container-dim.delete");

var entityAddressTypesApi = app.MapGroup("/api/v1/master-tables/entity-address-types");
entityAddressTypesApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? name, string? status, IntegratedLogisticsService service, CancellationToken ct) => service.SearchEntityAddressTypesAsync(new MasterTableFilter(id, name, status), ct)).RequireFinePermission("address-types.filter");
entityAddressTypesApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateEntityAddressTypeRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
{
    var item = await service.CreateEntityAddressTypeAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/master-tables/entity-address-types/{item.Id}", item);
}).RequireFinePermission("address-types.create");
entityAddressTypesApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdateEntityAddressTypeRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.UpdateEntityAddressTypeAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("address-types.update");
entityAddressTypesApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.DeactivateEntityAddressTypeAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("address-types.delete");

var paymentTypesApi = app.MapGroup("/api/v1/master-tables/payment-types");
paymentTypesApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? name, string? status, IntegratedLogisticsService service, CancellationToken ct) => service.SearchPaymentTypesAsync(new MasterTableFilter(id, name, status), ct)).RequireFinePermission("payment-types.filter");
paymentTypesApi.MapPost("/", [Authorize(Policy="users.write")] async (CreatePaymentTypeRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
{
    var item = await service.CreatePaymentTypeAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/master-tables/payment-types/{item.Id}", item);
}).RequireFinePermission("payment-types.create");
paymentTypesApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdatePaymentTypeRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.UpdatePaymentTypeAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("payment-types.update");
paymentTypesApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.DeactivatePaymentTypeAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("payment-types.delete");

var shippingLinesApi = app.MapGroup("/api/v1/master-tables/shipping-lines");
shippingLinesApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? description, string? sunatCode, string? status, IntegratedLogisticsService service, CancellationToken ct) => service.SearchShippingLinesAsync(new ShippingLineFilter(id, description, sunatCode, status), ct)).RequireFinePermission("ship-lines.filter");
shippingLinesApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateShippingLineRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
{
    var item = await service.CreateShippingLineAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/master-tables/shipping-lines/{item.Id}", item);
}).RequireFinePermission("ship-lines.create");
shippingLinesApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdateShippingLineRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.UpdateShippingLineAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("ship-lines.update");
shippingLinesApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.DeactivateShippingLineAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("ship-lines.delete");

var pickupPlacesApi = app.MapGroup("/api/v1/master-tables/pickup-places");
pickupPlacesApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? name, string? status, IntegratedLogisticsService service, CancellationToken ct) =>
    service.SearchPickupPlacesAsync(new MasterTableFilter(id, name, status), ct)).RequireFinePermission("pickup-sites.filter");
pickupPlacesApi.MapPost("/", [Authorize(Policy="users.write")] async (CreatePickupPlaceRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
{
    var item = await service.CreatePickupPlaceAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/master-tables/pickup-places/{item.Id}", item);
}).RequireFinePermission("pickup-sites.create");
pickupPlacesApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdatePickupPlaceRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.UpdatePickupPlaceAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("pickup-sites.update");
pickupPlacesApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.DeactivatePickupPlaceAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("pickup-sites.delete");

var currenciesApi = app.MapGroup("/api/v1/master-tables/currencies");
currenciesApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? description, string? symbol, string? status, IntegratedLogisticsService service, CancellationToken ct) =>
    service.SearchCurrenciesAsync(new CurrencyFilter(id, description, symbol, status), ct)).RequireFinePermission("currencies.filter");
currenciesApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateCurrencyRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
{
    var item = await service.CreateCurrencyAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/master-tables/currencies/{item.Id}", item);
}).RequireFinePermission("currencies.create");
currenciesApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdateCurrencyRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.UpdateCurrencyAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("currencies.update");
currenciesApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.DeactivateCurrencyAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("currencies.delete");

var currentAccountDocumentTypesApi = app.MapGroup("/api/v1/master-tables/current-account-document-types");
currentAccountDocumentTypesApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? code, string? description, string? status, IntegratedLogisticsService service, CancellationToken ct) =>
    service.SearchCurrentAccountDocumentTypesAsync(new CurrentAccountDocumentTypeFilter(id, code, description, status), ct)).RequireFinePermission("acct-docs.filter");
currentAccountDocumentTypesApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateCurrentAccountDocumentTypeRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
{
    var item = await service.CreateCurrentAccountDocumentTypeAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/master-tables/current-account-document-types/{item.Id}", item);
}).RequireFinePermission("acct-docs.create");
currentAccountDocumentTypesApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdateCurrentAccountDocumentTypeRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.UpdateCurrentAccountDocumentTypeAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("acct-docs.update");
currentAccountDocumentTypesApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.DeactivateCurrentAccountDocumentTypeAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("acct-docs.delete");

var packagingTypesApi = app.MapGroup("/api/v1/master-tables/packaging-types");
packagingTypesApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? name, string? status, IntegratedLogisticsService service, CancellationToken ct) =>
    service.SearchPackagingTypesAsync(new MasterTableFilter(id, name, status), ct)).RequireFinePermission("pack-types.filter");
packagingTypesApi.MapPost("/", [Authorize(Policy="users.write")] async (CreatePackagingTypeRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
{
    var item = await service.CreatePackagingTypeAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct);
    return Results.Created($"/api/v1/master-tables/packaging-types/{item.Id}", item);
}).RequireFinePermission("pack-types.create");
packagingTypesApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdatePackagingTypeRequest request, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.UpdatePackagingTypeAsync(id.ToString(), request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("pack-types.update");
packagingTypesApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, IntegratedLogisticsService service, CancellationToken ct) =>
    await service.DeactivatePackagingTypeAsync(id.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) == 1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("pack-types.delete");

var applicationsApi = app.MapGroup("/api/v1/security/master/applications");
applicationsApi.MapGet("/", [Authorize(Policy="users.read")] (string? idApp, string? idEmpresa, string? aplicacion, string? estado, SecurityMasterService service, CancellationToken ct) => service.SearchAsync(SecurityMasterType.Application, new(idApp, idEmpresa, aplicacion, estado), ct)).RequireFinePermission("applications.filter");
MapMasterWrites(applicationsApi, SecurityMasterType.Application, "applications");
var domainsApi = app.MapGroup("/api/v1/security/master/domains");
domainsApi.MapGet("/", [Authorize(Policy="users.read")] (string? idDominio, string? idEmpresa, string? dominio, string? estado, SecurityMasterService service, CancellationToken ct) => service.SearchAsync(SecurityMasterType.Domain, new(idDominio, idEmpresa, dominio, estado), ct)).RequireFinePermission("domains.filter");
MapMasterWrites(domainsApi, SecurityMasterType.Domain, "domains");
var areasApi = app.MapGroup("/api/v1/security/master/areas");
areasApi.MapGet("/", [Authorize(Policy="users.read")] (string? idArea, string? idEmpresa, string? area, string? estado, SecurityMasterService service, CancellationToken ct) => service.SearchAsync(SecurityMasterType.Area, new(idArea, idEmpresa, area, estado), ct)).RequireFinePermission("areas.filter");
MapMasterWrites(areasApi, SecurityMasterType.Area, "areas");
var rolesApi = app.MapGroup("/api/v1/security/master/roles");
rolesApi.MapGet("/", [Authorize(Policy="users.read")] (string? idRol, string? idArea, string? rol, string? estado, SecurityMasterService service, CancellationToken ct) => service.SearchAsync(SecurityMasterType.Role, new(idRol, idArea, rol, estado), ct)).RequireFinePermission("roles.filter");
MapMasterWrites(rolesApi, SecurityMasterType.Role, "roles");
var masterModulesApi = app.MapGroup("/api/v1/security/master/modules");
masterModulesApi.MapGet("/", [Authorize(Policy="users.read")] (string? idModulo, string? idApp, string? modulo, string? estado, SecurityMasterService service, CancellationToken ct) => service.SearchAsync(SecurityMasterType.Module, new(idModulo, idApp, modulo, estado), ct)).RequireFinePermission("modules.filter");
MapMasterWrites(masterModulesApi, SecurityMasterType.Module, "modules");
var masterMenusApi = app.MapGroup("/api/v1/security/master/menus");
masterMenusApi.MapGet("/", [Authorize(Policy="users.read")] (string? idMenu, string? idModulo, string? menu, string? estado, SecurityMasterService service, CancellationToken ct) => service.SearchAsync(SecurityMasterType.Menu, new(idMenu, idModulo, menu, estado), ct)).RequireFinePermission("menus.filter");
MapMasterWrites(masterMenusApi, SecurityMasterType.Menu, "menus");
var masterSubmenusApi = app.MapGroup("/api/v1/security/master/submenus");
masterSubmenusApi.MapGet("/", [Authorize(Policy="users.read")] (string? idSubmenu, string? idMenu, string? submenu, string? estado, SecurityMasterService service, CancellationToken ct) => service.SearchAsync(SecurityMasterType.Submenu, new(idSubmenu, idMenu, submenu, estado), ct)).RequireFinePermission("submenus.filter");
MapMasterWrites(masterSubmenusApi, SecurityMasterType.Submenu, "submenus");
var masterFormOptionsApi = app.MapGroup("/api/v1/security/master/form-options");
// Las opciones del formulario pueden depender directamente de un submenú; el
// segundo nivel es opcional y no debe impedir su administración.
// Esta ruta alimenta la pantalla de asignación de accesos. No es el
// mantenimiento de opciones, por lo que debe seguir el permiso de escritura
// de usuarios que habilita a administrar sus accesos.
masterFormOptionsApi.MapGet("/by-submenu/{submenuId:guid}", [Authorize(Policy="users.write")] (Guid submenuId, UserAccessService service, CancellationToken ct) =>
    service.GetFormOptionsAsync(submenuId.ToString(), ct));
masterFormOptionsApi.MapGet("/", [Authorize(Policy="users.write")] (string? id, string? submenuId, string? secondLevelId, string? name, string? status, FormOptionMasterService service, CancellationToken ct) => service.SearchAsync(new(id,submenuId,secondLevelId,name,status),ct)).RequireFinePermission("form-options.filter");
masterFormOptionsApi.MapPost("/", [Authorize(Policy="users.write")] async (FormOptionMasterRequest request, ClaimsPrincipal principal, FormOptionMasterService service, CancellationToken ct) => { var item=await service.CreateAsync(request,principal.FindFirstValue("unique_name") ?? string.Empty,ct); return Results.Created($"/api/v1/security/master/form-options/{item.Id}",item); }).RequireFinePermission("form-options.create");
masterFormOptionsApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, FormOptionMasterRequest request, ClaimsPrincipal principal, FormOptionMasterService service, CancellationToken ct) => await service.UpdateAsync(id.ToString(),request,principal.FindFirstValue("unique_name") ?? string.Empty,ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission("form-options.update");
masterFormOptionsApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, FormOptionMasterService service, CancellationToken ct) => Results.Ok(new { rowsAffected=await service.DeactivateAsync(id.ToString(),principal.FindFirstValue("unique_name") ?? string.Empty,ct) })).RequireFinePermission("form-options.delete");
var productsApi = app.MapGroup("/api/v1/security/master/products");
productsApi.MapGet("/", [Authorize(Policy="users.read")] (string? id, string? name, string? status, SecurityMasterService service, CancellationToken ct) => service.SearchAsync(SecurityMasterType.Product, new(id, null, name, status), ct)).RequireFinePermission("products.filter");
MapMasterWrites(productsApi, SecurityMasterType.Product, "products");
var masterUsersApi = app.MapGroup("/api/v1/security/master/users");
masterUsersApi.MapGet("/", [Authorize(Policy="users.read")] (string? idUser, string? idDominio, string? idRol, string? login, string? email, string? estado, SecurityMasterUserService service, CancellationToken ct) => service.SearchAsync(new(idUser,idDominio,idRol,login,email,estado),ct)).RequireFinePermission("users.filter");
masterUsersApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateSecurityMasterUserRequest request, ClaimsPrincipal principal, SecurityMasterUserService service, CancellationToken ct) => { var user=await service.CreateAsync(request,principal.FindFirstValue("unique_name") ?? string.Empty,ct); return Results.Created($"/api/v1/security/master/users/{user.Id}",user); }).RequireFinePermission("users.create");
masterUsersApi.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdateSecurityMasterUserRequest request, ClaimsPrincipal principal, SecurityMasterUserService service, CancellationToken ct) => await service.UpdateAsync(id.ToString(),request,principal.FindFirstValue("unique_name") ?? string.Empty,ct) is { } user ? Results.Ok(user) : Results.NotFound()).RequireFinePermission("users.update");
masterUsersApi.MapPut("/{id:guid}/password", [Authorize(Policy="users.write")] async (Guid id, ResetSecurityMasterUserPasswordRequest request, ClaimsPrincipal principal, SecurityMasterUserService service, IFinePermissionRepository permissions, CancellationToken ct) =>
{
    var code = string.IsNullOrWhiteSpace(request.Password) ? "users.reset-password" : "users.change-password";
    if (!Guid.TryParse(principal.FindFirstValue("sub"), out var actorId) || !await permissions.HasPermissionAsync(actorId, code, ct))
        return Results.Json(new { message = "El usuario no tiene permiso para ejecutar esta acción.", permission = code }, statusCode: StatusCodes.Status403Forbidden);
    return await service.ResetPasswordAsync(id.ToString(),request,principal.FindFirstValue("unique_name") ?? string.Empty,ct) is { } result ? Results.Ok(result) : Results.NotFound();
});
masterUsersApi.MapPut("/{id:guid}/modules/{moduleId:guid}", [Authorize(Policy="users.write")] async (Guid id, Guid moduleId, ClaimsPrincipal principal, UserNavigationAssignmentService service, CancellationToken ct) => Results.Ok(await service.AssignModuleAsync(id.ToString(),moduleId.ToString(),principal.FindFirstValue("unique_name") ?? string.Empty,ct)));
masterUsersApi.MapPut("/{id:guid}/menus/{menuId:guid}", [Authorize(Policy="users.write")] async (Guid id, Guid menuId, ClaimsPrincipal principal, UserNavigationAssignmentService service, CancellationToken ct) => Results.Ok(await service.AssignMenuAsync(id.ToString(),menuId.ToString(),principal.FindFirstValue("unique_name") ?? string.Empty,ct)));
masterUsersApi.MapPut("/{id:guid}/submenus/{submenuId:guid}", [Authorize(Policy="users.write")] async (Guid id, Guid submenuId, ClaimsPrincipal principal, UserNavigationAssignmentService service, CancellationToken ct) => Results.Ok(await service.AssignSubmenuAsync(id.ToString(),submenuId.ToString(),principal.FindFirstValue("unique_name") ?? string.Empty,ct)));
masterUsersApi.MapGet("/{id:guid}/access", [Authorize(Policy="users.read")] async (Guid id, UserAccessService service, CancellationToken ct) => Results.Ok(await service.GetAccessAsync(id.ToString(), ct)));
masterUsersApi.MapPut("/{id:guid}/form-options/{optionId:guid}", [Authorize(Policy="users.write")] async (Guid id, Guid optionId, ClaimsPrincipal principal, UserAccessService service, CancellationToken ct) => Results.Ok(await service.AssignFormOptionAsync(id.ToString(), optionId.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct)));
masterUsersApi.MapDelete("/{id:guid}/access/{accessId:guid}", [Authorize(Policy="users.write")] async (Guid id, Guid accessId, ClaimsPrincipal principal, UserAccessService service, CancellationToken ct) => await service.RemoveAccessAsync(id.ToString(), accessId.ToString(), principal.FindFirstValue("unique_name") ?? string.Empty, ct) ? Results.NoContent() : Results.NotFound());
masterUsersApi.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, SecurityMasterUserService service, CancellationToken ct) => await service.DeactivateAsync(id.ToString(),principal.FindFirstValue("unique_name") ?? string.Empty,ct)==1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission("users.delete");

var userProfilesApi = app.MapGroup("/api/v1/security/user-profiles");
userProfilesApi.MapGet("/", [Authorize(Policy="users.read")] (Guid? idperfil, Guid? idapp, string? nombre, string? estado, UserProfileService service, CancellationToken ct) => service.ListAsync(idperfil, idapp, nombre, estado, ct)).RequireFinePermission("profiles.filter");
userProfilesApi.MapPost("/", [Authorize(Policy="users.write")] async (CreateUserProfileRequest request, ClaimsPrincipal principal, UserProfileService service, CancellationToken ct) => Results.Created("/api/v1/security/user-profiles", await service.CreateAsync(request, principal.FindFirstValue("unique_name") ?? string.Empty, ct))).RequireFinePermission("profiles.create");
userProfilesApi.MapPut("/{profileId:guid}", [Authorize(Policy="users.write")] async (Guid profileId, UpdateUserProfileRequest request, ClaimsPrincipal principal, UserProfileService service, CancellationToken ct) => await service.UpdateAsync(profileId, request, principal.FindFirstValue("unique_name") ?? string.Empty, ct) is { } profile ? Results.Ok(profile) : Results.NotFound()).RequireFinePermission("profiles.update");
userProfilesApi.MapDelete("/{profileId:guid}", [Authorize(Policy="users.write")] async (Guid profileId, ClaimsPrincipal principal, UserProfileService service, CancellationToken ct) => await service.DeactivateAsync(profileId, principal.FindFirstValue("unique_name") ?? string.Empty, ct) ? Results.NoContent() : Results.NotFound()).RequireFinePermission("profiles.delete");
MapProfileAccess("modules", ProfileAccessKind.Modules);
MapProfileAccess("menus", ProfileAccessKind.Menus);
MapProfileAccess("submenus", ProfileAccessKind.Submenus);
MapProfileAccess("second-level-submenus", ProfileAccessKind.SecondLevels);
MapProfileAccess("form-options", ProfileAccessKind.FormOptions);
userProfilesApi.MapDelete("/access/{accessId:guid}", [Authorize(Policy="users.write")] async (Guid accessId, ClaimsPrincipal principal, UserProfileService service, CancellationToken ct) => await service.RemoveAccessAsync(accessId, principal.FindFirstValue("unique_name") ?? string.Empty, ct) ? Results.NoContent() : Results.NotFound()).RequireFinePermission("profiles.update");
userProfilesApi.MapPut("/{profileId:guid}/apply/users/{userId:guid}", [Authorize(Policy="users.write")] async (Guid profileId, Guid userId, ClaimsPrincipal principal, UserProfileService service, CancellationToken ct) => Results.Ok(await service.ApplyAsync(profileId, userId, principal.FindFirstValue("unique_name") ?? string.Empty, ct))).RequireFinePermission("profiles.assign");

void MapProfileAccess(string segment, ProfileAccessKind kind)
{
    userProfilesApi.MapGet("/{profileId:guid}/" + segment, [Authorize(Policy="users.read")] (Guid profileId, Guid? parentId, UserProfileService service, CancellationToken ct) => service.ListAccessAsync(kind, profileId, parentId, ct)).RequireFinePermission("profiles.update");
    userProfilesApi.MapPut("/{profileId:guid}/" + segment + "/{resourceId:guid}", [Authorize(Policy="users.write")] async (Guid profileId, Guid resourceId, ClaimsPrincipal principal, UserProfileService service, CancellationToken ct) => Results.Ok(await service.AssignAccessAsync(kind, profileId, resourceId, principal.FindFirstValue("unique_name") ?? string.Empty, ct))).RequireFinePermission("profiles.update");
}

void MapMasterWrites(RouteGroupBuilder group, SecurityMasterType type, string resource)
{
    group.MapPost("/", [Authorize(Policy="users.write")] async (CreateSecurityMasterRequest request, ClaimsPrincipal principal, SecurityMasterService service, CancellationToken ct) => { var item=await service.CreateAsync(type,request,principal.FindFirstValue("unique_name") ?? string.Empty,ct); return Results.Created($"/api/v1/security/master/{MasterRoute(type)}/{item.Id}",item); }).RequireFinePermission($"{resource}.create");
    group.MapPut("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, UpdateSecurityMasterRequest request, ClaimsPrincipal principal, SecurityMasterService service, CancellationToken ct) => await service.UpdateAsync(type,id.ToString(),request,principal.FindFirstValue("unique_name") ?? string.Empty,ct) is { } item ? Results.Ok(item) : Results.NotFound()).RequireFinePermission($"{resource}.update");
    group.MapDelete("/{id:guid}", [Authorize(Policy="users.write")] async (Guid id, ClaimsPrincipal principal, SecurityMasterService service, CancellationToken ct) => await service.DeactivateAsync(type,id.ToString(),principal.FindFirstValue("unique_name") ?? string.Empty,ct)==1 ? Results.NoContent() : Results.NotFound()).RequireFinePermission($"{resource}.delete");
}

string MasterRoute(SecurityMasterType type) => type switch
{
    SecurityMasterType.Application => "applications",
    SecurityMasterType.Domain => "domains",
    SecurityMasterType.Area => "areas",
    SecurityMasterType.Role => "roles",
    SecurityMasterType.Module => "modules",
    SecurityMasterType.Menu => "menus",
    SecurityMasterType.Submenu => "submenus",
    SecurityMasterType.Product => "products",
    _ => throw new ArgumentOutOfRangeException(nameof(type))
};
var profilesApi = app.MapGroup("/api/v1/security/profiles").RequireAuthorization("users.write");
profilesApi.MapGet("/", (SecurityAdminService service,CancellationToken ct) => service.GetProfilesAsync(ct));
profilesApi.MapPost("/", async (CreateProfileRequest request,SecurityAdminService service,CancellationToken ct) => Results.Created($"/api/v1/security/profiles/{await service.CreateProfileAsync(request,ct)}",null));
profilesApi.MapPut("/{id:guid}", async (Guid id,UpdateProfileRequest request,SecurityAdminService service,CancellationToken ct) => await service.UpdateProfileAsync(id,request,ct)?Results.NoContent():Results.NotFound());
profilesApi.MapPut("/{id:guid}/permissions", async (Guid id,ReplacePermissionsRequest request,SecurityAdminService service,CancellationToken ct) => { await service.ReplacePermissionsAsync(id,request,ct); return Results.NoContent(); });
app.MapGet("/api/v1/security/permissions", [Authorize(Policy="users.write")] (SecurityAdminService service,CancellationToken ct) => service.GetPermissionsAsync(ct));

if (app.Environment.IsDevelopment() && databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase)) app.Services.GetRequiredService<InMemorySecurityStore>().SeedDevelopmentUser();
app.Run();

void SetSessionCookie(HttpContext context, TokenResult result)
{
    context.Response.Cookies.Append("cxview_session", result.AccessToken, new CookieOptions
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        Expires = result.ExpiresAt
    });
}

public partial class Program;

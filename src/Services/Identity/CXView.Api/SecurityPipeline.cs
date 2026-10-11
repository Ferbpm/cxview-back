using System.Security.Claims;
using CXView.Application;
using Microsoft.AspNetCore.Diagnostics;
using System.Security.Cryptography;

public sealed class SessionValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ISessionRepository sessions)
    {
        if (context.Request.Path.Equals("/api/v1/auth/refresh", StringComparison.OrdinalIgnoreCase))
        { await next(context); return; }
        if (context.User.Identity?.IsAuthenticated == true)
        {
            if (!Guid.TryParse(context.User.FindFirstValue("sid"), out var sid) ||
                !Guid.TryParse(context.User.FindFirstValue("sub"), out var uid) ||
                !await sessions.IsActiveAsync(sid, uid, context.RequestAborted))
            { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return; }
        }
        await next(context);
    }
}

/// <summary>
/// Convierte fallos de validación de reglas de negocio en una respuesta manejable
/// por la interfaz, antes de que alcancen el manejador global de excepciones.
/// </summary>
public sealed class ValidationExceptionMiddleware(RequestDelegate next, ILogger<ValidationExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ArgumentException exception) when (!context.Response.HasStarted)
        {
            logger.LogWarning("Validation error. TraceId: {TraceId}; Message: {Message}", context.TraceIdentifier, exception.Message);
            await Results.Json(
                new { message = exception.Message, code = "validation_error", traceId = context.TraceIdentifier },
                statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(context);
        }
    }
}

public sealed record FinePermissionMetadata(string Code);

public sealed class FinePermissionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var code = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<FinePermissionMetadata>()?.Code;
        if (string.IsNullOrWhiteSpace(code) || !Guid.TryParse(context.HttpContext.User.FindFirstValue("sub"), out var userId)) return Results.Unauthorized();
        var repository = context.HttpContext.RequestServices.GetRequiredService<IFinePermissionRepository>();
        if (await repository.HasPermissionAsync(userId, code, context.HttpContext.RequestAborted)) return await next(context);
        return Results.Json(new { message = "El usuario no tiene permiso para ejecutar esta acción.", permission = code }, statusCode: StatusCodes.Status403Forbidden);
    }
}

public static class FinePermissionEndpointExtensions
{
    public static RouteHandlerBuilder RequireFinePermission(this RouteHandlerBuilder builder, string code) => builder.WithMetadata(new FinePermissionMetadata(code)).AddEndpointFilter<FinePermissionFilter>();
}

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is ArgumentException)
        {
            logger.LogWarning("Validation error. TraceId: {TraceId}; Message: {Message}", context.TraceIdentifier, exception.Message);
            await Results.Json(new { message = exception.Message, code = "validation_error", traceId = context.TraceIdentifier }, statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(context);
            return true;
        }
        logger.LogError(exception, "Unhandled request error. TraceId: {TraceId}", context.TraceIdentifier);
        await Results.Problem(statusCode: 500, title: "No se pudo procesar la solicitud.", extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
        return true;
    }
}

public static class DevelopmentKeyBootstrap
{
    public static void EnsureKeys(IHostEnvironment environment, IConfiguration configuration)
    {
        if (!environment.IsDevelopment()) return;
        var privatePath = configuration["Jwt:PrivateKeyPath"] ?? throw new InvalidOperationException("Jwt private key path is required.");
        var publicPath = configuration["Jwt:PublicKeyPath"] ?? throw new InvalidOperationException("Jwt public key path is required.");
        if (File.Exists(privatePath) && File.Exists(publicPath)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(privatePath) ?? ".");
        Directory.CreateDirectory(Path.GetDirectoryName(publicPath) ?? ".");
        using var rsa = RSA.Create(3072);
        File.WriteAllText(privatePath, rsa.ExportRSAPrivateKeyPem());
        File.WriteAllText(publicPath, rsa.ExportRSAPublicKeyPem());
    }
}

using System.Text;
using CXView.Domain;

namespace CXView.Application;

public sealed class SecurityMasterService(ISecurityMasterRepository repository)
{
    public Task<IReadOnlyCollection<SecurityMasterRecord>> SearchAsync(SecurityMasterType type, SecurityMasterFilter filter, CancellationToken ct) =>
        repository.SearchAsync(type, new SecurityMasterFilter(OptionalId(filter.Id), Optional(filter.ParentId, 36), Optional(filter.Name, 50), Status(filter.Status, true)), ct);
    public Task<SecurityMasterRecord> CreateAsync(SecurityMasterType type, CreateSecurityMasterRequest request, string userName, CancellationToken ct) =>
        repository.CreateAsync(type, RequiredId(request.ParentId), Required(request.Name, 50, "El nombre es obligatorio y no puede superar 50 caracteres."), Optional(request.Description, DescriptionMax(type)), ViewKey(type, request.ViewKey), User(userName), ct);
    public Task<SecurityMasterRecord?> UpdateAsync(SecurityMasterType type, string id, UpdateSecurityMasterRequest request, string userName, CancellationToken ct) =>
        repository.UpdateAsync(type, RequiredId(id), RequiredId(request.ParentId), Required(request.Name, 50, "El nombre es obligatorio y no puede superar 50 caracteres."), Optional(request.Description, DescriptionMax(type)), ViewKey(type, request.ViewKey), Status(request.Status, false)!, User(userName), ct);
    public Task<int> DeactivateAsync(SecurityMasterType type, string id, string userName, CancellationToken ct) => repository.DeactivateAsync(type, RequiredId(id), User(userName), ct);

    internal static string? Optional(string? value, int max) { var v=value?.Trim(); if(string.IsNullOrEmpty(v)) return null; if(v.Length>max) throw new ArgumentException($"El valor no puede superar {max} caracteres."); return v; }
    internal static string? OptionalId(string? value) { var v=Optional(value,36); if(v is not null && !Guid.TryParse(v,out _)) throw new ArgumentException("El identificador no es válido."); return v; }
    internal static string RequiredId(string value) => OptionalId(value) ?? throw new ArgumentException("El identificador es obligatorio.");
    internal static string Required(string? value,int max,string message) => Optional(value,max) ?? throw new ArgumentException(message);
    internal static string? Status(string? value,bool optional) { var v=Optional(value,1)?.ToUpperInvariant(); if(v is null && optional)return null; if(v is not "A" and not "I") throw new ArgumentException("El estado debe ser A o I."); return v; }
    internal static string User(string value) => Required(value,20,"No se encontró el usuario autenticado.");
    private static int DescriptionMax(SecurityMasterType type) => type switch { SecurityMasterType.Application => 200, SecurityMasterType.Menu => 50, _ => 100 };
    private static string? ViewKey(SecurityMasterType type, string? value) => type == SecurityMasterType.Submenu ? Optional(value, 100) : null;
}

public sealed class SecurityMasterUserService(ISecurityMasterUserRepository repository, IPasswordHasher passwordHasher)
{
    public Task<IReadOnlyCollection<SecurityMasterUser>> SearchAsync(SecurityUserFilter filter, CancellationToken ct) => repository.SearchAsync(new(SecurityMasterService.OptionalId(filter.Id), SecurityMasterService.OptionalId(filter.DomainId), SecurityMasterService.OptionalId(filter.RoleId), SecurityMasterService.Optional(filter.Login,20), SecurityMasterService.Optional(filter.Email,50), SecurityMasterService.Status(filter.Status,true)), ct);
    public Task<SecurityMasterUser> CreateAsync(CreateSecurityMasterUserRequest request,string userName,CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8) throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
        return repository.CreateAsync(Normalize(request), Encoding.UTF8.GetBytes(passwordHasher.Hash(request.Password)), SecurityMasterService.User(userName), ct);
    }
    public Task<SecurityMasterUser?> UpdateAsync(string id,UpdateSecurityMasterUserRequest request,string userName,CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(request.Password) && request.Password.Length < 8) throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
        return repository.UpdateAsync(SecurityMasterService.RequiredId(id), Normalize(request), string.IsNullOrEmpty(request.Password) ? null : Encoding.UTF8.GetBytes(passwordHasher.Hash(request.Password)), SecurityMasterService.User(userName), ct);
    }
    public Task<int> DeactivateAsync(string id,string userName,CancellationToken ct) => repository.DeactivateAsync(SecurityMasterService.RequiredId(id),SecurityMasterService.User(userName),ct);
    public Task<PasswordResetResult?> ResetPasswordAsync(string id, ResetSecurityMasterUserPasswordRequest request, string userName, CancellationToken ct)
    {
        var password = string.IsNullOrWhiteSpace(request.Password) ? null : request.Password;
        if (password is not null && password.Length < 8) throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
        var hash = password is null ? null : Encoding.UTF8.GetBytes(passwordHasher.Hash(password));
        return repository.ResetPasswordAsync(SecurityMasterService.RequiredId(id), hash, SecurityMasterService.User(userName), ct);
    }
    private static CreateSecurityMasterUserRequest Normalize(CreateSecurityMasterUserRequest r) => r with { DomainId=SecurityMasterService.OptionalId(r.DomainId),RoleId=SecurityMasterService.OptionalId(r.RoleId),Login=SecurityMasterService.Required(r.Login,20,"El login es obligatorio."),FirstNames=SecurityMasterService.Optional(r.FirstNames,50),PaternalLastName=SecurityMasterService.Optional(r.PaternalLastName,50),MaternalLastName=SecurityMasterService.Optional(r.MaternalLastName,50),Email=SecurityMasterService.Optional(r.Email,50),DefaultSiteId=SecurityMasterService.OptionalId(r.DefaultSiteId) };
    private static UpdateSecurityMasterUserRequest Normalize(UpdateSecurityMasterUserRequest r) => r with { DomainId=SecurityMasterService.OptionalId(r.DomainId),RoleId=SecurityMasterService.OptionalId(r.RoleId),Login=SecurityMasterService.Required(r.Login,20,"El login es obligatorio."),FirstNames=SecurityMasterService.Optional(r.FirstNames,50),PaternalLastName=SecurityMasterService.Optional(r.PaternalLastName,50),MaternalLastName=SecurityMasterService.Optional(r.MaternalLastName,50),Email=SecurityMasterService.Optional(r.Email,50),Status=SecurityMasterService.Status(r.Status,false)!,DefaultSiteId=SecurityMasterService.OptionalId(r.DefaultSiteId) };
}

public sealed class UserNavigationAssignmentService(IUserNavigationAssignmentRepository repository)
{
    public Task<UserModuleAssignment> AssignModuleAsync(string userId, string moduleId, string auditUser, CancellationToken ct) => repository.AssignModuleAsync(SecurityMasterService.RequiredId(userId), SecurityMasterService.RequiredId(moduleId), SecurityMasterService.User(auditUser), ct);
    public Task<UserMenuAssignment> AssignMenuAsync(string userId, string menuId, string auditUser, CancellationToken ct) => repository.AssignMenuAsync(SecurityMasterService.RequiredId(userId), SecurityMasterService.RequiredId(menuId), SecurityMasterService.User(auditUser), ct);
    public Task<UserSubmenuAssignment> AssignSubmenuAsync(string userId, string submenuId, string auditUser, CancellationToken ct) => repository.AssignSubmenuAsync(SecurityMasterService.RequiredId(userId), SecurityMasterService.RequiredId(submenuId), SecurityMasterService.User(auditUser), ct);
}

public sealed class UserAccessService(IUserAccessRepository repository)
{
    public Task<IReadOnlyCollection<SecurityMenu>> GetMenusAsync(string moduleId, CancellationToken ct) => repository.GetMenusAsync(SecurityMasterService.RequiredId(moduleId), ct);
    public Task<IReadOnlyCollection<SecuritySubmenu>> GetSubmenusAsync(string menuId, CancellationToken ct) => repository.GetSubmenusAsync(SecurityMasterService.RequiredId(menuId), ct);
    public Task<IReadOnlyCollection<SecurityFormOption>> GetFormOptionsAsync(string submenuId, CancellationToken ct) => repository.GetFormOptionsAsync(SecurityMasterService.RequiredId(submenuId), ct);
    public Task<UserAccessSnapshot> GetAccessAsync(string userId, CancellationToken ct) => repository.GetAccessAsync(SecurityMasterService.RequiredId(userId), ct);
    public Task<UserFormOptionAssignment> AssignFormOptionAsync(string userId, string optionId, string auditUser, CancellationToken ct) => repository.AssignFormOptionAsync(SecurityMasterService.RequiredId(userId), SecurityMasterService.RequiredId(optionId), SecurityMasterService.User(auditUser), ct);
    public Task<bool> RemoveAccessAsync(string userId, string accessId, string auditUser, CancellationToken ct) => repository.RemoveAccessAsync(SecurityMasterService.RequiredId(userId), SecurityMasterService.RequiredId(accessId), SecurityMasterService.User(auditUser), ct);
}

public sealed class FormOptionMasterService(IFormOptionMasterRepository repository)
{
    public Task<IReadOnlyCollection<FormOptionMaster>> SearchAsync(FormOptionMasterFilter filter, CancellationToken ct) => repository.SearchAsync(filter, ct);
    public Task<FormOptionMaster> CreateAsync(FormOptionMasterRequest request, string user, CancellationToken ct) => repository.CreateAsync(Normalize(request, false), SecurityMasterService.User(user), ct);
    public Task<FormOptionMaster?> UpdateAsync(string id, FormOptionMasterRequest request, string user, CancellationToken ct) => repository.UpdateAsync(SecurityMasterService.RequiredId(id), Normalize(request, true), SecurityMasterService.User(user), ct);
    public Task<int> DeactivateAsync(string id, string user, CancellationToken ct) => repository.DeactivateAsync(SecurityMasterService.RequiredId(id), SecurityMasterService.User(user), ct);
    private static FormOptionMasterRequest Normalize(FormOptionMasterRequest r, bool update)
    {
        var submenu = SecurityMasterService.OptionalId(r.SubmenuId); var second = SecurityMasterService.OptionalId(r.SecondLevelId);
        if (submenu is null && second is null) throw new ArgumentException("Debe indicar un submenú o submenú segundo nivel.");
        return r with { SubmenuId=submenu, SecondLevelId=second, Code=SecurityMasterService.Required(r.Code,50,"El código es obligatorio."), Name=SecurityMasterService.Required(r.Name,50,"El nombre es obligatorio."), Description=SecurityMasterService.Optional(r.Description,50), Color=SecurityMasterService.Optional(r.Color,20), Icon=SecurityMasterService.Optional(r.Icon,30), Status=update ? SecurityMasterService.Status(r.Status,false) : null };
    }
}

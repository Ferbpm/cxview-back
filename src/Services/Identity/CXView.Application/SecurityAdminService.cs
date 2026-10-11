namespace CXView.Application;

public sealed class SecurityAdminService(ISecurityAdminRepository repository, IPasswordHasher passwordHasher)
{
    public Task<IReadOnlyCollection<Domain.UserSummary>> GetUsersAsync(CancellationToken ct) => repository.GetUsersAsync(ct);
    public Task<Guid> CreateUserAsync(CreateUserRequest request, CancellationToken ct)
    {
        if (request.Password.Length < 8) throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
        return repository.CreateUserAsync(request.UserName.Trim(), request.Email.Trim(), passwordHasher.Hash(request.Password), ct);
    }
    public Task<bool> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken ct) => repository.UpdateUserAsync(id, request.Email.Trim(), request.IsActive, ct);
    public Task<bool> DeactivateUserAsync(Guid id, CancellationToken ct) => repository.DeactivateUserAsync(id, ct);
    public Task<IReadOnlyCollection<Domain.SecurityProfile>> GetProfilesAsync(CancellationToken ct) => repository.GetProfilesAsync(ct);
    public Task<Guid> CreateProfileAsync(CreateProfileRequest request, CancellationToken ct) => repository.CreateProfileAsync(request.Code.Trim(), request.Name.Trim(), ct);
    public Task<bool> UpdateProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken ct) => repository.UpdateProfileAsync(id, request.Name.Trim(), request.IsActive, ct);
    public Task<IReadOnlyCollection<Domain.SecurityPermission>> GetPermissionsAsync(CancellationToken ct) => repository.GetPermissionsAsync(ct);
    public Task AssignProfileAsync(Guid userId, AssignProfileRequest request, CancellationToken ct) => repository.AssignProfileAsync(userId, request.ProfileId, ct);
    public Task ReplacePermissionsAsync(Guid profileId, ReplacePermissionsRequest request, CancellationToken ct) => repository.ReplaceProfilePermissionsAsync(profileId, request.PermissionIds, ct);
}

public sealed class UserProfileService(IUserProfileRepository repository)
{
    public Task<IReadOnlyCollection<Domain.UserProfile>> ListAsync(Guid? id, Guid? applicationId, string? name, string? status, CancellationToken ct) => repository.ListProfilesAsync(id, applicationId, name, status, ct);
    public Task<Domain.UserProfile> CreateAsync(CreateUserProfileRequest request, string actor, CancellationToken ct) => repository.CreateProfileAsync(request.ApplicationId, request.Name.Trim(), request.Description?.Trim(), actor, ct);
    public Task<Domain.UserProfile?> UpdateAsync(Guid id, UpdateUserProfileRequest request, string actor, CancellationToken ct) => repository.UpdateProfileAsync(id, request.ApplicationId, request.Name.Trim(), request.Description?.Trim(), request.Status, actor, ct);
    public Task<bool> DeactivateAsync(Guid id, string actor, CancellationToken ct) => repository.DeactivateProfileAsync(id, actor, ct);
    public Task<IReadOnlyCollection<Domain.ProfileAccessItem>> ListAccessAsync(ProfileAccessKind kind, Guid profileId, Guid? parentId, CancellationToken ct) => repository.ListAccessAsync(kind, profileId, parentId, ct);
    public Task<Domain.ProfileAccessItem> AssignAccessAsync(ProfileAccessKind kind, Guid profileId, Guid resourceId, string actor, CancellationToken ct) => repository.AssignAccessAsync(kind, profileId, resourceId, actor, ct);
    public Task<bool> RemoveAccessAsync(Guid accessId, string actor, CancellationToken ct) => repository.RemoveAccessAsync(accessId, actor, ct);
    public Task<Domain.ProfileApplicationResult> ApplyAsync(Guid profileId, Guid userId, string actor, CancellationToken ct) => repository.ApplyProfileAsync(profileId, userId, actor, ct);
}

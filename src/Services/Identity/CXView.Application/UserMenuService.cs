using CXView.Domain;

namespace CXView.Application;

public sealed class UserMenuService(IUserMenuRepository repository)
{
    public Task<IReadOnlyCollection<UserMenu>> GetByUserNameAndModuleIdAsync(string userName, string moduleId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);

        var normalizedUserName = userName.Trim();
        var normalizedModuleId = moduleId.Trim();
        if (normalizedUserName.Length > 20)
            throw new ArgumentException("El nombre de usuario no puede superar 20 caracteres.", nameof(userName));
        if (!Guid.TryParse(normalizedModuleId, out _))
            throw new ArgumentException("El identificador del módulo no es válido.", nameof(moduleId));

        return repository.GetByUserNameAndModuleIdAsync(normalizedUserName, normalizedModuleId, ct);
    }
}

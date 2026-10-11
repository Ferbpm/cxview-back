using CXView.Domain;

namespace CXView.Application;

public sealed class UserSubmenuService(IUserSubmenuRepository repository)
{
    public Task<IReadOnlyCollection<UserSubmenu>> GetByUserNameAndMenuIdAsync(string userName, string menuId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(menuId);

        var normalizedUserName = userName.Trim();
        var normalizedMenuId = menuId.Trim();
        if (normalizedUserName.Length > 20)
            throw new ArgumentException("El nombre de usuario no puede superar 20 caracteres.", nameof(userName));
        if (!Guid.TryParse(normalizedMenuId, out _))
            throw new ArgumentException("El identificador del menú no es válido.", nameof(menuId));

        return repository.GetByUserNameAndMenuIdAsync(normalizedUserName, normalizedMenuId, ct);
    }
}

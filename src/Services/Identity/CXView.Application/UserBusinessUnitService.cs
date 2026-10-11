using CXView.Domain;

namespace CXView.Application;

public sealed class UserBusinessUnitService(IUserBusinessUnitRepository repository)
{
    public Task<IReadOnlyCollection<UserBusinessUnit>> GetByUserNameAsync(string userName, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        var normalizedUserName = userName.Trim();
        if (normalizedUserName.Length > 20)
            throw new ArgumentException("El nombre de usuario no puede superar 20 caracteres.", nameof(userName));

        return repository.GetByUserNameAsync(normalizedUserName, ct);
    }
}

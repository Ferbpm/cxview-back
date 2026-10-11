using CXView.Domain;

namespace CXView.Application;

public sealed class UserModuleService(IUserModuleRepository repository)
{
    public Task<IReadOnlyCollection<UserModule>> GetByUserNameAsync(string userName, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        var normalizedUserName = userName.Trim();
        if (normalizedUserName.Length > 20)
            throw new ArgumentException("El nombre de usuario no puede superar 20 caracteres.", nameof(userName));

        return repository.GetByUserNameAsync(normalizedUserName, ct);
    }
}

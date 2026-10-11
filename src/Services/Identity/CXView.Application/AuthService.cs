using CXView.Domain;

namespace CXView.Application;

public sealed class AuthService(
    IUserRepository users,
    ISessionRepository sessions,
    IOtpRepository otps,
    IPasswordHasher passwords,
    IOtpHasher otpHasher,
    IOtpDelivery delivery,
    ITokenIssuer tokens,
    IClock clock)
{
    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();
        var user = await users.FindByUserNameAsync(userName, cancellationToken);
        if (user is null || !user.IsActive || !passwords.Verify(user.PasswordHash, request.Password))
            return new(false, "Credenciales inválidas.");

        var code = otpHasher.CreateCode();
        var challenge = new OtpChallenge(Guid.NewGuid(), user.Id, string.Empty, clock.UtcNow.AddMinutes(5), false);
        challenge = challenge with { OtpHash = otpHasher.Hash(challenge.Id, code) };
        await otps.SaveAsync(challenge, cancellationToken);
        await delivery.SendAsync(user.Email, code, cancellationToken);
        return new(true, "Se envió un código de verificación.", challenge.Id, MaskEmail(user.Email));
    }

    public async Task<TokenResult?> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var challenge = await otps.FindAsync(request.ChallengeId, cancellationToken);
        if (challenge is null || challenge.IsConsumed || challenge.ExpiresAt <= clock.UtcNow ||
            !otpHasher.Verify(challenge.Id, request.Code, challenge.OtpHash)) return null;

        if (!await otps.ConsumeAsync(challenge.Id, cancellationToken)) return null;
        var user = await users.FindByIdAsync(challenge.UserId, cancellationToken);
        if (user is null || !user.IsActive) return null;

        var permissions = ExpandApiPermissions(await users.GetPermissionsAsync(user.Id, cancellationToken));
        var identity = new UserIdentity(user.Id, user.UserName, user.Email, permissions);
        var expiresAt = clock.UtcNow.AddMinutes(30);
        var session = new AuthSession(Guid.NewGuid(), user.Id, expiresAt, false);
        await sessions.CreateAsync(session, cancellationToken);
        return new(tokens.Issue(identity, session.Id, expiresAt), expiresAt);
    }

    public async Task<TokenResult?> RefreshAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive) return null;
        var expiresAt = clock.UtcNow.AddMinutes(30);
        if (!await sessions.RenewAsync(sessionId, userId, clock.UtcNow, expiresAt, cancellationToken)) return null;
        var permissions = ExpandApiPermissions(await users.GetPermissionsAsync(user.Id, cancellationToken));
        return new(tokens.Issue(new UserIdentity(user.Id, user.UserName, user.Email, permissions), sessionId, expiresAt), expiresAt);
    }

    private static string MaskEmail(string email)
    {
        var separator = email.IndexOf('@');
        if (separator <= 0 || separator == email.Length - 1) return "tu correo registrado";

        var localPart = email[..separator];
        var visibleCharacters = Math.Min(2, localPart.Length);
        return $"{localPart[..visibleCharacters]}****{email[separator..]}";
    }

    private static IReadOnlyCollection<string> ExpandApiPermissions(IReadOnlyCollection<string> sourcePermissions)
    {
        var permissions = new HashSet<string>(sourcePermissions, StringComparer.OrdinalIgnoreCase);

        // Las opciones de la vista son las asignables desde perfiles. Las
        // capacidades de API son sus equivalentes de infraestructura y evitan
        // que al aplicar un perfil se pierda el acceso a los mantenimientos.
        if (permissions.Contains("users.filter")) permissions.Add(Permissions.UsersRead);
        if (permissions.Overlaps(["users.create", "users.update", "users.delete"])) permissions.Add(Permissions.UsersWrite);

        return permissions.ToArray();
    }
}

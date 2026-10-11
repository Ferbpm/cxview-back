using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CXView.Application;
using CXView.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace CXView.Infrastructure;

public sealed class SystemClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA512, 32);
        return $"pbkdf2-sha512${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string encodedHash, string password)
    {
        try
        {
            var parts = encodedHash.Split('$');
            if (parts.Length != 4 || parts[0] != "pbkdf2-sha512") return false;
            var iterations = int.Parse(parts[1], CultureInfo.InvariantCulture);
            if (iterations is < 100_000 or > 1_000_000) return false;
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException) { return false; }
        catch (OverflowException) { return false; }
    }
}

public sealed class HmacOtpHasher(IOptions<OtpOptions> options) : IOtpHasher
{
    private readonly byte[] _key = Convert.FromBase64String(options.Value.PepperBase64);
    public string CreateCode() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString(CultureInfo.InvariantCulture);
    public string Hash(Guid challengeId, string code)
    {
        using var hmac = new HMACSHA256(_key);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{challengeId:N}:{code}")));
    }
    public bool Verify(Guid challengeId, string code, string hash)
    {
        try { return CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(hash), Convert.FromBase64String(Hash(challengeId, code))); }
        catch (FormatException) { return false; }
    }
}

public sealed class RsaJwtIssuer : ITokenIssuer, IDisposable
{
    private readonly JwtOptions _options;
    private readonly RSA _rsa;
    private readonly SigningCredentials _signingCredentials;

    public RsaJwtIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _rsa = RSA.Create();
        _rsa.ImportFromPem(File.ReadAllText(_options.PrivateKeyPath));
        _signingCredentials = new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256);
    }

    public string Issue(UserIdentity user, Guid sessionId, DateTimeOffset expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Email, user.Email), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("sid", sessionId.ToString())
        };
        claims.AddRange(user.Permissions.Select(p => new Claim("permission", p)));
        var jwt = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, DateTime.UtcNow,
            expiresAt.UtcDateTime, _signingCredentials);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public void Dispose() => _rsa.Dispose();
}

public sealed class JwtOptions { public string Issuer { get; init; } = ""; public string Audience { get; init; } = ""; public string PrivateKeyPath { get; init; } = ""; public string PublicKeyPath { get; init; } = ""; }
public sealed class OtpOptions { public string PepperBase64 { get; init; } = ""; }

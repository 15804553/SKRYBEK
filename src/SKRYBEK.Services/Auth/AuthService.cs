using System.Security.Cryptography;
using System.Text;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Repositories;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Auth;

public sealed class AuthService
{
    private readonly ChomikAuthRepository _chomikAuth;

    public AuthService(ChomikAuthRepository chomikAuth)
    {
        _chomikAuth = chomikAuth;
    }

    /// <summary>Zwraca listę loginów z CHOMIK do wyświetlenia w oknie logowania.</summary>
    public async Task<List<UserAccount>> GetAvailableUsersAsync()
    {
        return await _chomikAuth.GetAllAsync();
    }

    /// <summary>Loguje użytkownika po loginie (ponowny odczyt z CHOMIK).</summary>
    public async Task<SessionInfo?> LoginAsync(string login, string haslo)
    {
        var user = await _chomikAuth.GetByLoginAsync(login.Trim());
        if (user is null)
        {
            SkrybekLog.Warning($"Nieudana próba logowania — brak użytkownika: {login}");
            return null;
        }

        return LoginCore(user, haslo);
    }

    /// <summary>Loguje wybranego użytkownika (hash z listy logowania — bez drugiego odczytu).</summary>
    public Task<SessionInfo?> LoginAsync(UserAccount user, string haslo) =>
        Task.FromResult(LoginCore(user, haslo));

    private SessionInfo? LoginCore(UserAccount user, string? haslo)
    {
        var password = (haslo ?? string.Empty).Trim();
        var hash = user.HasloHash.Trim();
        var salt = user.HasloSol.Trim();

        // Konto PA — puste hasło w bazie CHOMIK
        if (string.IsNullOrEmpty(hash))
        {
            if (!string.IsNullOrEmpty(password))
            {
                SkrybekLog.Warning($"Konto PA nie wymaga hasła: {user.Login}");
                return null;
            }
        }
        else if (!VerifyChomikPassword(password, hash, salt))
        {
            SkrybekLog.Warning(
                $"Błędne hasło dla użytkownika: {user.Login} (długość wpisanego hasła: {password.Length}, hash w bazie: {hash.Length} znaków)");
            return null;
        }

        SkrybekLog.Info($"Zalogowano: {user.Login} ({user.NazwaZmiany})");

        return new SessionInfo
        {
            UserId      = user.Id,
            Login       = user.Login,
            NazwaZmiany = user.NazwaZmiany,
            NumerZmiany = user.NumerZmiany,
            IsReadOnly  = user.IsReadOnly,
            CanEditAll  = user.Role == UserRole.DCAJRG
        };
    }

    /// <summary>CHOMIK: Base64(SHA256(UTF8(password + salt))). Obsługuje też legacy hex z lokalnej bazy SKRYBEK.</summary>
    public static bool VerifyChomikPassword(string password, string hash, string salt)
    {
        if (string.IsNullOrEmpty(hash)) return string.IsNullOrEmpty(password);

        var p = password.Trim();
        var h = hash.Trim();
        var s = salt.Trim();

        if (ComputeChomikHash(p, s) == h) return true;

        // Starszy format SKRYBEK bootstrappera: HEX(SHA256)
        return ComputeLegacyHexHash(p, s).Equals(h, StringComparison.OrdinalIgnoreCase);
    }

    public static string ComputeChomikHash(string password, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes(password.Trim() + salt.Trim());
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    private static string ComputeLegacyHexHash(string password, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes(password.Trim() + salt.Trim());
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    public static string GenerateSalt()
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(saltBytes);
    }
}

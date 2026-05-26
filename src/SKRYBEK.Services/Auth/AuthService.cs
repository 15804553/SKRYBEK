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

    /// <summary>Loguje użytkownika weryfikując hasło w bazie CHOMIK (Base64-SHA256).</summary>
    public async Task<SessionInfo?> LoginAsync(string login, string haslo)
    {
        var user = await _chomikAuth.GetByLoginAsync(login.Trim());
        if (user is null)
        {
            SkrybekLog.Warning($"Nieudana próba logowania: {login}");
            return null;
        }

        // Konto PA — może mieć puste hasło
        if (string.IsNullOrEmpty(user.HasloHash))
        {
            if (!string.IsNullOrEmpty(haslo))
            {
                SkrybekLog.Warning($"Konto PA nie wymaga hasła: {login}");
                return null;
            }
        }
        else
        {
            if (!VerifyChomikPassword(haslo, user.HasloHash, user.HasloSol))
            {
                SkrybekLog.Warning($"Błędne hasło dla użytkownika: {login}");
                return null;
            }
        }

        SkrybekLog.Info($"Zalogowano: {login} ({user.NazwaZmiany})");

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

    /// <summary>CHOMIK: Base64(SHA256(UTF8(password + salt)))</summary>
    public static bool VerifyChomikPassword(string password, string hash, string salt)
    {
        var computed = ComputeChomikHash(password, salt);
        return computed == hash;
    }

    public static string ComputeChomikHash(string password, string salt)
    {
        var bytes = Encoding.UTF8.GetBytes(password + salt);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    public static string GenerateSalt()
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(saltBytes);
    }
}

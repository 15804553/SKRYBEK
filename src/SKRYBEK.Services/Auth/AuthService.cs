using System.Security.Cryptography;
using System.Text;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Repositories;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Auth;

public sealed class AuthService
{
    private readonly AuthRepository _repo;

    public AuthService(AuthRepository repo)
    {
        _repo = repo;
    }

    public async Task<SessionInfo?> LoginAsync(string login, string haslo)
    {
        var user = await _repo.GetByLoginAsync(login.Trim().ToLower());
        if (user is null)
        {
            SkrybekLog.Warning($"Nieudana próba logowania: {login}");
            return null;
        }

        var hash = HashPassword(haslo, user.HasloSol);
        if (!hash.Equals(user.HasloHash, StringComparison.OrdinalIgnoreCase))
        {
            SkrybekLog.Warning($"Błędne hasło dla użytkownika: {login}");
            return null;
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

    public string GenerateSalt() => Guid.NewGuid().ToString("N");

    public string HashPassword(string password, string salt)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + salt);
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }

    public async Task ChangePasswordAsync(int userId, string noweHaslo)
    {
        var user = (await _repo.GetAllAsync()).FirstOrDefault(u => u.Id == userId)
            ?? throw new InvalidOperationException("Użytkownik nie istnieje.");

        user.HasloSol  = GenerateSalt();
        user.HasloHash = HashPassword(noweHaslo, user.HasloSol);
        await _repo.UpsertAsync(user);
        SkrybekLog.Info($"Zmieniono hasło użytkownika Id={userId}");
    }
}

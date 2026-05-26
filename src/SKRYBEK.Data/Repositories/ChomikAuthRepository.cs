using System.Data.OleDb;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;

namespace SKRYBEK.Data.Repositories;

/// <summary>
/// Czyta użytkowników z bazy CHOMIK i weryfikuje ich hasła zgodnie
/// z algorytmem CHOMIK: Base64(SHA256(UTF8(password + salt))).
/// </summary>
public sealed class ChomikAuthRepository
{
    private readonly ChomikConnectionFactory _factory;

    public ChomikAuthRepository(ChomikConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<UserAccount>> GetAllAsync()
    {
        var list = new List<UserAccount>();

        await using var conn = _factory.Create();
        await conn.OpenAsync();

        await using var cmd = new OleDbCommand(
            "SELECT Id, Login, Rola, NumerZmiany, HasloHash, HasloSol FROM Uzytkownicy ORDER BY Rola, Login", conn);
        await using var r = await cmd.ExecuteReaderAsync();

        while (await r.ReadAsync())
        {
            // 0=Id, 1=Login, 2=Rola, 3=NumerZmiany (Integer w CHOMIK), 4=HasloHash, 5=HasloSol
            var rolaNr = r.GetIntSafe(2);
            var role = MapRola(rolaNr);
            if (role is null) continue;

            list.Add(new UserAccount
            {
                Id          = r.GetIntSafe(0),
                Login       = r.GetStringSafe(1),
                Role        = role.Value,
                NumerZmiany = r.GetIntSafe(3),   // Integer, NULL → 0
                HasloHash   = r.GetStringSafe(4),
                HasloSol    = r.GetStringSafe(5)
            });
        }

        return list;
    }

    public async Task<UserAccount?> GetByLoginAsync(string login)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        await using var cmd = new OleDbCommand(
            "SELECT Id, Login, Rola, NumerZmiany, HasloHash, HasloSol FROM Uzytkownicy WHERE Login=?", conn);
        cmd.Parameters.AddWithValue("Login", login.Trim());
        await using var r = await cmd.ExecuteReaderAsync();

        if (!await r.ReadAsync()) return null;

        var rolaNr = r.GetIntSafe(2);
        var role = MapRola(rolaNr);
        if (role is null) return null;

        return new UserAccount
        {
            Id          = r.GetIntSafe(0),
            Login       = r.GetStringSafe(1),
            Role        = role.Value,
            NumerZmiany = r.GetIntSafe(3),   // Integer w CHOMIK
            HasloHash   = r.GetStringSafe(4),
            HasloSol    = r.GetStringSafe(5)
        };
    }

    /// <summary>Mapuje numer roli CHOMIK na UserRole SKRYBEK. Null = rola poza zakresem SKRYBEK.</summary>
    private static UserRole? MapRola(int chomikRola) => chomikRola switch
    {
        0 => UserRole.PA,
        1 => UserRole.Zmiana1,
        2 => UserRole.Zmiana2,
        3 => UserRole.Zmiana3,
        4 => UserRole.Zmiana3,   // Zmiana 4 → traktujemy jak Zmiana 3 (brak w SKRYBEK)
        5 => UserRole.DCAJRG,
        6 => UserRole.DCAJRG,    // Administrator
        _ => null
    };
}

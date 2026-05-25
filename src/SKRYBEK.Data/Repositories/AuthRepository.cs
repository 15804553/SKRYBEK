using System.Data.OleDb;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;

namespace SKRYBEK.Data.Repositories;

public sealed class AuthRepository
{
    private readonly SkrybekConnectionFactory _factory;

    public AuthRepository(SkrybekConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<UserAccount?> GetByLoginAsync(string login)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();
        await using var cmd = new OleDbCommand(
            "SELECT Id, Login, Rola, NumerZmiany, HasloHash, HasloSol FROM Uzytkownicy WHERE Login=?", conn);
        cmd.Parameters.AddWithValue("Login", login);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;

        return new UserAccount
        {
            Id           = r.GetIntSafe(0),
            Login        = r.GetStringSafe(1),
            Role         = (UserRole)r.GetIntSafe(2),
            NumerZmiany  = r.GetIntSafe(3),
            HasloHash    = r.GetStringSafe(4),
            HasloSol     = r.GetStringSafe(5)
        };
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
            list.Add(new UserAccount
            {
                Id          = r.GetIntSafe(0),
                Login       = r.GetStringSafe(1),
                Role        = (UserRole)r.GetIntSafe(2),
                NumerZmiany = r.GetIntSafe(3),
                HasloHash   = r.GetStringSafe(4),
                HasloSol    = r.GetStringSafe(5)
            });
        }
        return list;
    }

    public async Task UpsertAsync(UserAccount user)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        if (user.Id == 0)
        {
            await using var cmd = new OleDbCommand(
                "INSERT INTO Uzytkownicy (Login, Rola, NumerZmiany, HasloHash, HasloSol) VALUES (?, ?, ?, ?, ?)", conn);
            cmd.Parameters.AddWithValue("Login", user.Login);
            cmd.Parameters.AddWithValue("Rola", (int)user.Role);
            cmd.Parameters.AddWithValue("NumerZmiany", user.NumerZmiany);
            cmd.Parameters.AddWithValue("HasloHash", user.HasloHash);
            cmd.Parameters.AddWithValue("HasloSol", user.HasloSol);
            await cmd.ExecuteNonQueryAsync();
        }
        else
        {
            await using var cmd = new OleDbCommand(
                "UPDATE Uzytkownicy SET Login=?, Rola=?, NumerZmiany=?, HasloHash=?, HasloSol=? WHERE Id=?", conn);
            cmd.Parameters.AddWithValue("Login", user.Login);
            cmd.Parameters.AddWithValue("Rola", (int)user.Role);
            cmd.Parameters.AddWithValue("NumerZmiany", user.NumerZmiany);
            cmd.Parameters.AddWithValue("HasloHash", user.HasloHash);
            cmd.Parameters.AddWithValue("HasloSol", user.HasloSol);
            cmd.Parameters.AddWithValue("Id", user.Id);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();
        await using var cmd = new OleDbCommand("DELETE FROM Uzytkownicy WHERE Id=?", conn);
        cmd.Parameters.AddWithValue("Id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}

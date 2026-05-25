using System.Data.OleDb;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;

namespace SKRYBEK.Data.Repositories;

public sealed class UstawieniaRepository
{
    private readonly SkrybekConnectionFactory _factory;

    public UstawieniaRepository(SkrybekConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Dictionary<string, string>> GetAllAsync()
    {
        var result = new Dictionary<string, string>();
        await using var conn = _factory.Create();
        await conn.OpenAsync();
        await using var cmd = new OleDbCommand("SELECT Klucz, Wartosc FROM Ustawienia", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            result[r.GetString(0)] = r.GetStringSafe(1);
        return result;
    }

    public async Task<string> GetAsync(string klucz, string domyslna = "")
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();
        await using var cmd = new OleDbCommand("SELECT Wartosc FROM Ustawienia WHERE Klucz=?", conn);
        cmd.Parameters.AddWithValue("Klucz", klucz);
        var val = await cmd.ExecuteScalarAsync();
        return val is null or DBNull ? domyslna : val.ToString()!;
    }

    public async Task SetAsync(string klucz, string wartosc)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        await using var check = new OleDbCommand("SELECT COUNT(*) FROM Ustawienia WHERE Klucz=?", conn);
        check.Parameters.AddWithValue("Klucz", klucz);
        var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;

        if (exists)
        {
            await using var upd = new OleDbCommand("UPDATE Ustawienia SET Wartosc=? WHERE Klucz=?", conn);
            upd.Parameters.AddWithValue("Wartosc", wartosc);
            upd.Parameters.AddWithValue("Klucz", klucz);
            await upd.ExecuteNonQueryAsync();
        }
        else
        {
            await using var ins = new OleDbCommand("INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)", conn);
            ins.Parameters.AddWithValue("Klucz", klucz);
            ins.Parameters.AddWithValue("Wartosc", wartosc);
            await ins.ExecuteNonQueryAsync();
        }
    }
}

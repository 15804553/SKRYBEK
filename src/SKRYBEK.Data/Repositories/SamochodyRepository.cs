using System.Data.OleDb;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;

namespace SKRYBEK.Data.Repositories;

public sealed class SamochodyRepository
{
    private readonly SkrybekConnectionFactory _factory;

    public SamochodyRepository(SkrybekConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Samochod>> GetAllAsync()
    {
        var list = new List<Samochod>();
        await using var conn = _factory.Create();
        await conn.OpenAsync();
        await using var cmd = new OleDbCommand(
            "SELECT Id, Nazwa, LiczbaPozycji, Typ, Kolejnosc, CzyAktywny FROM Samochody ORDER BY Kolejnosc", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new Samochod
            {
                Id            = r.GetIntSafe(0),
                Nazwa         = r.GetStringSafe(1),
                LiczbaPozycji = r.GetIntSafe(2),
                Typ           = (TypSamochodu)r.GetIntSafe(3),
                Kolejnosc     = r.GetIntSafe(4),
                CzyAktywny    = r.GetBoolSafe(5)
            });
        }
        await r.CloseAsync();

        await AttachWymaganiaAsync(conn, list);
        return list;
    }

    private static async Task AttachWymaganiaAsync(OleDbConnection conn, List<Samochod> list)
    {
        if (list.Count == 0) return;
        await using var cmd = new OleDbCommand(
            "SELECT SamochodId, TypUprawnieniaId FROM SamochodWymagania", conn);
        try
        {
            await using var r = await cmd.ExecuteReaderAsync();
            var dict = list.ToDictionary(s => s.Id);
            while (await r.ReadAsync())
            {
                var samId = r.GetIntSafe(0);
                var typId = r.GetIntSafe(1);
                if (dict.TryGetValue(samId, out var s))
                    s.WymaganeUprawnieniaIds.Add(typId);
            }
        }
        catch
        {
            // Tabela SamochodWymagania może jeszcze nie istnieć na starych bazach
        }
    }

    public async Task<List<Samochod>> GetAktywneAsync()
    {
        var all = await GetAllAsync();
        return all.Where(s => s.CzyAktywny).ToList();
    }

    public async Task UpsertAsync(Samochod s)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        if (s.Id == 0)
        {
            await using var cmd = new OleDbCommand(
                "INSERT INTO Samochody (Nazwa, LiczbaPozycji, Typ, Kolejnosc, CzyAktywny) VALUES (?, ?, ?, ?, ?)", conn);
            AddParams(cmd, s);
            await cmd.ExecuteNonQueryAsync();

            await using var idCmd = new OleDbCommand("SELECT @@IDENTITY", conn);
            s.Id = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
        }
        else
        {
            await using var cmd = new OleDbCommand(
                "UPDATE Samochody SET Nazwa=?, LiczbaPozycji=?, Typ=?, Kolejnosc=?, CzyAktywny=? WHERE Id=?", conn);
            AddParams(cmd, s);
            cmd.Parameters.AddWithValue("Id", s.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        await SaveWymaganiaAsync(conn, s);
    }

    private static async Task SaveWymaganiaAsync(OleDbConnection conn, Samochod s)
    {
        if (s.Id == 0) return;
        try
        {
            await using var del = new OleDbCommand("DELETE FROM SamochodWymagania WHERE SamochodId=?", conn);
            del.Parameters.AddWithValue("SamochodId", s.Id);
            await del.ExecuteNonQueryAsync();

            foreach (var typId in s.WymaganeUprawnieniaIds)
            {
                await using var ins = new OleDbCommand(
                    "INSERT INTO SamochodWymagania (SamochodId, TypUprawnieniaId) VALUES (?, ?)", conn);
                ins.Parameters.AddWithValue("SamochodId", s.Id);
                ins.Parameters.AddWithValue("TypUprawnieniaId", typId);
                await ins.ExecuteNonQueryAsync();
            }
        }
        catch
        {
            // Tabela SamochodWymagania może jeszcze nie istnieć
        }
    }

    public async Task DeleteAsync(int id)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        await using var delWym = new OleDbCommand("DELETE FROM SamochodWymagania WHERE SamochodId=?", conn);
        delWym.Parameters.AddWithValue("SamochodId", id);
        try { await delWym.ExecuteNonQueryAsync(); } catch { /* stara baza bez tabeli */ }

        await using var cmd = new OleDbCommand("DELETE FROM Samochody WHERE Id=?", conn);
        cmd.Parameters.AddWithValue("Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    private static void AddParams(OleDbCommand cmd, Samochod s)
    {
        cmd.Parameters.AddWithValue("Nazwa", s.Nazwa);
        cmd.Parameters.AddWithValue("LiczbaPozycji", s.LiczbaPozycji);
        cmd.Parameters.AddWithValue("Typ", (int)s.Typ);
        cmd.Parameters.AddWithValue("Kolejnosc", s.Kolejnosc);
        cmd.Parameters.AddWithValue("CzyAktywny", s.CzyAktywny);
    }
}

using System.Data.Common;
using System.Data.OleDb;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;

namespace SKRYBEK.Data.Repositories;

public sealed class RozkazRepository
{
    private readonly SkrybekConnectionFactory _factory;

    public RozkazRepository(SkrybekConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<RozkazDzienny>> GetByRokAsync(int rok)
    {
        var list = new List<RozkazDzienny>();
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        await using var cmd = new OleDbCommand(
            "SELECT Id, NumerRozkazu, Rok, Data, ZmianaId, Zajecia, Uwagi, DataUtworzenia, Status FROM Rozkazy WHERE Rok=? ORDER BY Data DESC",
            conn);
        cmd.Parameters.AddWithValue("Rok", rok);
        await using var r = await cmd.ExecuteReaderAsync();

        while (await r.ReadAsync())
            list.Add(MapRozkaz(r));

        return list;
    }

    public async Task<RozkazDzienny?> GetByIdAsync(int id)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        await using var cmd = new OleDbCommand(
            "SELECT Id, NumerRozkazu, Rok, Data, ZmianaId, Zajecia, Uwagi, DataUtworzenia, Status FROM Rozkazy WHERE Id=?", conn);
        cmd.Parameters.AddWithValue("Id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;

        var rozkaz = MapRozkaz(r);
        await r.CloseAsync();

        rozkaz.Sluzba          = await GetSluzbaAsync(conn, id);
        rozkaz.PodzialBojowy   = await GetPodzialBojowyAsync(conn, id);
        rozkaz.RatwnicyMedyczni = await GetRatwnicyMedyczniAsync(conn, id);
        rozkaz.Nieobecni       = await GetNieobecniAsync(conn, id);

        return rozkaz;
    }

    public async Task<int> SaveAsync(RozkazDzienny rozkaz)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        int rozkazId;
        try
        {
            if (rozkaz.Id == 0)
            {
                await using var cmd = new OleDbCommand(
                    "INSERT INTO Rozkazy (NumerRozkazu, Rok, Data, ZmianaId, Zajecia, Uwagi, DataUtworzenia, Status) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                    conn);
                AddRozkazParams(cmd, rozkaz);
                await cmd.ExecuteNonQueryAsync();

                await using var idCmd = new OleDbCommand("SELECT @@IDENTITY", conn);
                rozkazId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
                rozkaz.Id = rozkazId;
            }
            else
            {
                rozkazId = rozkaz.Id;
                await using var cmd = new OleDbCommand(
                    "UPDATE Rozkazy SET NumerRozkazu=?, Rok=?, Data=?, ZmianaId=?, Zajecia=?, Uwagi=?, DataUtworzenia=?, Status=? WHERE Id=?",
                    conn);
                AddRozkazParams(cmd, rozkaz);
                cmd.Parameters.AddInteger(rozkaz.Id);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Błąd zapisu nagłówka rozkazu: {ex.Message}", ex);
        }

        try { await SaveSluzbaAsync(conn, rozkazId, rozkaz.Sluzba); }
        catch (Exception ex) { throw new InvalidOperationException($"Błąd zapisu sekcji SŁUŻBA: {ex.Message}", ex); }

        try { await SavePodzialBojowyAsync(conn, rozkazId, rozkaz.PodzialBojowy); }
        catch (Exception ex) { throw new InvalidOperationException($"Błąd zapisu podziału bojowego: {ex.Message}", ex); }

        try { await SaveRatwnicyMedyczniAsync(conn, rozkazId, rozkaz.RatwnicyMedyczni); }
        catch (Exception ex) { throw new InvalidOperationException($"Błąd zapisu ratowników medycznych: {ex.Message}", ex); }

        try { await SaveNieobecniAsync(conn, rozkazId, rozkaz.Nieobecni); }
        catch (Exception ex) { throw new InvalidOperationException($"Błąd zapisu nieobecnych: {ex.Message}", ex); }

        return rozkazId;
    }

    public async Task DeleteAsync(int id)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();
        foreach (var tbl in new[] { "RozkazSluzba", "RozkazPodzialBojowy", "RozkazRatwnicyMedyczni", "RozkazNieobecni" })
        {
            await using var del = new OleDbCommand($"DELETE FROM {tbl} WHERE RozkazId=?", conn);
            del.Parameters.AddWithValue("RozkazId", id);
            await del.ExecuteNonQueryAsync();
        }
        await using var delMain = new OleDbCommand("DELETE FROM Rozkazy WHERE Id=?", conn);
        delMain.Parameters.AddWithValue("Id", id);
        await delMain.ExecuteNonQueryAsync();
    }

    public async Task<int> GetNastepnyNumerAsync(int rok)
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();
        await using var cmd = new OleDbCommand("SELECT MAX(NumerRozkazu) FROM Rozkazy WHERE Rok=?", conn);
        cmd.Parameters.AddWithValue("Rok", rok);
        var val = await cmd.ExecuteScalarAsync();
        return val is null or DBNull ? 1 : Convert.ToInt32(val) + 1;
    }

    // ── Sekcja SŁUŻBA ─────────────────────────────────────────────────────────

    private static async Task<List<PozycjaSluzby>> GetSluzbaAsync(OleDbConnection conn, int rozkazId)
    {
        var list = new List<PozycjaSluzby>();
        await using var cmd = new OleDbCommand(
            "SELECT Id, RozkazId, Stanowisko, FunkcjonariuszId, Nazwisko FROM RozkazSluzba WHERE RozkazId=? ORDER BY Stanowisko",
            conn);
        cmd.Parameters.AddWithValue("RozkazId", rozkazId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new PozycjaSluzby
            {
                Id               = r.GetIntSafe(0),
                RozkazId         = r.GetIntSafe(1),
                Stanowisko       = (StanowiskoSluzby)r.GetIntSafe(2),
                FunkcjonariuszId = r.GetIntOrNull(3),
                Nazwisko         = r.GetStringSafe(4)
            });
        }
        return list;
    }

    private static async Task SaveSluzbaAsync(OleDbConnection conn, int rozkazId, List<PozycjaSluzby> sluzba)
    {
        await using var del = new OleDbCommand("DELETE FROM RozkazSluzba WHERE RozkazId=?", conn);
        del.Parameters.AddWithValue("RozkazId", rozkazId);
        await del.ExecuteNonQueryAsync();

        foreach (var p in sluzba)
        {
            await using var ins = new OleDbCommand(
                "INSERT INTO RozkazSluzba (RozkazId, Stanowisko, FunkcjonariuszId, Nazwisko) VALUES (?, ?, ?, ?)", conn);
            ins.Parameters.AddInteger(rozkazId);
            ins.Parameters.AddSmallInt((int)p.Stanowisko);
            ins.Parameters.AddNullableInteger(p.FunkcjonariuszId);
            ins.Parameters.AddText(p.Nazwisko);
            await ins.ExecuteNonQueryAsync();
        }
    }

    // ── Podział bojowy ────────────────────────────────────────────────────────

    private static async Task<List<PozycjaSamochodu>> GetPodzialBojowyAsync(OleDbConnection conn, int rozkazId)
    {
        var list = new List<PozycjaSamochodu>();
        await using var cmd = new OleDbCommand(
            "SELECT Id, RozkazId, SamochodId, Pozycja, FunkcjonariuszId, Nazwisko FROM RozkazPodzialBojowy WHERE RozkazId=? ORDER BY SamochodId, Pozycja",
            conn);
        cmd.Parameters.AddWithValue("RozkazId", rozkazId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new PozycjaSamochodu
            {
                Id               = r.GetIntSafe(0),
                RozkazId         = r.GetIntSafe(1),
                SamochodId       = r.GetIntSafe(2),
                Pozycja          = r.GetIntSafe(3),
                FunkcjonariuszId = r.GetIntOrNull(4),
                Nazwisko         = r.GetStringSafe(5)
            });
        }
        return list;
    }

    private static async Task SavePodzialBojowyAsync(OleDbConnection conn, int rozkazId, List<PozycjaSamochodu> podział)
    {
        await using var del = new OleDbCommand("DELETE FROM RozkazPodzialBojowy WHERE RozkazId=?", conn);
        del.Parameters.AddWithValue("RozkazId", rozkazId);
        await del.ExecuteNonQueryAsync();

        foreach (var p in podział)
        {
            await using var ins = new OleDbCommand(
                "INSERT INTO RozkazPodzialBojowy (RozkazId, SamochodId, Pozycja, FunkcjonariuszId, Nazwisko) VALUES (?, ?, ?, ?, ?)", conn);
            ins.Parameters.AddInteger(rozkazId);
            ins.Parameters.AddInteger(p.SamochodId);
            ins.Parameters.AddSmallInt(p.Pozycja);
            ins.Parameters.AddNullableInteger(p.FunkcjonariuszId);
            ins.Parameters.AddText(p.Nazwisko);
            await ins.ExecuteNonQueryAsync();
        }
    }

    // ── Ratownicy medyczni ────────────────────────────────────────────────────

    private static async Task<List<RatownikMedyczny>> GetRatwnicyMedyczniAsync(OleDbConnection conn, int rozkazId)
    {
        var list = new List<RatownikMedyczny>();
        await using var cmd = new OleDbCommand(
            "SELECT Id, RozkazId, Pozycja, FunkcjonariuszId, Nazwisko FROM RozkazRatwnicyMedyczni WHERE RozkazId=? ORDER BY Pozycja",
            conn);
        cmd.Parameters.AddWithValue("RozkazId", rozkazId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new RatownikMedyczny
            {
                Id               = r.GetIntSafe(0),
                RozkazId         = r.GetIntSafe(1),
                Pozycja          = r.GetIntSafe(2),
                FunkcjonariuszId = r.GetIntOrNull(3),
                Nazwisko         = r.GetStringSafe(4)
            });
        }
        return list;
    }

    private static async Task SaveRatwnicyMedyczniAsync(OleDbConnection conn, int rozkazId, List<RatownikMedyczny> ratownicy)
    {
        await using var del = new OleDbCommand("DELETE FROM RozkazRatwnicyMedyczni WHERE RozkazId=?", conn);
        del.Parameters.AddWithValue("RozkazId", rozkazId);
        await del.ExecuteNonQueryAsync();

        foreach (var p in ratownicy)
        {
            await using var ins = new OleDbCommand(
                "INSERT INTO RozkazRatwnicyMedyczni (RozkazId, Pozycja, FunkcjonariuszId, Nazwisko) VALUES (?, ?, ?, ?)", conn);
            ins.Parameters.AddInteger(rozkazId);
            ins.Parameters.AddSmallInt(p.Pozycja);
            ins.Parameters.AddNullableInteger(p.FunkcjonariuszId);
            ins.Parameters.AddText(p.Nazwisko);
            await ins.ExecuteNonQueryAsync();
        }
    }

    // ── Nieobecni ─────────────────────────────────────────────────────────────

    private static async Task<List<NieobecnyWSluzbie>> GetNieobecniAsync(OleDbConnection conn, int rozkazId)
    {
        var list = new List<NieobecnyWSluzbie>();
        await using var cmd = new OleDbCommand(
            "SELECT Id, RozkazId, FunkcjonariuszId, Nazwisko, TypNieobecnosci FROM RozkazNieobecni WHERE RozkazId=? ORDER BY TypNieobecnosci, Nazwisko",
            conn);
        cmd.Parameters.AddWithValue("RozkazId", rozkazId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new NieobecnyWSluzbie
            {
                Id               = r.GetIntSafe(0),
                RozkazId         = r.GetIntSafe(1),
                FunkcjonariuszId = r.GetIntOrNull(2),
                Nazwisko         = r.GetStringSafe(3),
                TypNieobecnosci  = (TypNieobecnosci)r.GetIntSafe(4)
            });
        }
        return list;
    }

    private static async Task SaveNieobecniAsync(OleDbConnection conn, int rozkazId, List<NieobecnyWSluzbie> nieobecni)
    {
        await using var del = new OleDbCommand("DELETE FROM RozkazNieobecni WHERE RozkazId=?", conn);
        del.Parameters.AddWithValue("RozkazId", rozkazId);
        await del.ExecuteNonQueryAsync();

        foreach (var n in nieobecni)
        {
            await using var ins = new OleDbCommand(
                "INSERT INTO RozkazNieobecni (RozkazId, FunkcjonariuszId, Nazwisko, TypNieobecnosci) VALUES (?, ?, ?, ?)", conn);
            ins.Parameters.AddInteger(rozkazId);
            ins.Parameters.AddNullableInteger(n.FunkcjonariuszId);
            ins.Parameters.AddText(n.Nazwisko);
            ins.Parameters.AddSmallInt((int)n.TypNieobecnosci);
            await ins.ExecuteNonQueryAsync();
        }
    }

    // ── Mapowanie ─────────────────────────────────────────────────────────────

    private static RozkazDzienny MapRozkaz(DbDataReader r)
    {
        var data = r.GetDateTime(3);
        return new RozkazDzienny
        {
            Id             = r.GetIntSafe(0),
            NumerRozkazu   = r.GetIntSafe(1),
            Rok            = r.GetIntSafe(2),
            Data           = DateOnly.FromDateTime(data),
            ZmianaId       = r.GetIntSafe(4),
            Zajecia        = r.GetStringSafe(5),
            Uwagi          = r.GetStringSafe(6),
            DataUtworzenia = r.IsDBNull(7) ? DateTime.Now : r.GetDateTime(7),
            Status         = (StatusRozkazu)r.GetIntSafe(8)
        };
    }

    private static void AddRozkazParams(OleDbCommand cmd, RozkazDzienny rozkaz)
    {
        cmd.Parameters.AddInteger(rozkaz.NumerRozkazu);
        cmd.Parameters.AddInteger(rozkaz.Rok);
        cmd.Parameters.AddDate(rozkaz.Data.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddSmallInt(rozkaz.ZmianaId);
        cmd.Parameters.AddMemo(rozkaz.Zajecia);
        cmd.Parameters.AddMemo(rozkaz.Uwagi);
        cmd.Parameters.AddDate(rozkaz.DataUtworzenia);
        cmd.Parameters.AddSmallInt((int)rozkaz.Status);
    }
}

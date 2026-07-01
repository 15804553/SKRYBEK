using System.Data.OleDb;
using SKRYBEK.Core.Chomik;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;
using SKRYBEK.Data.Grafik;

namespace SKRYBEK.Data.Repositories;

/// <summary>
/// Personel, stanowiska i uprawnienia — z bazy CHOMIK.
/// Dostępność w danym dniu (grafik, nieobecności) — z bazy BOBER.
/// </summary>
public sealed class PersonnelRepository
{
    private readonly BoberConnectionFactory _bober;
    private readonly ChomikConnectionFactory _chomik;
    private readonly ShiftCalendarEngine _calendar;

    public PersonnelRepository(
        BoberConnectionFactory bober,
        ChomikConnectionFactory chomik,
        ShiftCalendarEngine calendar)
    {
        _bober    = bober;
        _chomik   = chomik;
        _calendar = calendar;
    }

    /// <summary>Zwraca wszystkich funkcjonariuszy danej zmiany z danymi z CHOMIK.</summary>
    public async Task<List<Funkcjonariusz>> GetByZmianaAsync(int nrZmiany)
    {
        var list = new List<Funkcjonariusz>();

        await using var conn = _chomik.Create();
        await conn.OpenAsync();

        const string sql =
            "SELECT f.Id, f.NumerZmiany, f.StopienId, f.Imie, f.Nazwisko, f.StanowiskoId, f.Telefon, f.StazLat," +
            " ss.Nazwa AS Stopien, st.Nazwa AS Stanowisko" +
            " FROM (Funkcjonariusze AS f INNER JOIN StopnieSlownik AS ss ON ss.Id = f.StopienId)" +
            " INNER JOIN StanowiskaSlownik AS st ON st.Id = f.StanowiskoId" +
            " WHERE f.NumerZmiany = ?" +
            " ORDER BY f.Id";

        await using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.AddWithValue("@p1", (short)nrZmiany);
        await using var r = await cmd.ExecuteReaderAsync();

        while (await r.ReadAsync())
        {
            list.Add(new Funkcjonariusz
            {
                Id           = r.GetIntSafe(0),
                NumerZmiany  = r.GetIntSafe(1),
                StopienId    = r.GetIntSafe(2),
                Imie         = r.GetStringSafe(3),
                Nazwisko     = r.GetStringSafe(4),
                StanowiskoId = r.GetIntSafe(5),
                Telefon      = r.IsDBNull(6) ? null : r.GetStringSafe(6),
                StazLat      = r.GetIntOrNull(7),
                Stopien      = r.GetStringSafe(8),
                Stanowisko   = r.GetStringSafe(9)
            });
        }

        await AttachUprawnieniaAsync(conn, list);
        await AttachFunkcjeDodatkoweAsync(conn, list);

        return list;
    }

    /// <summary>
    /// Zwraca funkcjonariuszy obecnych w danym dniu służby zmiany.
    /// W BOBER nieobecność = dowolny wpis w GrafikWpisy (pusta komórka = w pracy).
    /// </summary>
    public async Task<List<Funkcjonariusz>> GetDostepniWDniuAsync(
        DateOnly data,
        int nrZmiany,
        IReadOnlyList<Funkcjonariusz>? wszyscyZmiany = null)
    {
        var wszyscy = wszyscyZmiany?.ToList() ?? await GetByZmianaAsync(nrZmiany);
        if (wszyscy.Count == 0)
            return wszyscy;

        if (!await _calendar.IsWorkDayAsync(nrZmiany, data))
            return [];

        if (string.IsNullOrWhiteSpace(_bober.DatabasePath))
            return wszyscy;

        try
        {
            var nieobecniIds = await PobierzNieobecnychZBoberAsync(data, nrZmiany);
            return wszyscy.Where(f => !nieobecniIds.Contains(f.Id)).ToList();
        }
        catch
        {
            return wszyscy;
        }
    }

    private async Task<HashSet<int>> PobierzNieobecnychZBoberAsync(DateOnly data, int nrZmiany)
    {
        await using var boberConn = _bober.Create();
        await boberConn.OpenAsync();

        const string sql = """
            SELECT FunkcjonariuszId FROM GrafikWpisy
            WHERE ZmianaId=? AND Rok=? AND Miesiac=? AND Dzien=?
            """;

        await using var cmd = new OleDbCommand(sql, boberConn);
        cmd.Parameters.AddWithValue("@p1", (short)nrZmiany);
        cmd.Parameters.AddWithValue("@p2", (short)data.Year);
        cmd.Parameters.AddWithValue("@p3", (short)data.Month);
        cmd.Parameters.AddWithValue("@p4", (short)data.Day);
        await using var reader = await cmd.ExecuteReaderAsync();

        var nieobecniIds = new HashSet<int>();
        while (await reader.ReadAsync())
            nieobecniIds.Add(reader.GetIntSafe(0));

        return nieobecniIds;
    }

    /// <summary>
    /// Pobiera nieobecnych z BOBER wraz z typem nieobecności.
    /// Kolumna TypWpisu w GrafikWpisy przechowuje kody: U, Del, WS, D, DD, L4 itp.
    /// </summary>
    public async Task<List<(int FunkcjonariuszId, Core.Enums.TypNieobecnosci Typ)>> PobierzNieobecnychZTypemAsync(
        DateOnly data, int nrZmiany)
    {
        if (string.IsNullOrWhiteSpace(_bober.DatabasePath))
            return [];

        try
        {
            await using var conn = _bober.Create();
            await conn.OpenAsync();

            if (!await _calendar.IsWorkDayAsync(nrZmiany, data))
                return [];

            const string sqlZTypem = """
                SELECT FunkcjonariuszId, TypWpisu FROM GrafikWpisy
                WHERE ZmianaId=? AND Rok=? AND Miesiac=? AND Dzien=?
                """;

            await using var cmd = new OleDbCommand(sqlZTypem, conn);
            cmd.Parameters.AddWithValue("@p1", (short)nrZmiany);
            cmd.Parameters.AddWithValue("@p2", (short)data.Year);
            cmd.Parameters.AddWithValue("@p3", (short)data.Month);
            cmd.Parameters.AddWithValue("@p4", (short)data.Day);

            try
            {
                await using var reader = await cmd.ExecuteReaderAsync();
                var wynik = new List<(int, Core.Enums.TypNieobecnosci)>();
                while (await reader.ReadAsync())
                {
                    var fid = reader.GetIntSafe(0);
                    // Kolumna TypWpisu przechowuje kod tekstowy: "U", "Del", "WS", "D" itp.
                    var typStr = reader.IsDBNull(1) ? null : reader.GetStringSafe(1);
                    var typ = MapBoberTypNieobecnosci(typStr);
                    wynik.Add((fid, typ));
                }
                return wynik;
            }
            catch
            {
                // Fallback gdy zapytanie z TypWpisu zawiedzie z nieoczekiwanego powodu
                const string sqlBezTypu = """
                    SELECT FunkcjonariuszId FROM GrafikWpisy
                    WHERE ZmianaId=? AND Rok=? AND Miesiac=? AND Dzien=?
                    """;
                await using var cmd2 = new OleDbCommand(sqlBezTypu, conn);
                cmd2.Parameters.AddWithValue("@p1", (short)nrZmiany);
                cmd2.Parameters.AddWithValue("@p2", (short)data.Year);
                cmd2.Parameters.AddWithValue("@p3", (short)data.Month);
                cmd2.Parameters.AddWithValue("@p4", (short)data.Day);
                await using var r2 = await cmd2.ExecuteReaderAsync();
                var wynik = new List<(int, Core.Enums.TypNieobecnosci)>();
                while (await r2.ReadAsync())
                    wynik.Add((r2.GetIntSafe(0), Core.Enums.TypNieobecnosci.CzasWolny));
                return wynik;
            }
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Mapuje wartość z kolumny TypNieobecnosci w BOBER na wewnętrzny enum.
    /// BOBER może przechowywać kody tekstowe (U, Del, D, WS, L4, NW...) lub liczby (1-5).
    /// </summary>
    private static Core.Enums.TypNieobecnosci MapBoberTypNieobecnosci(string? kod)
    {
        if (string.IsNullOrWhiteSpace(kod))
            return Core.Enums.TypNieobecnosci.CzasWolny;

        return kod.Trim().ToUpperInvariant() switch
        {
            // Kody tekstowe PSP — urlop
            "U" or "URL" or "URLOP" or "UR"
                => Core.Enums.TypNieobecnosci.Urlop,

            // Kody tekstowe PSP — delegacja służbowa
            "DEL" or "DELEGACJA" or "DELEG" or "DG"
                => Core.Enums.TypNieobecnosci.Delegowany,

            // Kody tekstowe PSP — choroba / zwolnienie L4
            "CH" or "CHORY" or "CHORA" or "L4" or "ZW" or "ZWOLNIENIE"
                => Core.Enums.TypNieobecnosci.Chory,

            // Kody tekstowe PSP — dyżur
            "D" or "DD" or "DYZ" or "DYZUR" or "DYZURD" or "DYŻ" or "DYŻUR" or "DYŻURD"
                => Core.Enums.TypNieobecnosci.DyzurDomowy,

            // Kody tekstowe PSP — wolna służba / czas wolny
            "WS" or "W" or "WOL" or "WOLNY" or "WOLNA" or "CW" or "CWASLUZBY"
                => Core.Enums.TypNieobecnosci.CzasWolny,

            // Wartości liczbowe zapisane jako tekst
            "1" => Core.Enums.TypNieobecnosci.Urlop,
            "2" => Core.Enums.TypNieobecnosci.CzasWolny,
            "3" => Core.Enums.TypNieobecnosci.Chory,
            "4" => Core.Enums.TypNieobecnosci.Delegowany,
            "5" => Core.Enums.TypNieobecnosci.DyzurDomowy,

            _ => Core.Enums.TypNieobecnosci.CzasWolny
        };
    }

    /// <summary>Pobiera wszystkie typy uprawnień ze słownika CHOMIK.</summary>
    public async Task<List<(int Id, string Nazwa)>> GetTypyUprawnienAsync()
    {
        var list = new List<(int, string)>();
        try
        {
            await using var conn = _chomik.Create();
            await conn.OpenAsync();
            await using var cmd = new OleDbCommand(
                "SELECT Id, Nazwa FROM TypyUprawnien ORDER BY Nazwa", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add((r.GetIntSafe(0), r.GetStringSafe(1)));
        }
        catch { /* CHOMIK niedostępny lub brak tabeli */ }
        return list;
    }

    private static async Task AttachUprawnieniaAsync(OleDbConnection conn, List<Funkcjonariusz> list)
    {
        if (list.Count == 0) return;
        var ids = string.Join(",", list.Select(f => f.Id));
        var sql = $"""
            SELECT fu.FunkcjonariuszId, fu.TypUprawnieniaId, tu.Nazwa, tu.Podtyp
            FROM FunkcjonariuszUprawnienia fu
            INNER JOIN TypyUprawnien tu ON tu.Id = fu.TypUprawnieniaId
            WHERE fu.FunkcjonariuszId IN ({ids})
            """;
        await using var cmd = new OleDbCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync();
        var dict = list.ToDictionary(f => f.Id);
        while (await r.ReadAsync())
        {
            var fid = r.GetIntSafe(0);
            if (!dict.TryGetValue(fid, out var f)) continue;

            var typId  = r.GetIntSafe(1);
            var nazwa  = r.GetStringSafe(2);
            var podtyp = r.IsDBNull(3) ? null : r.GetStringSafe(3);

            f.IdUprawnien.Add(typId);
            f.NazwyUprawnien.Add(ChomikSlowniki.FormatUprawnienie(nazwa, podtyp));
        }
    }

    private static async Task AttachFunkcjeDodatkoweAsync(OleDbConnection conn, List<Funkcjonariusz> list)
    {
        if (list.Count == 0) return;
        var ids = string.Join(",", list.Select(f => f.Id));
        var sql = $"""
            SELECT ff.FunkcjonariuszId, fd.Nazwa
            FROM FunkcjonariuszFunkcjeDodatkowe ff
            INNER JOIN FunkcjeDodatkoweSlownik fd ON fd.Id = ff.FunkcjaDodatkowaId
            WHERE ff.FunkcjonariuszId IN ({ids})
            """;
        await using var cmd = new OleDbCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync();
        var dict = list.ToDictionary(f => f.Id);
        while (await r.ReadAsync())
        {
            var fid = r.GetIntSafe(0);
            if (dict.TryGetValue(fid, out var f))
                f.NazwyFunkcjiDodatkowych.Add(r.GetStringSafe(1));
        }
    }
}

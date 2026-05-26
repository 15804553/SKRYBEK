using System.Data.OleDb;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;

namespace SKRYBEK.Data.Repositories;

/// <summary>
/// Odczytuje listę dostępnych funkcjonariuszy z bazy BOBER (grafik)
/// i wzbogaca ich dane o uprawnienia z bazy CHOMIK.
/// </summary>
public sealed class PersonnelRepository
{
    private readonly BoberConnectionFactory _bober;
    private readonly ChomikConnectionFactory _chomik;

    public PersonnelRepository(BoberConnectionFactory bober, ChomikConnectionFactory chomik)
    {
        _bober  = bober;
        _chomik = chomik;
    }

    /// <summary>Zwraca wszystkich funkcjonariuszy danej zmiany z danymi z CHOMIK.</summary>
    public async Task<List<Funkcjonariusz>> GetByZmianaAsync(int nrZmiany)
    {
        var list = new List<Funkcjonariusz>();

        await using var conn = _chomik.Create();
        await conn.OpenAsync();

        // Access/JET wymaga nawiasów przy wielu złączeniach — bez nawiasów silnik traktuje
        // drugi INNER JOIN jako kontynuację warunku ON pierwszego złączenia.
        const string sql =
            "SELECT f.Id, f.NumerZmiany, f.StopienId, f.Imie, f.Nazwisko, f.StanowiskoId, f.Telefon, f.StazLat," +
            " ss.Nazwa AS Stopien, st.Nazwa AS Stanowisko" +
            " FROM (Funkcjonariusze AS f INNER JOIN StopnieSlownik AS ss ON ss.Id = f.StopienId)" +
            " INNER JOIN StanowiskaSlownik AS st ON st.Id = f.StanowiskoId" +
            " WHERE f.NumerZmiany = ?" +
            " ORDER BY f.Nazwisko, f.Imie";

        await using var cmd = new OleDbCommand(sql, conn);
        cmd.Parameters.AddWithValue("NumerZmiany", nrZmiany);
        await using var r = await cmd.ExecuteReaderAsync();

        while (await r.ReadAsync())
        {
            list.Add(new Funkcjonariusz
            {
                Id          = r.GetIntSafe(0),
                NumerZmiany = r.GetIntSafe(1),
                StopienId   = r.GetIntSafe(2),
                Imie        = r.GetStringSafe(3),
                Nazwisko    = r.GetStringSafe(4),
                StanowiskoId = r.GetIntSafe(5),
                Telefon     = r.IsDBNull(6) ? null : r.GetStringSafe(6),
                StazLat     = r.GetIntOrNull(7),
                Stopien     = r.GetStringSafe(8),
                Stanowisko  = r.GetStringSafe(9)
            });
        }

        await AttachUprawnieniaAsync(conn, list);
        await AttachFunkcjeDodatkoweAsync(conn, list);

        return list;
    }

    /// <summary>
    /// Zwraca funkcjonariuszy na dyżurze (typ "D") w danym dniu z bazy BOBER,
    /// wzbogaconych o dane z CHOMIK.
    /// </summary>
    public async Task<List<Funkcjonariusz>> GetDostepniWDniuAsync(DateOnly data, int nrZmiany)
    {
        // Próba odczytania GrafikWpisy z BOBER — jeśli baza nieosiągalna, zwraca wszystkich
        var wszyscy = await GetByZmianaAsync(nrZmiany);

        try
        {
            await using var boberConn = _bober.Create();
            await boberConn.OpenAsync();

            // Osoby z wpisem "D" (dyżur) lub bez wpisu (domyślnie pracujące)
            const string sql = """
                SELECT FunkcjonariuszId FROM GrafikWpisy
                WHERE ZmianaId=? AND Rok=? AND Miesiac=? AND Dzien=? AND TypWpisu IN ('U','WS')
                """;

            await using var cmd = new OleDbCommand(sql, boberConn);
            cmd.Parameters.AddWithValue("ZmianaId", nrZmiany);
            cmd.Parameters.AddWithValue("Rok", data.Year);
            cmd.Parameters.AddWithValue("Miesiac", data.Month);
            cmd.Parameters.AddWithValue("Dzien", data.Day);
            await using var r = await cmd.ExecuteReaderAsync();

            var nieobecniIds = new HashSet<int>();
            while (await r.ReadAsync())
                nieobecniIds.Add(r.GetIntSafe(0));

            return wszyscy.Where(f => !nieobecniIds.Contains(f.Id)).ToList();
        }
        catch
        {
            return wszyscy;
        }
    }

    private static async Task AttachUprawnieniaAsync(OleDbConnection conn, List<Funkcjonariusz> list)
    {
        if (list.Count == 0) return;
        var ids = string.Join(",", list.Select(f => f.Id));
        var sql = $"""
            SELECT fu.FunkcjonariuszId, tu.Nazwa
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
            if (dict.TryGetValue(fid, out var f))
                f.NazwyUprawnien.Add(r.GetStringSafe(1));
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

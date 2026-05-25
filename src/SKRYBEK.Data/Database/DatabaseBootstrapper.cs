using System.Data.OleDb;
using SKRYBEK.Data.Connections;

namespace SKRYBEK.Data.Database;

public sealed class DatabaseBootstrapper
{
    private readonly SkrybekConnectionFactory _factory;

    public DatabaseBootstrapper(SkrybekConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task EnsureCreatedAsync()
    {
        var path = _factory.DatabasePath;
        if (!File.Exists(path))
            CreateAccessDatabase(path);

        await EnsureTablesExistAsync();
        await EnsureDefaultDataAsync();
    }

    private static void CreateAccessDatabase(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // Tworzy pustą bazę Access przez ADOX (COM)
        var catalogType = Type.GetTypeFromProgID("ADOX.Catalog")
            ?? throw new InvalidOperationException("ADOX.Catalog niedostępny. Zainstaluj Microsoft Access Database Engine.");

        dynamic catalog = Activator.CreateInstance(catalogType)!;
        catalog.Create($"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path};Jet OLEDB:Database Password=5359;");
    }

    private async Task EnsureTablesExistAsync()
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        foreach (var sql in SchemaScripts.CreateTables)
        {
            var tableName = ExtractTableName(sql);
            if (!await TableExistsAsync(conn, tableName))
            {
                await using var cmd = new OleDbCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    private static async Task<bool> TableExistsAsync(OleDbConnection conn, string tableName)
    {
        var schema = await conn.GetSchemaAsync("Tables");
        foreach (System.Data.DataRow row in schema.Rows)
        {
            if (row["TABLE_TYPE"]?.ToString() == "TABLE" &&
                string.Equals(row["TABLE_NAME"]?.ToString(), tableName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private async Task EnsureDefaultDataAsync()
    {
        await EnsureDefaultSamochodyAsync();
        await EnsureDefaultUstawieniaAsync();
        await EnsureDefaultUsersAsync();
    }

    private async Task EnsureDefaultSamochodyAsync()
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        await using var countCmd = new OleDbCommand("SELECT COUNT(*) FROM Samochody", conn);
        var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        if (count > 0) return;

        var domyslne = new[]
        {
            ("GBARt 2,5/16",  6, 1, 1, true),
            ("GCBARt 5/24",   6, 1, 2, true),
            ("SD 30",         6, 1, 3, true),
            ("Grupa SGRW-N",  4, 2, 4, true),
            ("Dyżurni nurkowie", 4, 2, 5, true),
            ("Grupa SGS",     4, 2, 6, true)
        };

        foreach (var (nazwa, pozycje, typ, kolejnosc, aktywny) in domyslne)
        {
            await using var cmd = new OleDbCommand(
                "INSERT INTO Samochody (Nazwa, LiczbaPozycji, Typ, Kolejnosc, CzyAktywny) VALUES (?, ?, ?, ?, ?)", conn);
            cmd.Parameters.AddWithValue("Nazwa", nazwa);
            cmd.Parameters.AddWithValue("LiczbaPozycji", pozycje);
            cmd.Parameters.AddWithValue("Typ", typ);
            cmd.Parameters.AddWithValue("Kolejnosc", kolejnosc);
            cmd.Parameters.AddWithValue("CzyAktywny", aktywny);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private async Task EnsureDefaultUstawieniaAsync()
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        var defaults = new Dictionary<string, string>
        {
            ["SciezkaBoberBazy"]  = @"C:\Users\Maciej\source\repos\BOBER\src\BOBER.App\bin\Release\net10.0-windows\win-x64\BoberDatabase.accdb",
            ["SciezkaChomikBazy"] = @"C:\Users\Maciej\AppData\Local\CHOMIK\ChomikDatabase.accdb",
            ["NrJRG"]             = "4",
            ["OstatniBackup"]     = string.Empty
        };

        foreach (var (klucz, wartosc) in defaults)
        {
            await using var check = new OleDbCommand("SELECT COUNT(*) FROM Ustawienia WHERE Klucz=?", conn);
            check.Parameters.AddWithValue("Klucz", klucz);
            var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
            if (!exists)
            {
                await using var ins = new OleDbCommand("INSERT INTO Ustawienia (Klucz, Wartosc) VALUES (?, ?)", conn);
                ins.Parameters.AddWithValue("Klucz", klucz);
                ins.Parameters.AddWithValue("Wartosc", wartosc);
                await ins.ExecuteNonQueryAsync();
            }
        }
    }

    private async Task EnsureDefaultUsersAsync()
    {
        await using var conn = _factory.Create();
        await conn.OpenAsync();

        await using var countCmd = new OleDbCommand("SELECT COUNT(*) FROM Uzytkownicy", conn);
        var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        if (count > 0) return;

        // Domyślni użytkownicy — hasło "skrybek" + sól
        var domyslni = new[]
        {
            ("zmiana1",  1, 1, "skrybek"),
            ("zmiana2",  2, 2, "skrybek"),
            ("zmiana3",  3, 3, "skrybek"),
            ("dca",      10, 0, "skrybek"),
            ("pa",       20, 0, "skrybek")
        };

        foreach (var (login, rola, zmiana, haslo) in domyslni)
        {
            var sol = Guid.NewGuid().ToString("N");
            var hash = HashPassword(haslo, sol);
            await using var cmd = new OleDbCommand(
                "INSERT INTO Uzytkownicy (Login, Rola, NumerZmiany, HasloHash, HasloSol) VALUES (?, ?, ?, ?, ?)", conn);
            cmd.Parameters.AddWithValue("Login", login);
            cmd.Parameters.AddWithValue("Rola", rola);
            cmd.Parameters.AddWithValue("NumerZmiany", zmiana);
            cmd.Parameters.AddWithValue("HasloHash", hash);
            cmd.Parameters.AddWithValue("HasloSol", sol);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static string HashPassword(string password, string salt)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password + salt);
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }

    private static string ExtractTableName(string sql)
    {
        var parts = sql.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i].Equals("TABLE", StringComparison.OrdinalIgnoreCase))
                return parts[i + 1].Trim('(');
        }
        return string.Empty;
    }
}

namespace SKRYBEK.Core.Configuration;

/// <summary>
/// Ścieżki do baz CHOMIK i BOBER — wyłącznie z pliku DatabasePatch.txt obok SKRYBEK.exe.
/// Plik edytuj przed uruchomieniem; program nie zmienia go w trakcie pracy.
/// </summary>
public sealed class DatabasePatch
{
    public const string FileName = "DatabasePatch.txt";

    public const string ChomikKey = "ChomikDatabase";
    public const string BoberKey = "BoberDatabase";

    public string ChomikDatabasePath { get; }
    public string BoberDatabasePath { get; }

    private DatabasePatch(string chomikPath, string boberPath)
    {
        ChomikDatabasePath = chomikPath;
        BoberDatabasePath = boberPath;
    }

    public static string GetFilePath() =>
        Path.Combine(AppContext.BaseDirectory, FileName);

    public static string DefaultChomikDatabasePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CHOMIK",
            "ChomikDatabase.accdb");

    public static string DefaultBoberDatabasePath() =>
        Path.Combine(AppContext.BaseDirectory, "BoberDatabase.accdb");

    public static DatabasePatch Load()
    {
        var filePath = GetFilePath();
        if (!File.Exists(filePath))
            CreateDefaultFile(filePath);

        var values = ParseFile(File.ReadAllLines(filePath));
        var chomikRaw = GetValue(values, ChomikKey);
        var boberRaw = GetValue(values, BoberKey);

        if (string.IsNullOrWhiteSpace(chomikRaw))
        {
            throw new InvalidOperationException(
                $"Brak ścieżki bazy CHOMIK w pliku {filePath}.\n" +
                $"Dodaj linię: {ChomikKey}=...");
        }

        return new DatabasePatch(
            ResolveDatabasePath(chomikRaw),
            ResolveBoberPath(boberRaw));
    }

    /// <summary>
    /// Jeśli ścieżka w pliku jest pusta, próbuje znaleźć BoberDatabase.accdb obok exe lub w %LOCALAPPDATA%\BOBER.
    /// Pliku DatabasePatch.txt nie modyfikuje.
    /// </summary>
    public static string ResolveBoberPath(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
            return ResolveDatabasePath(configured);

        foreach (var candidate in EnumerateBoberCandidates())
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return string.Empty;
    }

    private static IEnumerable<string> EnumerateBoberCandidates()
    {
        yield return DefaultBoberDatabasePath();
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BOBER",
            "BoberDatabase.accdb");
    }

    private static void CreateDefaultFile(string filePath)
    {
        var content =
            "# Ścieżki baz danych — edytuj przed uruchomieniem SKRYBEK.\n" +
            "# Program odczytuje ten plik tylko przy starcie i go nie modyfikuje.\n" +
            "# Obsługiwane zmienne: %LOCALAPPDATA%, %USERPROFILE% itd.\n" +
            "\n" +
            $"{ChomikKey}={DefaultChomikDatabasePath()}\n" +
            $"{BoberKey}={DefaultBoberDatabasePath()}\n";

        File.WriteAllText(filePath, content);
    }

    private static Dictionary<string, string> ParseFile(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = trimmed[..separator].Trim();
            var value = trimmed[(separator + 1)..].Trim();
            result[key] = value;
        }

        return result;
    }

    private static string GetValue(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value : string.Empty;

    public static string ResolveDatabasePath(string configured)
    {
        var raw = configured.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var expanded = Environment.ExpandEnvironmentVariables(raw);
        var full = Path.IsPathRooted(expanded)
            ? expanded
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expanded));

        return NormalizeAccessExtension(full);
    }

    private static string NormalizeAccessExtension(string path)
    {
        if (File.Exists(path))
            return path;

        if (path.EndsWith(".acc", StringComparison.OrdinalIgnoreCase))
        {
            var accdb = path[..^4] + "accdb";
            if (File.Exists(accdb))
                return accdb;
        }

        return path;
    }
}

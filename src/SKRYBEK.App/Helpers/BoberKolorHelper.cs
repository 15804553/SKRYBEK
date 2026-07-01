using System.Data.OleDb;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using SKRYBEK.Core.Chomik;
using SKRYBEK.Core.Configuration;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;

namespace SKRYBEK.App.Helpers;

/// <summary>
/// Mapuje funkcjonariusza na kolory zgodne z grafikiem służby BOBER.
/// Kolory ról wczytywane są z tabeli KoloryStanowisk w bazie BOBER,
/// z fallbackiem do pliku Themes/kolory-rol.json.
/// </summary>
public static class BoberKolorHelper
{
    private const string KluczNurekCzcionka = "NurekCzcionka";

    private static readonly string PlikKolorow =
        Path.Combine(AppContext.BaseDirectory, "Themes", "kolory-rol.json");

    private static readonly IReadOnlyDictionary<string, string> DomyslneKolory =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DowodcaZmiany"] = "#F79646",
            ["DowodcaZastepu"] = "#92D050",
            ["DowodcaSekcji"] = "#B8CCE4",
            ["Kierowca"] = "#BFBFBF",
            ["Zwykly"] = "#FFFFFF",
            [KluczNurekCzcionka] = "#F80808",
        };

    private static readonly Lazy<IReadOnlyDictionary<string, string>> KoloryRoli = new(WczytajKoloryRoli);

    public static Brush DomyslnyForeground =>
        new SolidColorBrush(KontrastowyTekst(Color.FromRgb(0x2D, 0x2D, 0x2D)));

    public static Brush WyznaczKolorTla(Funkcjonariusz osoba) =>
        new SolidColorBrush(PobierzKolorTlaRoli(WyznaczKluczRoliTla(osoba)));

    public static Brush WyznaczKolorForeground(Funkcjonariusz osoba)
    {
        if (CzyNurek(osoba))
            return new SolidColorBrush(PobierzHexKlucza(KluczNurekCzcionka));

        var tlo = PobierzKolorTlaRoli(WyznaczKluczRoliTla(osoba));
        return new SolidColorBrush(KontrastowyTekst(tlo));
    }

    private static bool CzyNurek(Funkcjonariusz osoba) =>
        osoba.MaUprawnieniaNumek || osoba.MaUprawnieniaKPP;

    /// <summary>
    /// Rola tła wiersza — ta sama logika co w BOBER (nurek nie zmienia tła).
    /// </summary>
    private static string WyznaczKluczRoliTla(Funkcjonariusz osoba)
    {
        var sid = osoba.StanowiskoId;

        if (ChomikSlowniki.StanowiskaDowodcyZmiany.Contains(sid))
            return "DowodcaZmiany";

        if (ChomikSlowniki.StanowiskaDowodcySekcji.Contains(sid))
            return "DowodcaSekcji";

        if (sid == ChomikSlowniki.StanowiskoDowodcaZastepu)
            return "DowodcaZastepu";

        if (osoba.MaUprawnieniaKierowca)
            return "Kierowca";

        return "Zwykly";
    }

    private static Color PobierzKolorTlaRoli(string klucz) =>
        PobierzHexKlucza(klucz);

    private static Color PobierzHexKlucza(string klucz)
    {
        if (KoloryRoli.Value.TryGetValue(klucz, out var hex))
            return ParsujKolor(hex);

        return ParsujKolor(DomyslneKolory.TryGetValue(klucz, out var domyslny) ? domyslny : "#FFFFFF");
    }

    private static Color KontrastowyTekst(Color tlo)
    {
        var luminance = (0.299 * tlo.R + 0.587 * tlo.G + 0.114 * tlo.B) / 255;
        return luminance > 0.55
            ? Color.FromRgb(0x1E, 0x1E, 0x1E)
            : Color.FromRgb(0xE0, 0xE0, 0xE0);
    }

    private static IReadOnlyDictionary<string, string> WczytajKoloryRoli()
    {
        var kolory = new Dictionary<string, string>(DomyslneKolory, StringComparer.OrdinalIgnoreCase);

        foreach (var (klucz, hex) in WczytajKoloryZJson())
            kolory[klucz] = hex;

        foreach (var (klucz, hex) in WczytajKoloryZBazyBober())
            kolory[klucz] = hex;

        return kolory;
    }

    private static IEnumerable<KeyValuePair<string, string>> WczytajKoloryZJson()
    {
        var wynik = new List<KeyValuePair<string, string>>();
        try
        {
            if (!File.Exists(PlikKolorow))
                return wynik;

            var json = File.ReadAllText(PlikKolorow);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals("Nurek") && property.Value.ValueKind == JsonValueKind.Object)
                {
                    if (property.Value.TryGetProperty("czcionka", out var czcionka)
                        && czcionka.GetString() is { } nurekHex)
                    {
                        wynik.Add(new KeyValuePair<string, string>(KluczNurekCzcionka, nurekHex));
                    }

                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.String
                    && property.Value.GetString() is { } hex)
                {
                    wynik.Add(new KeyValuePair<string, string>(property.Name, hex));
                }
            }
        }
        catch
        {
            // fallback do wartości domyślnych
        }

        return wynik;
    }

    private static IEnumerable<KeyValuePair<string, string>> WczytajKoloryZBazyBober()
    {
        var wynik = new List<KeyValuePair<string, string>>();
        try
        {
            var patch = DatabasePatch.Load();
            var path = DatabasePatch.ResolveBoberPath(patch.BoberDatabasePath);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return wynik;

            var factory = new BoberConnectionFactory(path);
            using var connection = factory.Create();
            connection.Open();

            using var command = new OleDbCommand(
                "SELECT KluczRoli, KolorHex FROM KoloryStanowisk", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var klucz = reader.GetString(0);
                var hex = reader.GetString(1);
                if (!string.IsNullOrWhiteSpace(klucz) && !string.IsNullOrWhiteSpace(hex))
                    wynik.Add(new KeyValuePair<string, string>(klucz, hex));
            }
        }
        catch
        {
            // fallback do pliku JSON / wartości domyślnych
        }

        return wynik;
    }

    private static Color ParsujKolor(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex)!;
        }
        catch
        {
            return Colors.White;
        }
    }
}

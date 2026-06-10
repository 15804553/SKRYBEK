namespace SKRYBEK.Core.Chomik;

/// <summary>
/// Identyfikatory rekordów w słownikach bazy CHOMIK (StanowiskaSlownik, TypyUprawnien).
/// </summary>
public static class ChomikSlowniki
{
    /// <summary>Stanowiska z CHOMIK uprawniające do miejsca 1.D w pojeździe.</summary>
    public static readonly HashSet<int> StanowiskaMiejsca1D =
    [
        9,  // Dowódca zastępu
        10, // Dowódca zastępu
        12, // Dowódca sekcji
        13, // Zastępca dowódcy zmiany
        14  // Dowódca zmiany
    ];

    /// <summary>Funkcje dodatkowe z CHOMIK uprawniające do miejsca 1.D w pojeździe.</summary>
    public static readonly string[] NazwyFunkcjiDodatkowychMiejsca1D =
    [
        "DCA sekcji",
        "DCA zastępu"
    ];

    public static bool CzyMozeNaMiejsce1DPojazdu(int stanowiskoId, IEnumerable<string> funkcjeDodatkowe) =>
        StanowiskaMiejsca1D.Contains(stanowiskoId) ||
        funkcjeDodatkowe.Any(f =>
            NazwyFunkcjiDodatkowychMiejsca1D.Contains(f.Trim(), StringComparer.OrdinalIgnoreCase));

    public const int UprawnienieKierowcaKatC  = 2;
    public const int UprawnienieKierowcaKatCE = 3;
    public const int UprawnienieNurek          = 9;
    public const int UprawnienieKPP            = 10;

    public static string FormatUprawnienie(string nazwa, string? podtyp)
        => string.IsNullOrWhiteSpace(podtyp) ? nazwa.Trim() : $"{nazwa.Trim()} {podtyp.Trim()}";
}

using SKRYBEK.Core.Models;

namespace SKRYBEK.Core.Rules;

public static class PozycjaSamochoduRules
{
    public static string OznaczeniePozycji(int pozycja) => pozycja switch
    {
        1 => "D",
        2 => "K",
        _ => string.Empty
    };

    public static string EtykietaPozycji(int pozycja)
    {
        var ozn = OznaczeniePozycji(pozycja);
        return string.IsNullOrEmpty(ozn) ? $"{pozycja}." : $"{pozycja}.{ozn}";
    }

    public static bool CzyOsobaDozwolonaNaPozycji(Funkcjonariusz osoba, int pozycja) => pozycja switch
    {
        1 => osoba.CzyMozeNaMiejsce1DPojazdu,
        2 => osoba.MaUprawnieniaKierowca,
        _ => true
    };

    public static string OpisWymagania(int pozycja) => pozycja switch
    {
        1 => "Miejsce 1.D — dowódca zmiany, zastępca dowódcy zmiany, dowódca zastępu, dowódca sekcji " +
             "lub funkcja dodatkowa DCA zastępu / DCA sekcji.",
        2 => "Miejsce 2.K — tylko kierowca kat. C lub C+E.",
        _ => string.Empty
    };
}

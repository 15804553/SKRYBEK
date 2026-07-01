using SKRYBEK.Core.Chomik;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;

namespace SKRYBEK.Core.Rules;

public static class StanowiskoSluzbyRules
{
    /// <summary>
    /// Sprawdza czy osoba spełnia wymagania danego stanowiska służby:
    /// <list type="bullet">
    ///   <item>DowodcaZmiany — stanowisko: dowódca zmiany / zastępca d-cy zmiany / dowódca sekcji</item>
    ///   <item>Garazomistrz — uprawnienia kierowcy kat. C lub C+E</item>
    ///   <item>Bosman, DowodcaDzialanRatowniczychSGRWN — uprawnienia nurka</item>
    ///   <item>Sonarzysta — uprawnienie sonarzysty lub funkcja dodatkowa „sonarzysta"</item>
    ///   <item>Pozostałe — brak ograniczeń (każda osoba)</item>
    /// </list>
    /// </summary>
    public static bool CzyOsobaDozwolonaNaStanowisko(Funkcjonariusz osoba, StanowiskoSluzby stanowisko)
        => stanowisko switch
        {
            StanowiskoSluzby.DowodcaZmiany =>
                ChomikSlowniki.StanowiskaUprawnioneNaDowodceZmiany.Contains(osoba.StanowiskoId),
            StanowiskoSluzby.Garazomistrz =>
                osoba.MaUprawnieniaKierowca,
            StanowiskoSluzby.Bosman =>
                osoba.MaUprawnieniaNumek,
            StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN =>
                osoba.MaUprawnieniaNumek,
            StanowiskoSluzby.Sonarzysta =>
                osoba.IdUprawnien.Contains(ChomikSlowniki.UprawnienieSonarzysta)
                || osoba.NazwyFunkcjiDodatkowych.Any(f =>
                    f.Contains(ChomikSlowniki.FunkcjaSonarzysta, StringComparison.OrdinalIgnoreCase)),
            _ => true
        };

    public static string OpisWymagania(StanowiskoSluzby stanowisko) => stanowisko switch
    {
        StanowiskoSluzby.DowodcaZmiany =>
            "Dowódca zmiany — wymagane stanowisko: dowódca zmiany, zastępca d-cy zmiany lub dowódca sekcji.",
        StanowiskoSluzby.Garazomistrz =>
            "Garażomistrz — wymagane uprawnienia kierowcy (kat. C lub C+E).",
        StanowiskoSluzby.Bosman =>
            "Bosman — wymagane uprawnienia nurka.",
        StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN =>
            "Dowódca działań ratowniczych SGRW-N — wymagane uprawnienia nurka.",
        StanowiskoSluzby.Sonarzysta =>
            "Sonarzysta \u2014 wymagane uprawnienie sonarzysty lub funkcja dodatkowa \"sonarzysta\".",
        _ => string.Empty
    };

    public static bool MaWymagania(StanowiskoSluzby stanowisko) => stanowisko switch
    {
        StanowiskoSluzby.DowodcaZmiany => true,
        StanowiskoSluzby.Garazomistrz => true,
        StanowiskoSluzby.Bosman => true,
        StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN => true,
        StanowiskoSluzby.Sonarzysta => true,
        _ => false
    };
}

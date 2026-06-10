using SKRYBEK.Core.Chomik;

namespace SKRYBEK.Core.Models;

public sealed class Funkcjonariusz
{
    public int Id { get; set; }
    public int NumerZmiany { get; set; }
    public int StopienId { get; set; }
    public int StanowiskoId { get; set; }
    public string Stopien { get; set; } = string.Empty;
    public string Imie { get; set; } = string.Empty;
    public string Nazwisko { get; set; } = string.Empty;
    public string Stanowisko { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public int? StazLat { get; set; }

    public string PelneImieNazwisko => $"{Imie} {Nazwisko}".Trim();
    public string StopienINazwisko => $"{Stopien} {Nazwisko}".Trim();

    /// <summary>Id typów uprawnień z tabeli TypyUprawnien (CHOMIK).</summary>
    public List<int> IdUprawnien { get; set; } = [];

    /// <summary>Pełne nazwy uprawnień (Nazwa + Podtyp) z CHOMIK — do wyświetlania i filtrów.</summary>
    public List<string> NazwyUprawnien { get; set; } = [];

    public List<string> NazwyFunkcjiDodatkowych { get; set; } = [];

    public bool MaUprawnieniaKierowcaC =>
        IdUprawnien.Contains(ChomikSlowniki.UprawnienieKierowcaKatC);

    public bool MaUprawnieniaKierowcaCE =>
        IdUprawnien.Contains(ChomikSlowniki.UprawnienieKierowcaKatCE);

    public bool MaUprawnieniaKierowca => MaUprawnieniaKierowcaC || MaUprawnieniaKierowcaCE;

    public bool MaUprawnieniaNumek =>
        IdUprawnien.Contains(ChomikSlowniki.UprawnienieNurek);

    public bool MaUprawnieniaKPP =>
        IdUprawnien.Contains(ChomikSlowniki.UprawnienieKPP);

    /// <summary>Miejsce 1.D — stanowisko lub funkcja dodatkowa z CHOMIK.</summary>
    public bool CzyMozeNaMiejsce1DPojazdu =>
        ChomikSlowniki.CzyMozeNaMiejsce1DPojazdu(StanowiskoId, NazwyFunkcjiDodatkowych);
}

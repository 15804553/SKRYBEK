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

    public List<string> NazwyUprawnien { get; set; } = [];
    public List<string> NazwyFunkcjiDodatkowych { get; set; } = [];

    public bool MaUprawnieniaKierowcaC =>
        NazwyUprawnien.Any(u => u.Contains("kat. C", StringComparison.OrdinalIgnoreCase)
                             && !u.Contains("C+E", StringComparison.OrdinalIgnoreCase));

    public bool MaUprawnieniaKierowcaCE =>
        NazwyUprawnien.Any(u => u.Contains("kat. C+E", StringComparison.OrdinalIgnoreCase));

    public bool MaUprawnieniaKierowca => MaUprawnieniaKierowcaC || MaUprawnieniaKierowcaCE;

    public bool MaUprawnieniaNumek =>
        NazwyUprawnien.Any(u => u.Contains("Nurek", StringComparison.OrdinalIgnoreCase));

    public bool MaUprawnieniaKPP =>
        NazwyUprawnien.Any(u => u.Contains("Kierownik prac podwodnych", StringComparison.OrdinalIgnoreCase));
}

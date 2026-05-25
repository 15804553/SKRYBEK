using SKRYBEK.Core.Enums;

namespace SKRYBEK.Core.Models;

public sealed class RozkazDzienny
{
    public int Id { get; set; }
    public int NumerRozkazu { get; set; }
    public int Rok { get; set; }
    public DateOnly Data { get; set; }
    public int ZmianaId { get; set; }
    public string Zajecia { get; set; } = string.Empty;
    public string Uwagi { get; set; } = string.Empty;
    public DateTime DataUtworzenia { get; set; }
    public StatusRozkazu Status { get; set; }

    public string NumerFormatowany => $"{NumerRozkazu}/{Rok}";
    public string DataFormatowana => Data.ToString("dd.MM.yyyy");

    public List<PozycjaSluzby> Sluzba { get; set; } = [];
    public List<PozycjaSamochodu> PodzialBojowy { get; set; } = [];
    public List<RatownikMedyczny> RatwnicyMedyczni { get; set; } = [];
    public List<NieobecnyWSluzbie> Nieobecni { get; set; } = [];
}

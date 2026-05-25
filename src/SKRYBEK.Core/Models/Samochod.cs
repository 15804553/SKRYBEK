using SKRYBEK.Core.Enums;

namespace SKRYBEK.Core.Models;

public sealed class Samochod
{
    public int Id { get; set; }
    public string Nazwa { get; set; } = string.Empty;
    public int LiczbaPozycji { get; set; }
    public TypSamochodu Typ { get; set; }
    public int Kolejnosc { get; set; }
    public bool CzyAktywny { get; set; } = true;

    public bool CzyPodstawowy => Typ == TypSamochodu.Podstawowy;
}

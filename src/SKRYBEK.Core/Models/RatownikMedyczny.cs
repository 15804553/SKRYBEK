namespace SKRYBEK.Core.Models;

public sealed class RatownikMedyczny
{
    public int Id { get; set; }
    public int RozkazId { get; set; }
    public int Pozycja { get; set; }
    public int? FunkcjonariuszId { get; set; }
    public string Nazwisko { get; set; } = string.Empty;
}

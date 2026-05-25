using SKRYBEK.Core.Enums;

namespace SKRYBEK.Core.Models;

public sealed class NieobecnyWSluzbie
{
    public int Id { get; set; }
    public int RozkazId { get; set; }
    public int? FunkcjonariuszId { get; set; }
    public string Nazwisko { get; set; } = string.Empty;
    public TypNieobecnosci TypNieobecnosci { get; set; }

    public string NazwaTypu => TypNieobecnosci switch
    {
        TypNieobecnosci.Urlop        => "Urlop",
        TypNieobecnosci.CzasWolny    => "Czas wolny",
        TypNieobecnosci.Chory        => "Chory",
        TypNieobecnosci.Delegowany   => "Delegowany",
        TypNieobecnosci.DyzurDomowy  => "Dyżur domowy",
        _                            => string.Empty
    };
}

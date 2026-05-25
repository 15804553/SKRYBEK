using SKRYBEK.Core.Enums;

namespace SKRYBEK.Core.Models;

public sealed class PozycjaSluzby
{
    public int Id { get; set; }
    public int RozkazId { get; set; }
    public StanowiskoSluzby Stanowisko { get; set; }
    public int? FunkcjonariuszId { get; set; }
    public string Nazwisko { get; set; } = string.Empty;

    public string NazwaStanowiska => Stanowisko switch
    {
        StanowiskoSluzby.DowodcaZmiany                      => "Dowódca zmiany",
        StanowiskoSluzby.DyzurnyPAJRG                       => "Dyżurny PA JRG",
        StanowiskoSluzby.SzefZmiany                         => "Szef zmiany",
        StanowiskoSluzby.Garazomistrz                       => "Garażomistrz",
        StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN    => "Dowódca działań ratowniczych SGRW-N",
        StanowiskoSluzby.Bosman                             => "Bosman",
        StanowiskoSluzby.Sonarzysta                         => "Sonarzysta",
        StanowiskoSluzby.PodoficerDyzurny                   => "Podoficer dyżurny",
        StanowiskoSluzby.StrazakDyzurny                     => "Strażak dyżurny",
        _                                                   => Stanowisko.ToString()
    };
}

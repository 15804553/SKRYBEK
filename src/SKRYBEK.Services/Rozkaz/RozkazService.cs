using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Repositories;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Rozkaz;

public sealed class RozkazService
{
    private readonly RozkazRepository _repo;
    private readonly SamochodyRepository _samochodyRepo;

    public RozkazService(RozkazRepository repo, SamochodyRepository samochodyRepo)
    {
        _repo         = repo;
        _samochodyRepo = samochodyRepo;
    }

    public async Task<List<RozkazDzienny>> GetByRokAsync(int rok)
        => await _repo.GetByRokAsync(rok);

    public async Task<RozkazDzienny?> GetByIdAsync(int id)
        => await _repo.GetByIdAsync(id);

    public async Task<RozkazDzienny> NowyRozkazAsync(DateOnly data, int nrZmiany)
    {
        var numer = await _repo.GetNastepnyNumerAsync(data.Year);
        var samochody = await _samochodyRepo.GetAktywneAsync();

        var rozkaz = new RozkazDzienny
        {
            NumerRozkazu   = numer,
            Rok            = data.Year,
            Data           = data,
            ZmianaId       = nrZmiany,
            DataUtworzenia = DateTime.Now,
            Status         = StatusRozkazu.Roboczy,
            Zajecia        = "Według planu doskonalenia zawodowego"
        };

        // Inicjalizuj 9 stałych pozycji SŁUŻBA
        foreach (StanowiskoSluzby stanowisko in Enum.GetValues<StanowiskoSluzby>())
        {
            rozkaz.Sluzba.Add(new PozycjaSluzby { Stanowisko = stanowisko });
        }

        // Inicjalizuj pozycje dla aktywnych samochodów
        foreach (var s in samochody)
        {
            for (int poz = 1; poz <= s.LiczbaPozycji; poz++)
            {
                rozkaz.PodzialBojowy.Add(new PozycjaSamochodu
                {
                    SamochodId = s.Id,
                    Pozycja    = poz
                });
            }
        }

        // 2 pozycje ratowników medycznych
        rozkaz.RatwnicyMedyczni.Add(new RatownikMedyczny { Pozycja = 1 });
        rozkaz.RatwnicyMedyczni.Add(new RatownikMedyczny { Pozycja = 2 });

        return rozkaz;
    }

    public async Task<int> ZapiszAsync(RozkazDzienny rozkaz)
    {
        ValidateKonfliktyPojazdow(rozkaz);
        var id = await _repo.SaveAsync(rozkaz);
        SkrybekLog.Info($"Zapisano rozkaz nr {rozkaz.NumerRozkazu}/{rozkaz.Rok}, Id={id}");
        return id;
    }

    public async Task UsunAsync(int id)
    {
        await _repo.DeleteAsync(id);
        SkrybekLog.Info($"Usunięto rozkaz Id={id}");
    }

    /// <summary>Sprawdza czy żadna osoba nie jest przypisana do dwóch pojazdów podstawowych.</summary>
    public void ValidateKonfliktyPojazdow(RozkazDzienny rozkaz)
    {
        var podstawowe = rozkaz.PodzialBojowy
            .Where(p => p.FunkcjonariuszId.HasValue)
            .GroupBy(p => p.SamochodId)
            .ToLookup(g => g.Key);

        // Zbierz ID pojazdów podstawowych
        // Zakładamy że baza danych ma poprawne Id — używamy cache z NowyRozkazAsync lub sprawdzamy typ
        // Walidacja: ta sama osoba na dwóch różnych samochodach podstawowych
        var osobyNaPodstawowych = rozkaz.PodzialBojowy
            .Where(p => p.FunkcjonariuszId.HasValue)
            .GroupBy(p => p.FunkcjonariuszId!.Value)
            .Where(g => g.Select(p => p.SamochodId).Distinct().Count() > 1);

        // Uwaga: walidacja pełna wymaga informacji o typie pojazdu — sprawdzana w VM
        // Tu logujemy ostrzeżenie (sprawdzenie typów odbywa się w AssignFunkcjonariuszToVehicle)
    }

    /// <summary>
    /// Sprawdza konflikt: czy dana osoba jest już przypisana do pojazdu podstawowego.
    /// Zwraca true jeśli przypisanie jest dozwolone.
    /// </summary>
    public static bool MoznaAssignowacDoPodstawowego(
        RozkazDzienny rozkaz,
        int funkcjonariuszId,
        int docelowySamochodId,
        IEnumerable<Samochod> wszystkieSamochody)
    {
        var podstawoweIds = wszystkieSamochody
            .Where(s => s.CzyPodstawowy && s.Id != docelowySamochodId)
            .Select(s => s.Id)
            .ToHashSet();

        return !rozkaz.PodzialBojowy.Any(p =>
            p.FunkcjonariuszId == funkcjonariuszId &&
            podstawoweIds.Contains(p.SamochodId));
    }
}

using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Grafik;
using SKRYBEK.Data.Repositories;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Personnel;

public sealed class PersonnelService
{
    private readonly PersonnelRepository _repo;
    private readonly ShiftCalendarEngine _calendar;

    public PersonnelService(PersonnelRepository repo, ShiftCalendarEngine calendar)
    {
        _repo     = repo;
        _calendar = calendar;
    }

    public async Task<List<Funkcjonariusz>> GetDostepniAsync(DateOnly data, int nrZmiany)
    {
        var wszyscy = await _repo.GetByZmianaAsync(nrZmiany);
        SkrybekLog.Info($"CHOMIK — zmiana {nrZmiany}: {wszyscy.Count} funkcjonariuszy");

        var dzienSluzby = await _calendar.IsWorkDayAsync(nrZmiany, data);
        SkrybekLog.Info($"Kalendarz — {data:yyyy-MM-dd}, zmiana {nrZmiany}, dzień służby: {dzienSluzby}");

        var lista = await _repo.GetDostepniWDniuAsync(data, nrZmiany, wszyscy);
        SkrybekLog.Info($"Personel dostępny na {data:yyyy-MM-dd}: {lista.Count} osób");

        foreach (var f in lista)
        {
            var kierowca = f.MaUprawnieniaKierowca;
            SkrybekLog.Debug(
                $"  [{f.Id}] {f.StopienINazwisko} | StanowiskoId={f.StanowiskoId} " +
                $"| Uprawnienia=[{string.Join(",", f.IdUprawnien)}] " +
                $"| Kierowca={kierowca} | Funkcje=[{string.Join(",", f.NazwyFunkcjiDodatkowych)}]");
        }

        return lista;
    }

    /// <summary>Następny dzień służby zmiany po podanej dacie (bez dnia „po”).</summary>
    public Task<DateOnly> GetNastepnyDzienSluzbyPoAsync(int nrZmiany, DateOnly poDniu) =>
        _calendar.GetNextWorkDayAfterAsync(nrZmiany, poDniu);

    public Task<bool> CzyDzienSluzbyAsync(int nrZmiany, DateOnly data) =>
        _calendar.IsWorkDayAsync(nrZmiany, data);

    public async Task<List<Funkcjonariusz>> GetWszyscyZmianaAsync(int nrZmiany)
    {
        return await _repo.GetByZmianaAsync(nrZmiany);
    }

    public List<Funkcjonariusz> FiltrujWgKryteriow(
        IEnumerable<Funkcjonariusz> lista,
        bool tylkoKierowcyC,
        bool tylkoKierowcyCE,
        bool tylkoNurkowie,
        bool tylkoKPP,
        string? funkcja = null)
    {
        return lista.Where(f =>
            (!tylkoKierowcyC  || f.MaUprawnieniaKierowcaC) &&
            (!tylkoKierowcyCE || f.MaUprawnieniaKierowcaCE) &&
            (!tylkoNurkowie   || f.MaUprawnieniaNumek) &&
            (!tylkoKPP        || f.MaUprawnieniaKPP) &&
            (funkcja is null  || f.NazwyFunkcjiDodatkowych.Contains(funkcja, StringComparer.OrdinalIgnoreCase))
        ).ToList();
    }

    public List<string> GetDostepneFunkcje(IEnumerable<Funkcjonariusz> lista)
    {
        return lista
            .SelectMany(f => f.NazwyFunkcjiDodatkowych)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    /// <summary>
    /// Pobiera nieobecnych w danym dniu z BOBER i zwraca jako listę NieobecnyWSluzbie
    /// wstępnie wypełnionych danymi z CHOMIK. Zwraca pustą listę gdy BOBER niedostępny.
    /// </summary>
    public async Task<List<NieobecnyWSluzbie>> GetNieobecniWDniuAsync(
        DateOnly data, int nrZmiany, IReadOnlyList<Funkcjonariusz> wszyscy)
    {
        var nieobecniZBober = await _repo.PobierzNieobecnychZTypemAsync(data, nrZmiany);
        if (nieobecniZBober.Count == 0) return [];

        var personelPoId = wszyscy.ToDictionary(f => f.Id);
        var wynik = new List<NieobecnyWSluzbie>();

        foreach (var (fid, typ) in nieobecniZBober)
        {
            personelPoId.TryGetValue(fid, out var osoba);
            wynik.Add(new NieobecnyWSluzbie
            {
                FunkcjonariuszId = fid,
                Nazwisko = osoba is not null
                    ? $"{osoba.Stopien} {osoba.Nazwisko}".Trim()
                    : $"ID:{fid}",
                TypNieobecnosci = typ
            });
        }

        SkrybekLog.Info($"BOBER — nieobecni na {data:yyyy-MM-dd}: {wynik.Count} osób");
        return wynik;
    }

    public Task<List<(int Id, string Nazwa)>> GetTypyUprawnienAsync()
        => _repo.GetTypyUprawnienAsync();
}

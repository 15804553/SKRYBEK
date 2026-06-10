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
}

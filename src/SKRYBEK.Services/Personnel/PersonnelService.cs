using SKRYBEK.Core.Models;
using SKRYBEK.Data.Repositories;

namespace SKRYBEK.Services.Personnel;

public sealed class PersonnelService
{
    private readonly PersonnelRepository _repo;

    public PersonnelService(PersonnelRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<Funkcjonariusz>> GetDostepniAsync(DateOnly data, int nrZmiany)
    {
        return await _repo.GetDostepniWDniuAsync(data, nrZmiany);
    }

    public async Task<List<Funkcjonariusz>> GetWszyscyZmianaAsync(int nrZmiany)
    {
        return await _repo.GetByZmianaAsync(nrZmiany);
    }

    public List<Funkcjonariusz> FiltrujWgKryteriow(
        List<Funkcjonariusz> lista,
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

    public List<string> GetDostepneFunkcje(List<Funkcjonariusz> lista)
    {
        return lista
            .SelectMany(f => f.NazwyFunkcjiDodatkowych)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }
}

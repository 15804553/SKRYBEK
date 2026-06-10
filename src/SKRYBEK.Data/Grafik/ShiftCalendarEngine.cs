using System.Data.OleDb;
using System.Text.Json;
using SKRYBEK.Data.Connections;

namespace SKRYBEK.Data.Grafik;

/// <summary>
/// Cykl 3-dniowy zmian PSP — odczyt DataReferencyjna i OffsetyZmian z bazy BOBER.
/// Pusta komórka grafiku w BOBER = funkcjonariusz w pracy.
/// </summary>
public sealed class ShiftCalendarEngine
{
    private readonly BoberConnectionFactory _bober;

    private DateOnly? _referenceDate;
    private Dictionary<int, int>? _offsets;

    public ShiftCalendarEngine(BoberConnectionFactory bober)
    {
        _bober = bober;
    }

    public async Task<bool> IsWorkDayAsync(int zmianaId, DateOnly date)
    {
        await EnsureLoadedAsync();
        return ComputeIsWorkDay(zmianaId, date);
    }

    public Task<DateOnly> GetNextWorkDayAfterAsync(int zmianaId, DateOnly afterDate) =>
        GetNextWorkDayAsync(zmianaId, afterDate.AddDays(1));

    public async Task<DateOnly> GetNextWorkDayAsync(int zmianaId, DateOnly fromDate)
    {
        var day = fromDate;
        for (var i = 0; i < 366; i++)
        {
            if (await IsWorkDayAsync(zmianaId, day))
                return day;
            day = day.AddDays(1);
        }

        throw new InvalidOperationException(
            $"Nie znaleziono dnia służby dla zmiany {zmianaId} w ciągu roku od {fromDate:yyyy-MM-dd}.");
    }

    private async Task EnsureLoadedAsync()
    {
        if (_referenceDate.HasValue)
            return;

        var refStr = await ReadUstawienieAsync("DataReferencyjna") ?? "2026-01-01";
        _referenceDate = DateOnly.ParseExact(refStr, "yyyy-MM-dd");

        var offsetsJson = await ReadUstawienieAsync("OffsetyZmian")
                          ?? """{"1":1,"2":2,"3":0}""";

        _offsets = JsonSerializer.Deserialize<Dictionary<string, int>>(offsetsJson)!
            .ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value);
    }

    private async Task<string?> ReadUstawienieAsync(string klucz)
    {
        if (string.IsNullOrWhiteSpace(_bober.DatabasePath))
            return null;

        try
        {
            await using var conn = _bober.Create();
            await conn.OpenAsync();
            await using var cmd = new OleDbCommand("SELECT Wartosc FROM Ustawienia WHERE Klucz=?", conn);
            cmd.Parameters.AddWithValue("Klucz", klucz);
            var val = await cmd.ExecuteScalarAsync();
            return val is null or DBNull ? null : val.ToString();
        }
        catch
        {
            return null;
        }
    }

    private bool ComputeIsWorkDay(int zmianaId, DateOnly date)
    {
        var dayOffset = date.DayNumber - _referenceDate!.Value.DayNumber;
        var slot = ((dayOffset % 3) + 3) % 3;
        return _offsets!.TryGetValue(zmianaId, out var zmianaOffset) && slot == zmianaOffset;
    }
}

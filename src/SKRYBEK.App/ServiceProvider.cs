using SKRYBEK.Services;

namespace SKRYBEK.App;

/// <summary>Dostęp do serwisów SKRYBEK — ustawiane przy starcie (standalone lub TUKAN).</summary>
public static class ServiceProvider
{
    public static AppServices Services { get; set; } = null!;
}

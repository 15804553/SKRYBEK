using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SKRYBEK.Core.Models;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.App.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private UserAccount? _wybranyUzytkownik;

    [ObservableProperty]
    private string _blad = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasloWymagane = true;

    public ObservableCollection<UserAccount> Uzytkownicy { get; } = [];

    public SessionInfo? Session { get; private set; }
    public event EventHandler<bool>? LoginCompleted;

    public async Task ZaladujUzytkownikowAsync()
    {
        IsLoading = true;
        Blad = string.Empty;
        try
        {
            SkrybekLog.Info("Ładowanie listy użytkowników z CHOMIK...");
            var lista = await App.Services.Auth.GetAvailableUsersAsync();
            SkrybekLog.Info($"Załadowano {lista.Count} użytkowników z CHOMIK");

            Uzytkownicy.Clear();
            foreach (var u in lista)
            {
                Uzytkownicy.Add(u);
                SkrybekLog.Info($"  Użytkownik: '{u.Login}' Rola={u.Role}");
            }

            WybranyUzytkownik = Uzytkownicy.FirstOrDefault();
        }
        catch (Exception ex)
        {
            SkrybekLog.Error("Błąd ładowania użytkowników CHOMIK", ex);
            Blad = $"Nie można załadować listy użytkowników:\n{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnWybranyUzytkownikChanged(UserAccount? value)
    {
        // PA nie wymaga hasła (brak hash w bazie CHOMIK)
        HasloWymagane = value is not null && !string.IsNullOrEmpty(value.HasloHash);
        Blad = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync(string haslo)
    {
        if (WybranyUzytkownik is null) return;

        Blad = string.Empty;
        IsLoading = true;
        try
        {
            var session = await App.Services.Auth.LoginAsync(WybranyUzytkownik.Login, haslo);
            if (session is null)
            {
                Blad = HasloWymagane
                    ? "Nieprawidłowe hasło."
                    : "Błąd logowania.";
                return;
            }
            Session = session;
            LoginCompleted?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            Blad = $"Błąd połączenia z bazą:\n{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanLogin() => WybranyUzytkownik is not null;
}

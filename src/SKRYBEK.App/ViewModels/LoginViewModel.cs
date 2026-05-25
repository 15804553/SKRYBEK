using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _login = string.Empty;

    [ObservableProperty]
    private string _blad = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public SessionInfo? Session { get; private set; }
    public event EventHandler<bool>? LoginCompleted;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync(string haslo)
    {
        Blad = string.Empty;
        IsLoading = true;
        try
        {
            var session = await App.Services.Auth.LoginAsync(Login, haslo);
            if (session is null)
            {
                Blad = "Nieprawidłowy login lub hasło.";
                return;
            }
            Session = session;
            LoginCompleted?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            Blad = $"Błąd połączenia z bazą danych:\n{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanLogin() => !string.IsNullOrWhiteSpace(Login);
}

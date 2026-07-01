using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SKRYBEK.App.Helpers;
using SKRYBEK.App.ViewModels;
using SKRYBEK.Core.Models;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.App.Views;

/// <summary>Moduł rozkazów SKRYBEK — osadzalny w TUKAN lub w oknie standalone.</summary>
public partial class SkrybekMainView : UserControl
{
    private MainViewModel _vm = null!;
    private string _loggedInLogin = string.Empty;
    private bool _canEditAll;

    public bool IsEmbedded { get; set; }

    public event EventHandler? LogoutRequested;

    public SkrybekMainView()
    {
        InitializeComponent();
        RozkazEditor.Loaded += (_, _) => ApplyEditorAccess();
    }

    public async Task InitializeAsync(SessionInfo session)
    {
        SetLoggedInUser(session.Login, session.CanEditAll);

        SkrybekLog.Info(
            $"SkrybekMainView.InitializeAsync: login='{_loggedInLogin}', isPa={SkrybekLoginAccess.IsPaAccount(_loggedInLogin)}");

        if (DataContext is MainViewModel existing)
        {
            existing.PropertyChanged -= OnVmPropertyChanged;
        }

        _vm = new MainViewModel(session);
        _vm.PropertyChanged += OnVmPropertyChanged;
        DataContext = _vm;

        UserLabel.Text = $"{session.Login}  |  {session.NazwaZmiany}";
        ApplyEmbeddedUi();

        RokCombo.SelectionChanged -= RokCombo_SelectionChanged;
        var lata = Enumerable.Range(DateTime.Today.Year - 2, 5).Reverse().ToList();
        RokCombo.ItemsSource = lata;
        RokCombo.SelectedItem = DateTime.Today.Year;
        RokCombo.SelectionChanged += RokCombo_SelectionChanged;

        await _vm.LoadAsync();
        ApplyEditorAccess();
    }

    /// <summary>Aktualizuje login po zalogowaniu (TUKAN może wywołać ponownie przy wejściu w moduł).</summary>
    public void SetLoggedInUser(string login, bool canEditAll)
    {
        _loggedInLogin = login ?? string.Empty;
        _canEditAll = canEditAll;
        ApplyEditorAccess();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.EditorVm))
        {
            ApplyEditorAccess();
        }
    }

    private void ApplyEditorAccess()
    {
        RozkazEditor.ApplyLoggedInUserAccess(_loggedInLogin, _canEditAll, _vm?.EditorVm);
    }

    private void ApplyEmbeddedUi()
    {
        if (!IsEmbedded)
        {
            return;
        }

        SettingsButton.Visibility = Visibility.Collapsed;
        LogoutButton.Visibility = Visibility.Collapsed;
        UserLabel.Visibility = Visibility.Collapsed;
    }

    private Window? OwnerWindow => Window.GetWindow(this);

    private async void RozkazyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RozkazyList.SelectedItem is RozkazDzienny rozkaz)
        {
            await _vm.OtworzRozkazCommand.ExecuteAsync(rozkaz);
            ApplyEditorAccess();
            _ = Dispatcher.BeginInvoke(ApplyEditorAccess, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void DeleteRozkaz_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RozkazDzienny rozkaz })
        {
            if (SkrybekMessageBox.Confirm(
                $"Czy na pewno usunąć rozkaz Nr {rozkaz.NumerFormatowany} z dnia {rozkaz.DataFormatowana}?",
                "Usuń rozkaz",
                SkrybekMessageKind.Warning,
                OwnerWindow))
            {
                _ = _vm.UsunRozkazCommand.ExecuteAsync(rozkaz);
            }
        }
    }

    private void RokCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RokCombo.SelectedItem is int rok && _vm is not null)
        {
            _ = _vm.ZmienRokCommand.ExecuteAsync(rok);
        }
    }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Session is null || !_vm.Session.CanEditAll)
        {
            return;
        }

        var win = new SettingsWindow(_vm.Session);
        if (OwnerWindow is not null)
        {
            win.Owner = OwnerWindow;
        }

        win.ShowDialog();
        await _vm.OdswiezPoUstawieniachAsync();
        ApplyEditorAccess();
    }

    private void Logout_Click(object sender, RoutedEventArgs e) => LogoutRequested?.Invoke(this, EventArgs.Empty);
}

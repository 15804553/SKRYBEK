using System.Windows;
using System.Windows.Controls;
using SKRYBEK.App.ViewModels;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.Views;

public partial class MainWindow : Window
{
    private MainViewModel _vm = null!;

    public MainWindow()
    {
        InitializeComponent();
    }

    public async Task InitializeAsync(SessionInfo session)
    {
        _vm = new MainViewModel(session);
        DataContext = _vm;

        UserLabel.Text = $"{session.Login}  |  {session.NazwaZmiany}";

        // Wypełnij combo lat
        var lata = Enumerable.Range(DateTime.Today.Year - 2, 5).Reverse().ToList();
        RokCombo.ItemsSource = lata;
        RokCombo.SelectedItem = DateTime.Today.Year;

        await _vm.LoadAsync();
    }

    private void RozkazyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RozkazyList.SelectedItem is RozkazDzienny rozkaz)
            _ = _vm.OtworzRozkazCommand.ExecuteAsync(rozkaz);
    }

    private void DeleteRozkaz_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RozkazDzienny rozkaz })
        {
            var result = MessageBox.Show(
                $"Czy na pewno usunąć rozkaz Nr {rozkaz.NumerFormatowany} z dnia {rozkaz.DataFormatowana}?",
                "Usuń rozkaz",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
                _ = _vm.UsunRozkazCommand.ExecuteAsync(rozkaz);
        }
    }

    private void RokCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RokCombo.SelectedItem is int rok && _vm is not null)
            _ = _vm.ZmienRokCommand.ExecuteAsync(rok);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow(_vm.Session!);
        win.Owner = this;
        win.ShowDialog();
        _ = _vm.LoadAsync();
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        _vm.Logout();
        var login = new LoginWindow();
        if (login.ShowDialog() == true && login.Session is not null)
        {
            _ = InitializeAsync(login.Session);
        }
        else
        {
            Application.Current.Shutdown();
        }
    }
}

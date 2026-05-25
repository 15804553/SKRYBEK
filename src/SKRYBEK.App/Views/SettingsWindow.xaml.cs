using System.Windows;
using Microsoft.Win32;
using SKRYBEK.App.ViewModels;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsWindow(SessionInfo session)
    {
        InitializeComponent();
        _vm = new SettingsViewModel(session);
        DataContext = _vm;
        _ = _vm.LoadAsync();
    }

    private void BrowseBoberDb_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title            = "Wybierz bazę BOBER",
            Filter           = "Access Database|*.accdb|Wszystkie|*.*",
            FileName         = _vm.SciezkaBoberBazy
        };
        if (dlg.ShowDialog() == true)
            _vm.SciezkaBoberBazy = dlg.FileName;
    }

    private void BrowseChomikDb_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title    = "Wybierz bazę CHOMIK",
            Filter   = "Access Database|*.accdb|Wszystkie|*.*",
            FileName = _vm.SciezkaChomikBazy
        };
        if (dlg.ShowDialog() == true)
            _vm.SciezkaChomikBazy = dlg.FileName;
    }
}

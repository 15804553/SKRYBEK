using System.Windows;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.Views;

public partial class SettingsWindow : Window
{
    public SkrybekSettingsView SettingsView { get; }

    public SettingsWindow(SessionInfo session)
    {
        InitializeComponent();
        SettingsView = new SkrybekSettingsView(session);
        SettingsHost.Content = SettingsView;
    }
}

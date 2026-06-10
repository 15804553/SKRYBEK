using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SKRYBEK.App.Helpers;

namespace SKRYBEK.App.Views;

public partial class SkrybekMessageDialog : Window
{
    private readonly SkrybekMessageButtons _buttons;

    public SkrybekMessageResult Result { get; private set; } = SkrybekMessageResult.None;

    public SkrybekMessageDialog(string message, string title, SkrybekMessageButtons buttons, SkrybekMessageKind kind)
    {
        InitializeComponent();
        _buttons = buttons;
        Title = title;
        TitleBar.Title = title;
        MessageText.Text = message;
        UstawIkone(kind);
        ZbudujPrzyciski(buttons);
        KeyDown += OnKeyDown;
        Closing += (_, _) =>
        {
            if (Result != SkrybekMessageResult.None) return;
            Result = buttons == SkrybekMessageButtons.YesNo
                ? SkrybekMessageResult.No
                : SkrybekMessageResult.OK;
        };
    }

    private void UstawIkone(SkrybekMessageKind kind)
    {
        switch (kind)
        {
            case SkrybekMessageKind.Warning:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x5A, 0x4A, 0x20));
                IconText.Foreground = (Brush)FindResource("AccentLightBrush");
                IconText.Text = "!";
                break;
            case SkrybekMessageKind.Error:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x5A, 0x28, 0x28));
                IconText.Foreground = (Brush)FindResource("AlertBrush");
                IconText.Text = "✕";
                break;
            case SkrybekMessageKind.Question:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x4A));
                IconText.Foreground = (Brush)FindResource("AccentLightBrush");
                IconText.Text = "?";
                break;
            default:
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x4A));
                IconText.Foreground = (Brush)FindResource("AccentLightBrush");
                IconText.Text = "i";
                break;
        }
    }

    private void ZbudujPrzyciski(SkrybekMessageButtons buttons)
    {
        ButtonsPanel.Children.Clear();

        if (buttons == SkrybekMessageButtons.YesNo)
        {
            DodajPrzycisk("Nie", secondary: true, SkrybekMessageResult.No);
            DodajPrzycisk("Tak", secondary: false, SkrybekMessageResult.Yes, isDefault: true);
            return;
        }

        DodajPrzycisk("OK", secondary: false, SkrybekMessageResult.OK, isDefault: true);
    }

    private void DodajPrzycisk(string tekst, bool secondary, SkrybekMessageResult wynik, bool isDefault = false)
    {
        var btn = new Button
        {
            Content = tekst,
            Style = (Style)FindResource(secondary ? "SecondaryButton" : "PrimaryButton"),
            MinWidth = 88,
            Padding = new Thickness(16, 6, 16, 6),
            Margin = new Thickness(ButtonsPanel.Children.Count > 0 ? 8 : 0, 0, 0, 0),
            IsDefault = isDefault
        };
        btn.Click += (_, _) => ZamknijZWynikiem(wynik);
        ButtonsPanel.Children.Add(btn);
    }

    private void ZamknijZWynikiem(SkrybekMessageResult wynik)
    {
        Result = wynik;
        DialogResult = true;
        Close();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;

        Result = _buttons == SkrybekMessageButtons.YesNo
            ? SkrybekMessageResult.No
            : SkrybekMessageResult.OK;
        DialogResult = true;
        Close();
        e.Handled = true;
    }
}

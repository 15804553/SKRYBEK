using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SKRYBEK.App.Views.Chrome;

public partial class SkrybekTitleBar : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SkrybekTitleBar),
            new PropertyMetadata("SKRYBEK", OnTitleChanged));

    public static readonly DependencyProperty ShowMinimizeButtonProperty =
        DependencyProperty.Register(nameof(ShowMinimizeButton), typeof(bool), typeof(SkrybekTitleBar),
            new PropertyMetadata(true, OnButtonVisibilityChanged));

    public static readonly DependencyProperty ShowMaximizeButtonProperty =
        DependencyProperty.Register(nameof(ShowMaximizeButton), typeof(bool), typeof(SkrybekTitleBar),
            new PropertyMetadata(true, OnButtonVisibilityChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool ShowMinimizeButton
    {
        get => (bool)GetValue(ShowMinimizeButtonProperty);
        set => SetValue(ShowMinimizeButtonProperty, value);
    }

    public bool ShowMaximizeButton
    {
        get => (bool)GetValue(ShowMaximizeButtonProperty);
        set => SetValue(ShowMaximizeButtonProperty, value);
    }

    public SkrybekTitleBar()
    {
        InitializeComponent();
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SkrybekTitleBar tb)
            tb.TitleText.Text = e.NewValue?.ToString() ?? "SKRYBEK";
    }

    private static void OnButtonVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SkrybekTitleBar tb)
        {
            tb.MinimizeButton.Visibility = tb.ShowMinimizeButton ? Visibility.Visible : Visibility.Collapsed;
            tb.MaximizeButton.Visibility = tb.ShowMaximizeButton ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private Window? GetWindow() => Window.GetWindow(this);

    private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            ToggleMaximize();
        else
            GetWindow()?.DragMove();
    }

    private void DragArea_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            GetWindow()?.DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (GetWindow() is { } w)
            w.WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => GetWindow()?.Close();

    private void ToggleMaximize()
    {
        if (GetWindow() is not { } w) return;
        w.WindowState = w.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}

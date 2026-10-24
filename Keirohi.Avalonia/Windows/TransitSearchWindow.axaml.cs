using Avalonia.Controls;

namespace Keirohi.Avalonia.Windows;

public partial class TransitSearchWindow : Window
{
    public TransitSearchWindow(Uri searchUri, string routeLabel)
    {
        InitializeComponent();
        Title = $"Yahoo!乗換案内 — {routeLabel}";
        RouteLabel.Text = routeLabel;
        Opened += (_, _) => SearchWebView.Url = searchUri;
    }
}

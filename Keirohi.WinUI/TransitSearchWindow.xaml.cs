using Microsoft.UI.Xaml;

namespace Keirohi_App;

public sealed partial class TransitSearchWindow : Window
{
    private readonly Uri searchUri;
    private bool webViewInitialized;

    public TransitSearchWindow(Uri searchUri, string routeLabel)
    {
        this.searchUri = searchUri;
        InitializeComponent();
        RouteLabel.Text = routeLabel;
        Title = $"Yahoo!乗換案内 — {routeLabel}";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(960, 720));
        Activated += OnActivated;
    }

    private async void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (webViewInitialized)
        {
            return;
        }

        webViewInitialized = true;
        await SearchWebView.EnsureCoreWebView2Async();
        SearchWebView.Source = searchUri;
    }
}

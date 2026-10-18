using Keirohi_App.Models;
using Microsoft.UI.Xaml;

namespace Keirohi_App.Helpers;

public static class TransitSearchLauncher
{
    public static async Task OpenYahooTransitAsync(
        XamlRoot xamlRoot,
        string departure,
        string arrival,
        TransportMode mode = TransportMode.Train)
    {
        if (!YahooTransitSearchHelper.TryBuildSearchUri(departure, arrival, out var uri, mode))
        {
            await DialogHelper.ShowOkAsync(xamlRoot, "Yahoo!乗換案内", "出発と到着を入力してください。");
            return;
        }

        var label = $"{departure.Trim()} → {arrival.Trim()}";
        var window = new TransitSearchWindow(uri, label);
        window.Activate();
    }
}

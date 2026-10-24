using Avalonia.Threading;
using Keirohi_App.Helpers;
using Keirohi_App.Models;
using Keirohi.Avalonia.Windows;

namespace Keirohi.Avalonia.Helpers;

public static class TransitSearchLauncher
{
    public static Task OpenYahooTransitAsync(string departure, string arrival, TransportMode mode = TransportMode.Train)
    {
        if (!YahooTransitSearchHelper.TryBuildSearchUri(departure, arrival, out var uri, mode))
        {
            return DialogService.ShowOkAsync("Yahoo!乗換案内", "出発と到着を入力してください。");
        }

#if WINDOWS
        var label = $"{departure.Trim()} → {arrival.Trim()}";
        Dispatcher.UIThread.Post(() =>
        {
            var form = new TransitSearchForm(uri, label);
            form.Show();
        });
#else
        var label = $"{departure.Trim()} → {arrival.Trim()}";
        var owner = AppHost.MainWindowInstance;
        Dispatcher.UIThread.Post(() =>
        {
            var window = new TransitSearchWindow(uri, label);
            if (owner is not null)
            {
                window.Show(owner);
            }
            else
            {
                window.Show();
            }
        });
#endif

        return Task.CompletedTask;
    }
}

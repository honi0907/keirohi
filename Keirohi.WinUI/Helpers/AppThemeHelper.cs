using Keirohi_App.Models;
using Microsoft.UI.Xaml;

namespace Keirohi_App.Helpers;

public static class AppThemeHelper
{
    public static void Apply(AppSettings settings) => ApplyPreference(settings.ThemePreference);

    public static void ApplyPreference(string? preference)
    {
        if (App.MainWindowInstance?.Content is not FrameworkElement root)
        {
            return;
        }

        root.RequestedTheme = ToElementTheme(preference);
    }

    public static ElementTheme ToElementTheme(string? preference) =>
        preference switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
}

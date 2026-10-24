using Avalonia;
using Avalonia.Styling;
using Keirohi_App.Models;

namespace Keirohi.Avalonia.Helpers;

public static class AppThemeHelper
{
    public static void Apply(AppSettings settings) => ApplyPreference(settings.ThemePreference);

    public static void ApplyPreference(string? preference)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = preference switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}

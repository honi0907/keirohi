using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Keirohi_App.Models;

namespace Keirohi.Avalonia.Helpers;

public static class AppThemeHelper
{
    public static void Apply(AppSettings settings) => ApplyPreference(settings.ThemePreference);

    public static void ApplyPreference(string? preference)
    {
        if (Application.Current is not { } app)
        {
            return;
        }

        var variant = ToVariant(preference);
        app.RequestedThemeVariant = variant;

        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: Window main })
        {
            main.RequestedThemeVariant = variant;
            ApplyToOwnedWindows(main, variant);
        }
    }

    private static void ApplyToOwnedWindows(Window window, ThemeVariant variant)
    {
        foreach (var owned in window.OwnedWindows)
        {
            if (owned is not Window child)
            {
                continue;
            }

            child.RequestedThemeVariant = variant;
            ApplyToOwnedWindows(child, variant);
        }
    }

    public static ThemeVariant ToVariant(string? preference) =>
        preference switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
}

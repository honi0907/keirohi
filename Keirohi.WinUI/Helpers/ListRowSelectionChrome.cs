using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Keirohi_App.Helpers;

public static class ListRowSelectionChrome
{
    public static SolidColorBrush RowOutlineBrush { get; } = new(Color.FromArgb(255, 180, 180, 180));

    private static SolidColorBrush? selectionBorderBrush;

    public static SolidColorBrush BorderBrush(bool isSelected) =>
        isSelected ? GetSelectionBorderBrush() : RowOutlineBrush;

    public static Thickness BorderThickness(bool isSelected) =>
        isSelected ? new Thickness(2) : new Thickness(1);

    private static SolidColorBrush GetSelectionBorderBrush()
    {
        if (selectionBorderBrush is not null)
        {
            return selectionBorderBrush;
        }

        if (Application.Current.Resources.TryGetValue("SystemAccentColor", out var resource) &&
            resource is Color accent)
        {
            selectionBorderBrush = new SolidColorBrush(accent);
            return selectionBorderBrush;
        }

        selectionBorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212));
        return selectionBorderBrush;
    }
}

using Avalonia;
using Avalonia.Media;

namespace Keirohi.Avalonia.Helpers;

public static class ListRowSelectionChrome
{
    public static IBrush RowOutlineBrush { get; } = new SolidColorBrush(Color.Parse("#FFB4B4B4"));

    private static IBrush? selectionBorderBrush;

    public static IBrush BorderBrush(bool isSelected) =>
        isSelected ? GetSelectionBorderBrush() : RowOutlineBrush;

    public static Thickness BorderThickness(bool isSelected) =>
        isSelected ? new Thickness(2) : new Thickness(1);

    private static IBrush GetSelectionBorderBrush()
    {
        selectionBorderBrush ??= new SolidColorBrush(Color.Parse("#FF0078D4"));
        return selectionBorderBrush;
    }
}

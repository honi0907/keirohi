using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Keirohi_App.Helpers;

public static class EntryRowBackgroundBrushes
{
    public static SolidColorBrush Default { get; } = new(Color.FromArgb(255, 255, 255, 255));

    public static SolidColorBrush Alternate { get; } = new(Color.FromArgb(255, 232, 244, 252));
}

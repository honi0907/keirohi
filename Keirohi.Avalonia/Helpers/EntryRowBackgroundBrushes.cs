using Avalonia.Media;

namespace Keirohi.Avalonia.Helpers;

public static class EntryRowBackgroundBrushes
{
    public static IBrush Default { get; } = new SolidColorBrush(Color.Parse("#FFFFFFFF"));

    public static IBrush Alternate { get; } = new SolidColorBrush(Color.Parse("#FFE8F4FC"));
}

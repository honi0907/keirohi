using Avalonia.Media;

namespace Keirohi.Avalonia.Helpers;

public static class EntryRowBackgroundBrushes
{
    /// <summary>日付グループ（偶数）。手入力・PDF 共通。</summary>
    public static IBrush DateBandA { get; } = new SolidColorBrush(Color.Parse("#FFFFFFFF"));

    /// <summary>日付グループ（奇数）。手入力・PDF 共通。</summary>
    public static IBrush DateBandB { get; } = new SolidColorBrush(Color.Parse("#FFE8F4FC"));

    public static IBrush Default => DateBandA;

    public static IBrush Alternate => DateBandB;
}

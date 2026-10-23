using Avalonia.Media;

namespace Keirohi.Avalonia.Helpers;

public static class RoutePatternListColorHelper
{
    public sealed record Preset(string Label, string? Hex);

    public static IReadOnlyList<Preset> Presets { get; } =
    [
        new("色なし", null),
        new("水色", "#FFB3E5FC"),
        new("緑", "#FFA5D6A7"),
        new("黄", "#FFFFF59D"),
        new("橙", "#FFFFCC80"),
        new("紫", "#FFCE93D8"),
        new("ピンク", "#FFF48FB1"),
        new("グレー", "#FFB0BEC5"),
        new("赤", "#FFEF9A9A"),
        new("青", "#FF90CAF9"),
        new("紺", "#FF9FA8DA"),
        new("青緑", "#FF80CBC4"),
        new("黄緑", "#FFC5E1A5"),
        new("茶", "#FFBCAAA4"),
        new("ベージュ", "#FFFFE0B2"),
    ];

    private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);

    public static IBrush ToRowBackgroundBrush(string? hex)
    {
        if (!TryParseColor(hex, out var color))
        {
            return TransparentBrush;
        }

        return new SolidColorBrush(color);
    }

    public static IBrush ToAccentBarBrush(string? hex)
    {
        if (!TryParseColor(hex, out var color))
        {
            return TransparentBrush;
        }

        var accent = Color.FromArgb(
            255,
            (byte)(color.R * 0.45),
            (byte)(color.G * 0.45),
            (byte)(color.B * 0.45));
        return new SolidColorBrush(accent);
    }

    private static bool TryParseColor(string? hex, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        var value = hex.Trim();
        if (value.StartsWith('#'))
        {
            value = value[1..];
        }

        if (value.Length == 6)
        {
            value = "FF" + value;
        }

        if (value.Length != 8 || !uint.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out var argb))
        {
            return false;
        }

        color = Color.FromArgb(
            (byte)(argb >> 24),
            (byte)(argb >> 16),
            (byte)(argb >> 8),
            (byte)argb);
        return true;
    }
}

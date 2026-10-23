namespace Keirohi_App.Helpers;

public static class IsoDateFormats
{
    public static string ToIsoString(DateTimeOffset? date) =>
        date?.LocalDateTime.ToString("yyyy-MM-dd") ?? "";

    public static DateTimeOffset? ParseIso(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso))
        {
            return null;
        }

        return DateTime.TryParse(iso.Trim(), out var parsed)
            ? new DateTimeOffset(DateTime.SpecifyKind(parsed.Date, DateTimeKind.Local))
            : null;
    }

    public static DateTimeOffset TodayLocal() => DateTimeOffset.Now.Date;
}

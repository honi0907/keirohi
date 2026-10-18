using Microsoft.UI.Xaml.Controls;

namespace Keirohi_App.Helpers;

public static class IsoDateHelper
{
    public static string ToIsoString(DateTimeOffset? date) =>
        date?.LocalDateTime.ToString("yyyy-MM-dd") ?? "";

    public static void SetFromIso(CalendarDatePicker picker, string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso))
        {
            picker.Date = null;
            return;
        }

        if (DateTime.TryParse(iso.Trim(), out var parsed))
        {
            picker.Date = new DateTimeOffset(DateTime.SpecifyKind(parsed.Date, DateTimeKind.Local));
        }
    }

    public static void EnsureTodayIfEmpty(CalendarDatePicker picker)
    {
        if (picker.Date is null)
        {
            picker.Date = DateTimeOffset.Now.Date;
        }
    }
}

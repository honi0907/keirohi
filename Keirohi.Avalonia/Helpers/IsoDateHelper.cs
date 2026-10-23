using Avalonia.Controls;
using Keirohi_App.Helpers;

namespace Keirohi.Avalonia.Helpers;

public static class IsoDateHelper
{
    public static string ToIsoString(DateTimeOffset? date) =>
        date?.LocalDateTime.ToString("yyyy-MM-dd") ?? "";

    public static string ToIsoString(DateTime? date) =>
        date?.ToString("yyyy-MM-dd") ?? "";

    public static void SetFromIso(CalendarDatePicker picker, string? iso) =>
        picker.SelectedDate = IsoDateFormats.ParseIso(iso)?.LocalDateTime.Date;

    public static void EnsureTodayIfEmpty(CalendarDatePicker picker)
    {
        if (picker.SelectedDate is null)
        {
            picker.SelectedDate = DateTime.Today;
        }
    }
}

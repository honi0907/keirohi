using Keirohi_App.Models;

namespace Keirohi_App.Services;

public static class DateFormatHelper
{
    public static (string Month, string Day) FormatSuicaMonthDay(string iso)
    {
        if (DateTime.TryParse(iso, out var dt))
        {
            return (dt.Month.ToString("00"), dt.Day.ToString("00"));
        }
        return ("", "");
    }

    public static string FormatSuicaStatementDate(string iso)
    {
        if (DateTime.TryParse(iso, out var dt))
        {
            return $"{dt.Year}/{dt.Month}/{dt.Day}";
        }
        return iso;
    }

    public static string FormatPeriodLabel(IEnumerable<TripEntry> entries)
    {
        var sorted = entries.OrderBy(e => e.Date, StringComparer.Ordinal).ToList();
        if (sorted.Count == 0)
        {
            return "0000~0000";
        }
        static string Fmt(DateTime d) => $"{d.Month:00}{d.Day:00}";
        var first = DateTime.Parse(sorted[0].Date);
        var last = DateTime.Parse(sorted[^1].Date);
        return $"{Fmt(first)}~{Fmt(last)}";
    }

    /// <summary>一覧の表示順で、カレンダー上の日付が逆転している箇所があるか。</summary>
    public static bool HasDateOrderAnomalyInDisplayOrder(IEnumerable<TripEntry> entriesInDisplayOrder)
    {
        DateTime? previous = null;
        foreach (var entry in entriesInDisplayOrder)
        {
            if (!DateTime.TryParse(entry.Date, out var current))
            {
                continue;
            }

            if (previous is not null && current < previous.Value)
            {
                return true;
            }

            previous = current;
        }

        return false;
    }

    public static List<TripEntry> EntriesForSuicaSheet(IEnumerable<TripEntry> entries)
    {
        var list = entries as IList<TripEntry> ?? entries.ToList();
        return list
            .Select((entry, index) => (entry, index))
            .OrderBy(x => x.entry.Date, StringComparer.Ordinal)
            .ThenBy(x => x.index)
            .Take(AppStateService.MaxSuicaEntries)
            .Select(x => x.entry)
            .ToList();
    }
}

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

    /// <summary>明細シートの入金・利用額（利用はマイナス表記）。</summary>
    public static string FormatSuicaSheetAmount(int amount) => $"-{Math.Abs(amount)}";

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

    /// <summary>保存セットの自動名称（例: 2026 0901~0930）。</summary>
    public static string FormatEntrySetAutoName(IEnumerable<TripEntry> entries)
    {
        var sorted = entries
            .Where(e => DateTime.TryParse(e.Date, out _))
            .OrderBy(e => e.Date, StringComparer.Ordinal)
            .ToList();
        if (sorted.Count == 0)
        {
            return "";
        }

        var first = DateTime.Parse(sorted[0].Date);
        var last = DateTime.Parse(sorted[^1].Date);
        static string MmDd(DateTime d) => $"{d.Month:00}{d.Day:00}";
        if (first.Year == last.Year)
        {
            return $"{first.Year} {MmDd(first)}~{MmDd(last)}";
        }

        return $"{first.Year} {MmDd(first)}~{last.Year} {MmDd(last)}";
    }

    /// <summary>一覧の表示順で、カレンダー上の日付が逆転している箇所があるか。</summary>
    public static bool HasDateOrderAnomalyInDisplayOrder(IEnumerable<TripEntry> entriesInDisplayOrder)
    {
        return GetDateOrderAnomalyEntryIds(entriesInDisplayOrder).Count > 0;
    }

    /// <summary>表示順で前の行より日付が古い（逆転している）明細の Id。</summary>
    public static HashSet<string> GetDateOrderAnomalyEntryIds(IEnumerable<TripEntry> entriesInDisplayOrder)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        DateTime? previous = null;
        foreach (var entry in entriesInDisplayOrder)
        {
            if (!DateTime.TryParse(entry.Date, out var current))
            {
                continue;
            }

            if (previous is not null && current < previous.Value)
            {
                ids.Add(entry.Id);
            }

            previous = current;
        }

        return ids;
    }

    public static List<TripEntry> EntriesForSuicaSheet(IEnumerable<TripEntry> entries)
    {
        var list = entries as IList<TripEntry> ?? entries.ToList();
        return list
            .Select((entry, index) => (entry, index))
            .OrderBy(x => x.entry.Date, StringComparer.Ordinal)
            .ThenBy(x => x.entry.FromMobileSuicaPdf ? x.entry.PdfImportOrder : x.index)
            .Take(AppStateService.MaxSuicaEntries)
            .Select(x => x.entry)
            .ToList();
    }
}

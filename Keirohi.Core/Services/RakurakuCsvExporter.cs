using System.Text;
using Keirohi_App.Models;

namespace Keirohi_App.Services;

public static class RakurakuCsvExporter
{
    private static readonly Encoding RakurakuEncoding;

    static RakurakuCsvExporter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        RakurakuEncoding = Encoding.GetEncoding(932);
    }

    public static string BuildCsv(IEnumerable<TripEntry> entries)
    {
        var sorted = entries
            .OrderBy(e => e.Date, StringComparer.Ordinal)
            .ThenBy(e => e.Id, StringComparer.Ordinal)
            .ToList();
        var lines = new List<string> { "日付,目的地,出発,到着,備考,金額・km" };
        foreach (var entry in sorted)
        {
            lines.Add(string.Join(',', new[]
            {
                Escape(formatRakurakuDate(entry.Date)),
                Escape(entry.Destination),
                Escape(entry.Departure),
                Escape(entry.Arrival),
                Escape(entry.Note),
                Escape(entry.Amount.ToString()),
            }));
        }
        lines.AddRange(["", "", ""]);
        return string.Join("\r\n", lines);
    }

    public static async Task SaveAsync(IEnumerable<TripEntry> entries, string filePath)
    {
        var body = RakurakuEncoding.GetBytes(BuildCsv(entries));
        await File.WriteAllBytesAsync(filePath, body);
    }

    private static string formatRakurakuDate(string isoDate)
    {
        if (DateTime.TryParse(isoDate, out var dt))
        {
            return dt.ToString("yyyy/M/d");
        }
        return isoDate;
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

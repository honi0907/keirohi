using System.Text.RegularExpressions;
using Keirohi_App.Models;

namespace Keirohi_App.Services;

public static class RakurakuCsvImporter
{
    private static readonly Regex RakurakuDate = new(
        @"^(\d{4})/(\d{1,2})/(\d{1,2})$",
        RegexOptions.Compiled);

    public static List<TripEntry> Parse(string text)
    {
        var normalized = text.TrimStart('\uFEFF');
        var lines = normalized.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
        if (lines.Count < 2)
        {
            return [];
        }

        var entries = new List<TripEntry>();
        for (var i = 1; i < lines.Count; i++)
        {
            var fields = ParseCsvLine(lines[i]);
            if (fields.Count < 6)
            {
                continue;
            }

            var iso = RakurakuDateToIso(fields[0]);
            if (iso is null || !int.TryParse(fields[5], out var amount))
            {
                continue;
            }

            entries.Add(new TripEntry
            {
                Id = Guid.NewGuid().ToString(),
                Date = iso,
                Destination = fields[1],
                Departure = fields[2],
                Arrival = fields[3],
                Note = fields[4],
                Amount = amount,
                Mode = TransportMode.Train,
            });
        }

        return entries;
    }

    public static List<TripEntry> MergeImported(IEnumerable<TripEntry> existing, IEnumerable<TripEntry> imported)
    {
        var list = existing.ToList();
        list.AddRange(imported);
        return list;
    }

    private static string? RakurakuDateToIso(string raw)
    {
        var match = RakurakuDate.Match(raw.Trim());
        if (!match.Success)
        {
            return null;
        }

        var y = int.Parse(match.Groups[1].Value);
        var m = int.Parse(match.Groups[2].Value);
        var d = int.Parse(match.Groups[3].Value);
        return $"{y}-{m:00}-{d:00}";
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = "";
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current += ch;
                }
                continue;
            }

            if (ch == '"')
            {
                inQuotes = true;
            }
            else if (ch == ',')
            {
                fields.Add(current);
                current = "";
            }
            else
            {
                current += ch;
            }
        }

        fields.Add(current);
        return fields;
    }
}

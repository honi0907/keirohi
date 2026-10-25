using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Keirohi_App.Models;
using UglyToad.PdfPig;

namespace Keirohi_App.Services;

public static class MobileSuicaPdfImporter
{
    private static readonly Regex PrintDateRegex = new(
        @"(\d{4})/(\d{1,2})/(\d{1,2})",
        RegexOptions.Compiled);

    private static readonly Regex TrainLineRegex = new(
        @"^([+-][\d,]+)\s+(\d{1,2})\s+[＊\*]?入\s+(.+?)\s+出\s+(.+?)\s+\\([\d,]+)\s+(\d{1,2})\s*$",
        RegexOptions.Compiled);

    private static readonly Regex BusLineRegex = new(
        @"^([+-][\d,]+)\s+(\d{1,2})\s+(?:ﾊﾞｽ等|バス等)\s+(.+?)\s+\\([\d,]+)\s+(\d{1,2})\s*$",
        RegexOptions.Compiled);

    /// <summary>PdfPig が改行なしで連結する「月日＋入/出＋利用額末尾」形式。</summary>
    private static readonly Regex CompactEntryRegex = new(
        @"(\d{4})(?:[＊\*]?入(.+?)出(.+?)|(?:ﾊﾞｽ等|バス等)(.+?))(\d+)(?=\d{4}(?:[＊\*]?入|(?:ﾊﾞｽ等|バス等))|$)",
        RegexOptions.Compiled);

    /// <summary>公式 PDF（列順: 利用額・月・種別…・残高\・日）が PdfPig で連結される形式。</summary>
    private static readonly Regex ColumnarAmountPrefix = new(
        @"[+-](?:\d{1,3}(?:,\d{3})+(?=\d{2}(?:[＊\*]?入|(?:ﾊﾞｽ等|バス等)|物販|現金|繰))|\d+(?=\d{2}(?:[＊\*]?入|(?:ﾊﾞｽ等|バス等)|物販|現金|繰)))",
        RegexOptions.Compiled);

    private static readonly Regex ColumnarTrainRegex = new(
        @"([+-](?:\d{1,3}(?:,\d{3})+(?=\d{2}[＊\*]?入)|\d+(?=\d{2}[＊\*]?入)))(\d{2})[＊\*]?入([^\\]+?)出([^\\]+?)\\([\d,]+)(\d{2})(?=[+-]|入金|残高|ご利用|\d{4}/|$)",
        RegexOptions.Compiled);

    private static readonly Regex ColumnarBusRegex = new(
        @"([+-](?:\d{1,3}(?:,\d{3})+(?=\d{2}(?:ﾊﾞｽ等|バス等))|\d+(?=\d{2}(?:ﾊﾞｽ等|バス等))))(\d{2})(?:ﾊﾞｽ等|バス等)([^\\]+?)\\([\d,]+)(\d{2})(?=[+-]|入金|残高|ご利用|\d{4}/|$)",
        RegexOptions.Compiled);

    public static MobileSuicaPdfParseResult ParseFile(string pdfPath)
    {
        using var document = PdfDocument.Open(pdfPath);
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return ParseText(builder.ToString());
    }

    public static MobileSuicaPdfParseResult ParseText(string text)
    {
        var referenceDate = ResolveReferenceDate(text);
        var entries = new List<TripEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var skipped = 0;
        var pdfOrder = 0;

        if (TryParseCompactHistory(text, referenceDate, seen, entries, ref pdfOrder, out var compactSkipped))
        {
            SortEntriesForImport(entries);
            return new MobileSuicaPdfParseResult
            {
                Entries = entries,
                SkippedLines = compactSkipped,
            };
        }

        if (TryParseColumnarHistory(text, referenceDate, seen, entries, ref pdfOrder, out var columnarSkipped))
        {
            SortEntriesForImport(entries);
            return new MobileSuicaPdfParseResult
            {
                Entries = entries,
                SkippedLines = columnarSkipped,
            };
        }

        foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = NormalizeLine(rawLine);
            if (line.Length == 0)
            {
                continue;
            }

            if (TryParseTrain(line, referenceDate, out var train))
            {
                if (TryAddUnique(seen, train, entries, ref pdfOrder))
                {
                    continue;
                }

                skipped++;
                continue;
            }

            if (TryParseBus(line, referenceDate, out var bus))
            {
                if (TryAddUnique(seen, bus, entries, ref pdfOrder))
                {
                    continue;
                }

                skipped++;
                continue;
            }

            if (LooksLikeSuicaDataLine(line))
            {
                skipped++;
            }
        }

        SortEntriesForImport(entries);

        return new MobileSuicaPdfParseResult
        {
            Entries = entries,
            SkippedLines = skipped,
        };
    }

    private static void SortEntriesForImport(List<TripEntry> entries) =>
        entries.Sort(static (a, b) =>
        {
            var dateCompare = string.Compare(a.Date, b.Date, StringComparison.Ordinal);
            return dateCompare != 0
                ? dateCompare
                : a.PdfImportOrder.CompareTo(b.PdfImportOrder);
        });

    private static bool TryParseCompactHistory(
        string text,
        DateTime referenceDate,
        HashSet<string> seen,
        List<TripEntry> entries,
        ref int pdfOrder,
        out int skipped)
    {
        skipped = 0;
        var compact = Regex.Replace(text, @"[\s\r\n　]+", "");
        if (!compact.Contains("入金・利用額", StringComparison.Ordinal) ||
            !Regex.IsMatch(compact, @"\d{4}[＊\*]?入"))
        {
            return false;
        }

        var start = compact.IndexOf("入金・利用額", StringComparison.Ordinal) + "入金・利用額".Length;
        var end = compact.IndexOf("ご利用", StringComparison.Ordinal);
        var history = end > start ? compact[start..end] : compact[start..];

        foreach (Match match in CompactEntryRegex.Matches(history))
        {
            if (!TryParseCompactDate(match.Groups[1].Value, referenceDate, out var iso))
            {
                skipped++;
                continue;
            }

            if (!int.TryParse(match.Groups[5].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount) ||
                amount <= 0)
            {
                skipped++;
                continue;
            }

            TripEntry entry;
            if (match.Groups[2].Success && match.Groups[2].Length > 0)
            {
                entry = CreateEntry(
                    iso,
                    NormalizeStation(match.Groups[2].Value),
                    NormalizeStation(match.Groups[3].Value),
                    amount,
                    TransportMode.Train);
            }
            else
            {
                entry = CreateEntry(
                    iso,
                    NormalizeStation(match.Groups[4].Value),
                    "",
                    amount,
                    TransportMode.Bus);
            }

            if (!TryAddUnique(seen, entry, entries, ref pdfOrder))
            {
                skipped++;
            }
        }

        return entries.Count > 0;
    }

    private static bool TryParseColumnarHistory(
        string text,
        DateTime referenceDate,
        HashSet<string> seen,
        List<TripEntry> entries,
        ref int pdfOrder,
        out int skipped)
    {
        skipped = 0;
        var compact = Regex.Replace(text, @"[\s\r\n　]+", "");
        if (!ColumnarAmountPrefix.IsMatch(compact) ||
            !Regex.IsMatch(compact, @"[＊\*]?入.+?出.+?\\[\d,]+\d{1,2}(?=[+-]|$)"))
        {
            return false;
        }

        var orderedMatches = ColumnarTrainRegex.Matches(compact)
            .Cast<Match>()
            .Select(m => (Index: m.Index, Match: m, IsTrain: true))
            .Concat(
                ColumnarBusRegex.Matches(compact)
                    .Cast<Match>()
                    .Select(m => (Index: m.Index, Match: m, IsTrain: false)))
            .OrderBy(x => x.Index)
            .ToList();

        foreach (var item in orderedMatches)
        {
            var ok = item.IsTrain
                ? TryParseColumnarEntry(
                    item.Match,
                    referenceDate,
                    amountGroup: 1,
                    monthGroup: 2,
                    departureGroup: 3,
                    arrivalGroup: 4,
                    dayGroup: 6,
                    TransportMode.Train,
                    seen,
                    entries,
                    ref pdfOrder)
                : TryParseColumnarEntry(
                    item.Match,
                    referenceDate,
                    amountGroup: 1,
                    monthGroup: 2,
                    departureGroup: 3,
                    arrivalGroup: 0,
                    dayGroup: 5,
                    TransportMode.Bus,
                    seen,
                    entries,
                    ref pdfOrder);
            if (!ok)
            {
                skipped++;
            }
        }

        skipped += CountColumnarSkippedRows(compact);
        return entries.Count > 0;
    }

    private static int CountColumnarSkippedRows(string compact)
    {
        var skip = 0;
        var skipTypes = new Regex(
            @"[+-](?:\d{1,3}(?:,\d{3})+(?=\d{2}(?:物販|現金|繰))|\d+(?=\d{2}(?:物販|現金|繰)))\d{2}(?:物販|現金|繰)",
            RegexOptions.Compiled);
        skip += skipTypes.Matches(compact).Count;
        return skip;
    }

    private static bool TryParseColumnarEntry(
        Match match,
        DateTime referenceDate,
        int amountGroup,
        int monthGroup,
        int departureGroup,
        int arrivalGroup,
        int dayGroup,
        TransportMode mode,
        HashSet<string> seen,
        List<TripEntry> entries,
        ref int pdfOrder)
    {
        if (!TryParseUsageAmount(match.Groups[amountGroup].Value, out var amount))
        {
            return false;
        }

        var month = int.Parse(match.Groups[monthGroup].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups[dayGroup].Value, CultureInfo.InvariantCulture);
        if (!TryBuildIsoDate(referenceDate, month, day, out var iso))
        {
            return false;
        }

        var departure = NormalizeStation(match.Groups[departureGroup].Value);
        var arrival = arrivalGroup > 0 ? NormalizeStation(match.Groups[arrivalGroup].Value) : "";
        var entry = CreateEntry(iso, departure, arrival, amount, mode);
        return TryAddUnique(seen, entry, entries, ref pdfOrder);
    }

    private static bool TryParseCompactDate(string mmdd, DateTime referenceDate, out string iso)
    {
        iso = "";
        if (mmdd.Length != 4 ||
            !int.TryParse(mmdd.AsSpan(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var month) ||
            !int.TryParse(mmdd.AsSpan(2, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var day))
        {
            return false;
        }

        return TryBuildIsoDate(referenceDate, month, day, out iso);
    }

    private static bool TryAddUnique(
        HashSet<string> seen,
        TripEntry entry,
        List<TripEntry> entries,
        ref int pdfOrder)
    {
        var key = $"{entry.Date}|{entry.Mode}|{entry.Departure}|{entry.Arrival}|{entry.Amount}";
        if (!seen.Add(key))
        {
            return false;
        }

        entry.PdfImportOrder = pdfOrder++;
        entries.Add(entry);
        return true;
    }

    private static bool LooksLikeSuicaDataLine(string line) =>
        Regex.IsMatch(line, @"^[+-]?[\d,]+\s+\d{1,2}\s+");

    private static bool TryParseTrain(string line, DateTime referenceDate, out TripEntry entry)
    {
        entry = null!;
        var match = TrainLineRegex.Match(line);
        if (!match.Success)
        {
            return false;
        }

        if (!TryParseUsageAmount(match.Groups[1].Value, out var amount))
        {
            return false;
        }

        var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture);
        if (!TryBuildIsoDate(referenceDate, month, day, out var iso))
        {
            return false;
        }

        entry = CreateEntry(
            iso,
            NormalizeStation(match.Groups[3].Value),
            NormalizeStation(match.Groups[4].Value),
            amount,
            TransportMode.Train);
        return true;
    }

    private static bool TryParseBus(string line, DateTime referenceDate, out TripEntry entry)
    {
        entry = null!;
        var match = BusLineRegex.Match(line);
        if (!match.Success)
        {
            return false;
        }

        if (!TryParseUsageAmount(match.Groups[1].Value, out var amount))
        {
            return false;
        }

        var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture);
        if (!TryBuildIsoDate(referenceDate, month, day, out var iso))
        {
            return false;
        }

        entry = CreateEntry(
            iso,
            NormalizeStation(match.Groups[3].Value),
            "",
            amount,
            TransportMode.Bus);
        return true;
    }

    private static TripEntry CreateEntry(
        string isoDate,
        string departure,
        string arrival,
        int amount,
        TransportMode mode) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Date = isoDate,
            Departure = departure,
            Arrival = arrival,
            Destination = "",
            Note = "",
            Amount = amount,
            Mode = mode,
            FromMobileSuicaPdf = true,
        };

    private static bool TryParseUsageAmount(string raw, out int amount)
    {
        amount = 0;
        var normalized = raw.Replace(",", "", StringComparison.Ordinal).Trim();
        if (!int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var signed))
        {
            return false;
        }

        if (signed >= 0)
        {
            return false;
        }

        amount = Math.Abs(signed);
        return amount > 0;
    }

    private static string NormalizeLine(string raw) =>
        Regex.Replace(raw.Trim(), @"[\t　]+", " ");

    private static string NormalizeStation(string raw) =>
        Regex.Replace(raw.Trim(), @"\s+", " ");

    private static DateTime ResolveReferenceDate(string text)
    {
        DateTime? best = null;
        foreach (Match match in PrintDateRegex.Matches(text))
        {
            var y = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var m = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var d = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            try
            {
                var candidate = new DateTime(y, m, d);
                if (best is null || candidate > best)
                {
                    best = candidate;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // ignore invalid footer dates
            }
        }

        return best ?? DateTime.Today;
    }

    private static bool TryBuildIsoDate(DateTime referenceDate, int month, int day, out string iso)
    {
        iso = "";
        var year = referenceDate.Year;
        if (month > referenceDate.Month)
        {
            year--;
        }

        try
        {
            var date = new DateTime(year, month, day);
            iso = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}

using Keirohi_App.Models;

namespace Keirohi_App.Services;

public static class RouteSectionHistoryCatalog
{
    public static IReadOnlyList<RouteSectionHistoryEntry> Build(
        IEnumerable<EntrySet> entrySets,
        IReadOnlyList<FareMemoryRow> fareMemory)
    {
        var result = new List<RouteSectionHistoryEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        var timeline = new List<TripEntry>();
        foreach (var set in entrySets)
        {
            timeline.AddRange(set.Entries);
        }

        for (var i = timeline.Count - 1; i >= 0; i--)
        {
            var entry = timeline[i];
            if (!TryAddEntry(result, seen, entry.Mode, entry.Departure, entry.Arrival, entry.Amount))
            {
                continue;
            }
        }

        foreach (var row in fareMemory)
        {
            TryAddEntry(result, seen, TransportMode.Train, row.Departure, row.Arrival, row.Amount);
        }

        return result;
    }

    private static bool TryAddEntry(
        List<RouteSectionHistoryEntry> result,
        HashSet<string> seen,
        TransportMode mode,
        string departure,
        string arrival,
        int amount)
    {
        var dep = departure.Trim();
        var arr = arrival.Trim();
        if (dep.Length == 0)
        {
            return false;
        }

        if (mode == TransportMode.Train && arr.Length == 0)
        {
            return false;
        }

        if (amount <= 0)
        {
            return false;
        }

        var key = $"{mode}\u0001{dep}\u0001{arr}";
        if (!seen.Add(key))
        {
            return false;
        }

        result.Add(new RouteSectionHistoryEntry
        {
            Mode = mode,
            Departure = dep,
            Arrival = arr,
            Amount = amount,
        });
        return true;
    }
}

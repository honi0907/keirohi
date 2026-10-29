using Keirohi_App.Models;
using Keirohi_App.Services;

namespace Keirohi.Avalonia.Helpers;

public sealed record DestinationLegDraft(
    string Date,
    string Departure,
    string Arrival,
    TransportMode Mode,
    int Count,
    IReadOnlyList<string> EntryIds,
    string SuggestedDestination,
    bool IncludesMissingDestination);

public sealed record DestinationDateSection(
    string Date,
    IReadOnlyList<DestinationLegDraft> Legs);

public static class DestinationBatchPlanner
{
    private readonly record struct RouteKey(TransportMode Mode, string Departure, string Arrival);

    public static IReadOnlyList<DestinationDateSection> BuildSections(IReadOnlyList<TripEntry> entries)
    {
        if (entries.Count == 0)
        {
            return [];
        }

        var history = BuildHistoryLookup();
        var groups = entries
            .GroupBy(e => e.Date, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                var orderedEntries = g
                    .OrderBy(e => e.PdfImportOrder)
                    .ThenBy(e => e.Id, StringComparer.Ordinal)
                    .ToList();
                var legOrder = new List<RouteKey>();
                var idsByRoute = new Dictionary<RouteKey, List<string>>();
                var entryById = orderedEntries.ToDictionary(e => e.Id, StringComparer.Ordinal);
                foreach (var entry in orderedEntries)
                {
                    var key = new RouteKey(entry.Mode, entry.Departure, entry.Arrival);
                    if (!idsByRoute.TryGetValue(key, out var ids))
                    {
                        ids = [];
                        idsByRoute[key] = ids;
                        legOrder.Add(key);
                    }

                    ids.Add(entry.Id);
                }

                var legs = legOrder
                    .Select(key =>
                    {
                        var ids = idsByRoute[key];
                        var prefilled = ResolvePrefilledDestination(ids, entryById, history, key);
                        var missing = AnyEntryMissingDestination(ids, entryById);
                        return new DestinationLegDraft(
                            g.Key,
                            key.Departure,
                            key.Arrival,
                            key.Mode,
                            ids.Count,
                            ids,
                            prefilled,
                            missing);
                    })
                    .ToList();
                return new DestinationDateSection(g.Key, legs);
            })
            .ToList();

        return groups;
    }

    public static string? TryReverseArrowDestination(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Split('→', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return null;
        }

        return $"{parts[1]}→{parts[0]}";
    }

    public static bool IsReverseRoute(string depA, string arrA, string depB, string arrB) =>
        string.Equals(depA, arrB, StringComparison.Ordinal) &&
        string.Equals(arrA, depB, StringComparison.Ordinal);

    private static bool AnyEntryMissingDestination(
        IReadOnlyList<string> entryIds,
        IReadOnlyDictionary<string, TripEntry> entryById)
    {
        foreach (var id in entryIds)
        {
            if (!entryById.TryGetValue(id, out var entry))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Destination))
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolvePrefilledDestination(
        IReadOnlyList<string> entryIds,
        IReadOnlyDictionary<string, TripEntry> entryById,
        Dictionary<RouteKey, string> history,
        RouteKey key)
    {
        foreach (var id in entryIds)
        {
            if (!entryById.TryGetValue(id, out var entry))
            {
                continue;
            }

            var dest = entry.Destination?.Trim() ?? "";
            if (dest.Length > 0)
            {
                return dest;
            }
        }

        return history.TryGetValue(key, out var suggested) ? suggested : "";
    }

    private static Dictionary<RouteKey, string> BuildHistoryLookup()
    {
        var lookup = new Dictionary<RouteKey, string>();
        foreach (var set in AppHost.State.EntrySets)
        {
            foreach (var entry in set.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Destination))
                {
                    continue;
                }

                var key = new RouteKey(entry.Mode, entry.Departure, entry.Arrival);
                lookup[key] = entry.Destination.Trim();
            }
        }

        return lookup;
    }
}

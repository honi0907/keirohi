using Avalonia.Controls;
using Keirohi_App.Models;
using Keirohi.Avalonia.Windows;

namespace Keirohi.Avalonia.Helpers;

public static class DestinationBatchUiHelper
{
    public static async Task ShowForCurrentSetDestinationsAsync(Window owner)
    {
        var entries = AppHost.State.Entries.ToList();
        await ShowForEntriesAsync(owner, entries);
    }

    public static async Task ShowForImportedEntryIdsAsync(Window owner, IReadOnlyList<string> importedEntryIds)
    {
        if (importedEntryIds.Count == 0)
        {
            return;
        }

        var idSet = importedEntryIds.ToHashSet(StringComparer.Ordinal);
        var entries = new List<TripEntry>();
        foreach (var set in AppHost.State.EntrySets)
        {
            entries.AddRange(set.Entries.Where(e => idSet.Contains(e.Id)));
        }

        await ShowForEntriesAsync(owner, entries);
    }

    private static async Task ShowForEntriesAsync(Window owner, IReadOnlyList<TripEntry> entries)
    {
        if (entries.Count == 0)
        {
            await DialogService.ShowOkAsync(
                "目的地",
                "明細がありません。",
                owner);
            return;
        }

        var sections = DestinationBatchPlanner.BuildSections(entries);
        await new DestinationBatchWindow(sections).ShowDialog<bool>(owner);
    }
}

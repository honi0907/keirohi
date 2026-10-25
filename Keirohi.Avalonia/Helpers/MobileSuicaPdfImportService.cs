using Avalonia.Controls;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Keirohi.Avalonia.Windows;

namespace Keirohi.Avalonia.Helpers;

public static class MobileSuicaPdfImportService
{
    public sealed class ImportResult
    {
        public bool Success { get; init; }

        public IReadOnlyList<string> ImportedEntryIds { get; init; } = [];
    }

    public static async Task<ImportResult> TryImportAsync(
        IReadOnlyList<TripEntry> entries,
        int skippedLines,
        Window owner)
    {
        if (entries.Count == 0)
        {
            return new ImportResult();
        }

        var room = MobileSuicaPdfImportPlanner.GetRoomInCurrentSet();
        if (MobileSuicaPdfImportPlanner.FitsInCurrentSetOnly(entries.Count, room))
        {
            var confirm = new MobileSuicaPdfImportConfirmWindow(entries, skippedLines);
            if (!await confirm.ShowDialog<bool>(owner))
            {
                return new ImportResult();
            }

            var selected = confirm.SelectedEntries;
            AppHost.State.ImportEntries(selected);
            return new ImportResult
            {
                Success = true,
                ImportedEntryIds = selected.Select(e => e.Id).ToList(),
            };
        }

        if (!MobileSuicaPdfImportPlanner.FitsInTwoSets(entries.Count, room))
        {
            await DialogService.ShowOkAsync(
                "PDF 取込",
                $"取込 {entries.Count} 件は、現在の空き {room} 件と新しいセット1つ（最大 {AppStateService.MaxSuicaEntries} 件）では収まりません。\n2回以上に分けるか、セットを空けてから再度お試しください。",
                owner);
            return new ImportResult();
        }

        var plan = MobileSuicaPdfImportPlanner.BuildSplitPlan(entries.Count, room);
        var dialog = new MobileSuicaPdfSplitWindow(entries, plan, skippedLines);
        var accepted = await dialog.ShowDialog<bool>(owner);
        if (!accepted || dialog.FirstBatchCount is not { } firstCount)
        {
            return new ImportResult();
        }

        var toImport = dialog.SelectedEntries;
        var importedIds = ApplySplitImport(toImport, firstCount, plan.UsesCurrentSet);
        return new ImportResult
        {
            Success = true,
            ImportedEntryIds = importedIds,
        };
    }

    private static List<string> ApplySplitImport(
        IReadOnlyList<TripEntry> entries,
        int firstCount,
        bool usesCurrentSet)
    {
        var importedIds = new List<string>();
        var first = entries.Take(firstCount).ToList();
        var second = entries.Skip(firstCount).ToList();

        if (usesCurrentSet)
        {
            if (first.Count > 0)
            {
                AppHost.State.ImportEntries(first);
                importedIds.AddRange(first.Select(e => e.Id));
            }

            if (second.Count > 0)
            {
                AppHost.State.AddEntrySet();
                AppHost.State.ImportEntries(second);
                importedIds.AddRange(second.Select(e => e.Id));
            }

            return importedIds;
        }

        if (first.Count > 0)
        {
            AppHost.State.AddEntrySet();
            AppHost.State.ImportEntries(first);
            importedIds.AddRange(first.Select(e => e.Id));
        }

        if (second.Count > 0)
        {
            AppHost.State.AddEntrySet();
            AppHost.State.ImportEntries(second);
            importedIds.AddRange(second.Select(e => e.Id));
        }

        return importedIds;
    }
}

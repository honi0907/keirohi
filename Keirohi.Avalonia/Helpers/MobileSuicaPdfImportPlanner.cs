using Keirohi_App.Models;
using Keirohi_App.Services;

namespace Keirohi.Avalonia.Helpers;

public sealed record MobileSuicaPdfSplitPlan(
    int MinFirstBatchCount,
    int MaxFirstBatchCount,
    int DefaultFirstBatchCount,
    bool UsesCurrentSet,
    int RoomInCurrentSet);

public static class MobileSuicaPdfImportPlanner
{
    public static int GetRoomInCurrentSet() =>
        Math.Max(0, AppStateService.MaxSuicaEntries - AppHost.State.Entries.Count);

    public static bool FitsInCurrentSetOnly(int importCount, int room) => importCount <= room;

    public static bool FitsInTwoSets(int importCount, int room) =>
        importCount <= room + AppStateService.MaxSuicaEntries;

    public static MobileSuicaPdfSplitPlan BuildSplitPlan(int importCount, int room)
    {
        if (room <= 0)
        {
            var min = Math.Max(1, importCount - AppStateService.MaxSuicaEntries);
            var max = Math.Min(importCount - 1, AppStateService.MaxSuicaEntries);
            var defaultCount = Math.Clamp(importCount / 2, min, max);
            return new MobileSuicaPdfSplitPlan(min, max, defaultCount, UsesCurrentSet: false, RoomInCurrentSet: 0);
        }

        var minFirst = Math.Max(0, importCount - AppStateService.MaxSuicaEntries);
        var maxFirst = Math.Min(room, importCount);
        var defaultFirst = Math.Clamp(Math.Min(room, importCount), minFirst, maxFirst);
        return new MobileSuicaPdfSplitPlan(minFirst, maxFirst, defaultFirst, UsesCurrentSet: true, RoomInCurrentSet: room);
    }

    public static string FormatPreviewLine(TripEntry entry) =>
        FormatPreviewLine(entry, 0);

    public static string FormatPreviewLine(TripEntry entry, int index)
    {
        var mode = entry.Mode == TransportMode.Bus ? "バス" : "電車";
        var route = entry.Mode == TransportMode.Bus
            ? entry.Departure
            : $"{entry.Departure} → {entry.Arrival}";
        return $"{index + 1,3}. {entry.Date}  {mode}  {route}  ¥{entry.Amount:N0}";
    }

    public static string FormatImportSelectionSummary(int checkedCount, int totalCount)
    {
        var uncheckedCount = totalCount - checkedCount;
        if (uncheckedCount == 0)
        {
            return $"取り込み予定: {checkedCount} 件";
        }

        return $"取り込み予定: {checkedCount} 件（チェックを外し {uncheckedCount} 件）";
    }
}

using Keirohi_App.Services;

namespace Keirohi.Avalonia.Helpers;

public static class SuicaEntryLimitHelper
{
    public static int Remaining =>
        Math.Max(0, AppStateService.MaxSuicaEntries - AppHost.State.Entries.Count);

    public static bool IsAtLimit =>
        AppHost.State.Entries.Count >= AppStateService.MaxSuicaEntries;

    public static string LimitMessage =>
        $"Suica明細は{AppStateService.MaxSuicaEntries}件までです。出力後に新しいセットで続けてください。";

    public static async Task<bool> BlockIfAtLimitWithExportOfferAsync()
    {
        if (!IsAtLimit)
        {
            return false;
        }

        await ShowLimitDialogAsync(LimitMessage);
        return true;
    }

    public static async Task OfferExportAfterFillAsync(string reason)
    {
        var goExport = await DialogService.ShowConfirmAsync(
            $"{AppStateService.MaxSuicaEntries}件に達しました",
            reason,
            "出力画面へ",
            "このまま");
        if (goExport)
        {
            AppHost.MainWindowInstance?.NavigateToNavTag("export");
        }
    }

    private static async Task ShowLimitDialogAsync(string message)
    {
        var goExport = await DialogService.ShowConfirmAsync(
            "上限",
            message,
            "出力画面へ",
            "閉じる");
        if (goExport)
        {
            AppHost.MainWindowInstance?.NavigateToNavTag("export");
        }
    }
}

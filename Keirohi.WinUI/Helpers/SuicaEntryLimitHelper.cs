using Keirohi_App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Keirohi_App.Helpers;

public static class SuicaEntryLimitHelper
{
    public static int Remaining =>
        Math.Max(0, AppStateService.MaxSuicaEntries - App.State.Entries.Count);

    public static bool IsAtLimit =>
        App.State.Entries.Count >= AppStateService.MaxSuicaEntries;

    public static string LimitMessage =>
        $"Suica明細は{AppStateService.MaxSuicaEntries}件までです。出力後に新しいセットで続けてください。";

    /// <summary>上限時にダイアログを出し、了承なら出力画面へ。上限なら true（呼び出し側は登録を止める）。</summary>
    public static async Task<bool> BlockIfAtLimitWithExportOfferAsync(XamlRoot xamlRoot)
    {
        if (!IsAtLimit)
        {
            return false;
        }

        await ShowLimitDialogAsync(xamlRoot, LimitMessage);
        return true;
    }

    /// <summary>54件到達または一部省略したあと、出力画面への誘導を出す。</summary>
    public static async Task OfferExportAfterFillAsync(XamlRoot xamlRoot, string reason)
    {
        var dialog = new ContentDialog
        {
            Title = $"{AppStateService.MaxSuicaEntries}件に達しました",
            Content = reason,
            PrimaryButtonText = "出力画面へ",
            CloseButtonText = "このまま",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            App.MainWindowInstance?.NavigateToNavTag("export");
        }
    }

    private static async Task ShowLimitDialogAsync(XamlRoot xamlRoot, string message)
    {
        var dialog = new ContentDialog
        {
            Title = "上限",
            Content = message,
            PrimaryButtonText = "出力画面へ",
            CloseButtonText = "閉じる",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            App.MainWindowInstance?.NavigateToNavTag("export");
        }
    }
}

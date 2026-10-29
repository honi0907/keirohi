using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Keirohi_App.Services;

namespace Keirohi.Avalonia.Helpers;

public static class ManualDataShareHelper
{
    private static string DefaultExportFileName()
    {
        var d = DateTime.Today;
        return $"keirohi-{d:yyyyMMdd}.json";
    }

    public static async Task<(bool Success, string Message)> TryExportAsync(Window? owner)
    {
        owner ??= AppHost.MainWindowInstance;
        if (owner?.StorageProvider is not { } storage)
        {
            return (false, "ファイル保存ダイアログを開けませんでした。");
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "JSON をダウンロード",
            SuggestedFileName = DefaultExportFileName(),
            FileTypeChoices =
            [
                new FilePickerFileType("JSON")
                {
                    Patterns = ["*.json", "*.txt"],
                },
            ],
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return (false, "保存をキャンセルしました。");
        }

        try
        {
            var json = AppHost.State.BuildManualShareJson();
            await File.WriteAllTextAsync(path, json);
            return (true, $"保存しました:\n{path}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static async Task<(bool Success, string Message)> TryImportAsync(Window? owner)
    {
        owner ??= AppHost.MainWindowInstance;
        if (owner?.StorageProvider is not { } storage)
        {
            return (false, "ファイル選択ダイアログを開けませんでした。");
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "JSON を読み込む",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("JSON / テキスト")
                {
                    Patterns = ["*.json", "*.txt"],
                },
            ],
        });

        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (string.IsNullOrWhiteSpace(path))
        {
            return (false, "取り込みをキャンセルしました。");
        }

        var ok = await DialogService.ShowConfirmAsync(
            "JSON を取り込む",
            "選択した JSON の内容で、明細・パターン・運賃履歴を上書きします。Suica 設定などはそのままです。よろしいですか？",
            "取り込む",
            "キャンセル",
            owner);
        if (!ok)
        {
            return (false, "取り込みをキャンセルしました。");
        }

        try
        {
            LocalDataSyncBackupService.CreatePreSyncBackup();
            var json = await File.ReadAllTextAsync(path);
            AppHost.State.ImportManualShareJson(json);
            return (true, "JSON を取り込みました。");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}

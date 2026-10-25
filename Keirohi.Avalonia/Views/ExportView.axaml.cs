using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Keirohi.Avalonia.Helpers;

namespace Keirohi.Avalonia.Views;

public partial class ExportView : UserControl
{
    public ExportView()
    {
        InitializeComponent();
        AppHost.State.Changed += (_, _) => RefreshSummary();
    }

    public void OnNavigatedTo() => RefreshSummary();

    private void RefreshSummary()
    {
        var export = DateFormatHelper.EntriesForSuicaSheet(AppHost.State.Entries);
        var setName = AppHost.State.ActiveEntrySet?.Name;
        var setLabel = string.IsNullOrWhiteSpace(setName) ? "" : $"セット「{setName}」— ";
        PeriodText.Text = export.Count == 0
            ? $"{setLabel}出力する区間がありません。「入力」で登録するか、右側で別セットを選んでください。"
            : $"{setLabel}出力対象 {export.Count} 件 — 期間ラベル: {DateFormatHelper.FormatPeriodLabel(export)}";
    }

    private List<TripEntry> GetExportEntries() =>
        DateFormatHelper.EntriesForSuicaSheet(AppHost.State.Entries);

    private string GetPeriodLabel() => DateFormatHelper.FormatPeriodLabel(GetExportEntries());

    private async void ExportCsv_Click(object? sender, RoutedEventArgs e) =>
        await SaveCsvAsync(await PickSavePathAsync($"バス・電車精算_申請データ_{GetPeriodLabel()}.csv", ["csv"]));

    private async void ExportExcel_Click(object? sender, RoutedEventArgs e) =>
        await SaveExcelAsync(await PickSavePathAsync($"モバイルSuica明細_{GetPeriodLabel()}.xlsx", ["xlsx"]));

    private async void ExportBoth_Click(object? sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (folder is null)
        {
            return;
        }

        var period = GetPeriodLabel();
        var csvPath = Path.Combine(folder, $"バス・電車精算_申請データ_{period}.csv");
        var xlsxPath = Path.Combine(folder, $"モバイルSuica明細_{period}.xlsx");
        await SaveCsvAsync(csvPath);
        await SaveExcelAsync(xlsxPath);
        await DialogService.ShowOkAsync("保存しました", folder);
    }

    private async void ImportPdf_Click(object? sender, RoutedEventArgs e)
    {
        var path = await PickOpenPathAsync(["pdf"], "モバイル Suica PDF を選択");
        if (path is null)
        {
            return;
        }

        MobileSuicaPdfParseResult parsed;
        try
        {
            parsed = MobileSuicaPdfImporter.ParseFile(path);
        }
        catch (Exception ex)
        {
            await DialogService.ShowOkAsync("PDF 取込", $"PDF を読み込めませんでした。\n\n{ex.Message}");
            return;
        }

        if (parsed.Entries.Count == 0)
        {
            await DialogService.ShowOkAsync(
                "PDF 取込",
                $"鉄道（入/出）またはバス等として読み取れる行がありませんでした（スキップ {parsed.SkippedLines} 行）。");
            return;
        }

        var owner = TopLevel.GetTopLevel(this) as Window ?? AppHost.MainWindowInstance;
        if (owner is null)
        {
            return;
        }

        var importResult = await MobileSuicaPdfImportService.TryImportAsync(parsed.Entries, parsed.SkippedLines, owner);
        if (importResult.Success)
        {
            StatusText.Text = $"PDF から {parsed.Entries.Count} 件を取り込みました（スキップ {parsed.SkippedLines} 行）。";
            RefreshSummary();
            await DestinationBatchUiHelper.ShowForImportedEntryIdsAsync(owner, importResult.ImportedEntryIds);
        }
    }

    private async void ImportCsv_Click(object? sender, RoutedEventArgs e)
    {
        var path = await PickOpenPathAsync(["csv"]);
        if (path is null)
        {
            return;
        }

        var text = await File.ReadAllTextAsync(path);
        var imported = RakurakuCsvImporter.Parse(text);
        if (imported.Count == 0)
        {
            await DialogService.ShowOkAsync("取込", "読み込める行がありませんでした。");
            return;
        }

        var room = AppStateService.MaxSuicaEntries - AppHost.State.Entries.Count;
        if (room <= 0)
        {
            await DialogService.ShowOkAsync("取込", $"すでに{AppStateService.MaxSuicaEntries}件です。");
            return;
        }

        var accepted = imported.Take(room).ToList();
        AppHost.State.ImportEntries(accepted);

        var msg = imported.Count > accepted.Count
            ? $"{accepted.Count}件を取り込みました。{imported.Count - accepted.Count}件は上限のため省略しました。"
            : $"{accepted.Count}件を取り込みました。";
        await DialogService.ShowOkAsync("取込", msg);
    }

    private async Task SaveCsvAsync(string? path)
    {
        if (path is null)
        {
            return;
        }

        if (GetExportEntries().Count == 0)
        {
            await DialogService.ShowOkAsync("出力", "出力する区間がありません。");
            return;
        }

        await RakurakuCsvExporter.SaveAsync(GetExportEntries(), path);
        await DialogService.ShowOkAsync("保存しました", path);
    }

    private async Task SaveExcelAsync(string? path)
    {
        if (path is null)
        {
            return;
        }

        if (GetExportEntries().Count == 0)
        {
            await DialogService.ShowOkAsync("出力", "出力する区間がありません。");
            return;
        }

        await SuicaExcelExporter.SaveAsync(GetExportEntries(), AppHost.State.Settings, path);
        await DialogService.ShowOkAsync("保存しました", path);
    }

    private async Task<string?> PickSavePathAsync(string suggestedName, IReadOnlyList<string> extensions)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider is not { } storage)
        {
            return null;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存先",
            SuggestedFileName = suggestedName,
            FileTypeChoices =
            [
                new FilePickerFileType("ファイル")
                {
                    Patterns = extensions.Select(e => $"*.{e}").ToList(),
                },
            ],
        });

        return file?.TryGetLocalPath();
    }

    private async Task<string?> PickOpenPathAsync(IReadOnlyList<string> extensions, string title = "CSV を選択")
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider is not { } storage)
        {
            return null;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("CSV") { Patterns = extensions.Select(e => $"*.{e}").ToList() },
            ],
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async Task<string?> PickFolderAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider is not { CanPickFolder: true } storage)
        {
            return null;
        }

        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "保存先フォルダ",
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}

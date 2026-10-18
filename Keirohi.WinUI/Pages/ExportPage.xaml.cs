using Keirohi_App.Helpers;
using Keirohi_App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Keirohi_App.Pages;

public sealed partial class ExportPage : Page
{
    public ExportPage()
    {
        InitializeComponent();
        App.State.Changed += (_, _) => RefreshSummary();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        var export = DateFormatHelper.EntriesForSuicaSheet(App.State.Entries);
        var setName = App.State.ActiveEntrySet?.Name;
        var setLabel = string.IsNullOrWhiteSpace(setName) ? "" : $"セット「{setName}」— ";
        PeriodText.Text = export.Count == 0
            ? $"{setLabel}出力する区間がありません。「入力」で登録するか、右側で別セットを選んでください。"
            : $"{setLabel}出力対象 {export.Count} 件 — 期間ラベル: {DateFormatHelper.FormatPeriodLabel(export)}";
    }

    private List<Models.TripEntry> GetExportEntries()
    {
        return DateFormatHelper.EntriesForSuicaSheet(App.State.Entries);
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e) =>
        await SaveCsvAsync(await PickSaveFileAsync($"バス・電車精算_申請データ_{GetPeriodLabel()}.csv", ".csv"));

    private async void ExportExcel_Click(object sender, RoutedEventArgs e) =>
        await SaveExcelAsync(await PickSaveFileAsync($"モバイルSuica明細_{GetPeriodLabel()}.xlsx", ".xlsx"));

    private async void ExportBoth_Click(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (folder is null)
        {
            return;
        }

        var period = GetPeriodLabel();
        await SaveCsvAsync(await folder.CreateFileAsync($"バス・電車精算_申請データ_{period}.csv", CreationCollisionOption.ReplaceExisting));
        await SaveExcelAsync(await folder.CreateFileAsync($"モバイルSuica明細_{period}.xlsx", CreationCollisionOption.ReplaceExisting));
        await ShowOkAsync("保存しました", folder.Path);
    }

    private async void ImportCsv_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        PickerHelper.BindToWindow(picker, App.MainWindowInstance!);
        picker.FileTypeFilter.Add(".csv");
        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        var text = await File.ReadAllTextAsync(file.Path);
        var imported = RakurakuCsvImporter.Parse(text);
        if (imported.Count == 0)
        {
            await ShowOkAsync("取込", "読み込める行がありませんでした。");
            return;
        }

        var room = AppStateService.MaxSuicaEntries - App.State.Entries.Count;
        if (room <= 0)
        {
            await ShowOkAsync("取込", $"すでに{AppStateService.MaxSuicaEntries}件です。");
            return;
        }

        var accepted = imported.Take(room).ToList();
        App.State.ImportEntries(accepted);

        var msg = imported.Count > accepted.Count
            ? $"{accepted.Count}件を取り込みました。{imported.Count - accepted.Count}件は上限のため省略しました。"
            : $"{accepted.Count}件を取り込みました。";
        await ShowOkAsync("取込", msg);
    }

    private string GetPeriodLabel() => DateFormatHelper.FormatPeriodLabel(GetExportEntries());

    private async Task SaveCsvAsync(StorageFile? file)
    {
        if (file is null)
        {
            return;
        }

        if (GetExportEntries().Count == 0)
        {
            await ShowOkAsync("出力", "出力する区間がありません。");
            return;
        }

        await RakurakuCsvExporter.SaveAsync(GetExportEntries(), file.Path);
        await ShowOkAsync("保存しました", file.Path);
    }

    private async Task SaveExcelAsync(StorageFile? file)
    {
        if (file is null)
        {
            return;
        }

        if (GetExportEntries().Count == 0)
        {
            await ShowOkAsync("出力", "出力する区間がありません。");
            return;
        }

        await SuicaExcelExporter.SaveAsync(GetExportEntries(), App.State.Settings, file.Path);
        await ShowOkAsync("保存しました", file.Path);
    }

    private async Task<StorageFile?> PickSaveFileAsync(string suggestedName, string extension)
    {
        var picker = new FileSavePicker();
        PickerHelper.BindToWindow(picker, App.MainWindowInstance!);
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedName);
        picker.FileTypeChoices.Add("ファイル", [extension]);
        return await picker.PickSaveFileAsync();
    }

    private async Task<StorageFolder?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        PickerHelper.BindToWindow(picker, App.MainWindowInstance!);
        picker.FileTypeFilter.Add("*");
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        return await picker.PickSingleFolderAsync();
    }

    private async Task ShowOkAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot,
        };
        await dialog.ShowAsync();
    }
}

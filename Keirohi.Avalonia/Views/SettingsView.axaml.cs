using Avalonia.Controls;
using Avalonia.Interactivity;
using Keirohi_App.Models;
using Keirohi_App.Updates;
using Keirohi.Avalonia.Helpers;
using Keirohi.Avalonia.Services;

namespace Keirohi.Avalonia.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public void OnNavigatedTo()
    {
        var s = AppHost.State.Settings;
        CardLabelBox.Text = s.SuicaCardLabel;
        IsoDateHelper.SetFromIso(StatementDatePicker, s.StatementDate);
        IsoDateHelper.EnsureTodayIfEmpty(StatementDatePicker);
        AutoFillFareToggle.IsChecked = s.AutoFillFareEnabled;
        ShowSuicaPreviewPngAndPrintToggle.IsChecked = s.ShowSuicaPreviewPngAndPrint;
        VersionText.Text = $"現在のバージョン: {AppVersionReader.GetCurrentVersion()}";
    }

    private async void CheckUpdate_Click(object? sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        UpdateStatusText.Text = "更新を確認しています...";
        try
        {
            await OnlineUpdateUiHelper.RunFromSettingsAsync(message => UpdateStatusText.Text = message);
        }
        finally
        {
            CheckUpdateButton.IsEnabled = true;
        }
    }

    private void StatementDateToday_Click(object? sender, RoutedEventArgs e) =>
        StatementDatePicker.SelectedDate = DateTime.Today;

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var current = AppHost.State.Settings;
        AppHost.State.UpdateSettings(new AppSettings
        {
            SuicaCardLabel = CardLabelBox.Text?.Trim() ?? "",
            StatementDate = IsoDateHelper.ToIsoString(StatementDatePicker.SelectedDate) is { Length: > 0 } iso
                ? iso
                : DateTime.Today.ToString("yyyy-MM-dd"),
            EkispertApiKey = current.EkispertApiKey,
            AutoFillFareEnabled = AutoFillFareToggle.IsChecked == true,
            ShowSuicaPreviewPngAndPrint = ShowSuicaPreviewPngAndPrintToggle.IsChecked == true,
            PrintImageScale = current.PrintImageScale,
            PrintOffsetXmm = current.PrintOffsetXmm,
            PrintOffsetYmm = current.PrintOffsetYmm,
            PrintMarginLeft = current.PrintMarginLeft,
            PrintMarginRight = current.PrintMarginRight,
            PrintMarginTop = current.PrintMarginTop,
            PrintMarginBottom = current.PrintMarginBottom,
            PrintLayoutDefaultsV2Applied = current.PrintLayoutDefaultsV2Applied,
        });
        UpdateStatusText.Text = "設定を保存しました。";
    }
}

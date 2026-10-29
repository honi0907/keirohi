using Avalonia.Controls;
using Avalonia.Interactivity;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Keirohi_App.Updates;
using Keirohi.Avalonia.Helpers;
using Keirohi.Avalonia.Services;
using Keirohi.Avalonia.Windows;

namespace Keirohi.Avalonia.Views;

public partial class SettingsView : UserControl
{
    private static readonly string[] ThemePreferences = ["System", "Light", "Dark"];
    private bool suppressThemeChange;

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
        suppressThemeChange = true;
        SelectThemePreference(s.ThemePreference);
        suppressThemeChange = false;
        AutoFillFareToggle.IsChecked = s.AutoFillFareEnabled;
        ShowSuicaPreviewPngAndPrintToggle.IsChecked = s.ShowSuicaPreviewPngAndPrint;
        VersionText.Text = $"現在のバージョン: {AppVersionReader.GetCurrentVersion()}";
    }

    private async void ManualShareExport_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? AppHost.MainWindowInstance;
        var (success, message) = await ManualDataShareHelper.TryExportAsync(owner);
        UpdateStatusText.Text = message;
        if (success)
        {
            await DialogService.ShowOkAsync("JSON をダウンロード", message, owner);
        }
    }

    private async void ManualShareImport_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? AppHost.MainWindowInstance;
        var (success, message) = await ManualDataShareHelper.TryImportAsync(owner);
        UpdateStatusText.Text = message;
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

    private async void FareHistory_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? AppHost.MainWindowInstance;
        if (owner is null)
        {
            return;
        }

        await new FareMemoryWindow().ShowDialog(owner);
    }

    private void ThemeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (suppressThemeChange || ThemeCombo.SelectedIndex < 0)
        {
            return;
        }

        PersistSettings();
        AppThemeHelper.Apply(AppHost.State.Settings);
        UpdateStatusText.Text = $"外観を{ThemeLabel(AppHost.State.Settings.ThemePreference)}にしました。";
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        PersistSettings();
        AppThemeHelper.Apply(AppHost.State.Settings);
        UpdateStatusText.Text = "設定を保存しました。";
    }

    private void PersistSettings()
    {
        var current = AppHost.State.Settings;
        AppHost.State.UpdateSettings(new AppSettings
        {
            SuicaCardLabel = CardLabelBox.Text?.Trim() ?? "",
            StatementDate = IsoDateHelper.ToIsoString(StatementDatePicker.SelectedDate) is { Length: > 0 } iso
                ? iso
                : DateTime.Today.ToString("yyyy-MM-dd"),
            EkispertApiKey = current.EkispertApiKey,
            ThemePreference = ReadThemePreference(),
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
            ReleaseNotesAcknowledgedVersion = current.ReleaseNotesAcknowledgedVersion,
        });
    }

    private async void ReleaseNotesHistory_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? AppHost.MainWindowInstance;
        if (owner is null)
        {
            return;
        }

        await new ReleaseNotesHistoryWindow().ShowDialog(owner);
    }

    private static string ThemeLabel(string preference) =>
        preference switch
        {
            "Light" => "ライト",
            "Dark" => "ダーク",
            _ => "システムに合わせる",
        };

    private void SelectThemePreference(string preference)
    {
        var index = Array.IndexOf(ThemePreferences, preference);
        ThemeCombo.SelectedIndex = index >= 0 ? index : 0;
    }

    private string ReadThemePreference()
    {
        var index = ThemeCombo.SelectedIndex;
        return index >= 0 && index < ThemePreferences.Length
            ? ThemePreferences[index]
            : ThemePreferences[0];
    }
}

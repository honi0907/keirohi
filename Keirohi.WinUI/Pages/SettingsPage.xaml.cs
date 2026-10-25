using Keirohi_App.Helpers;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Keirohi_App.Updates;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Keirohi_App.Pages;

public sealed partial class SettingsPage : Page
{
    private static readonly string[] ThemePreferences = ["System", "Light", "Dark"];
    private bool suppressThemeChange;

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var s = App.State.Settings;
        CardLabelBox.Text = s.SuicaCardLabel;
        IsoDateHelper.SetFromIso(StatementDatePicker, s.StatementDate);
        IsoDateHelper.EnsureTodayIfEmpty(StatementDatePicker);
        suppressThemeChange = true;
        SelectThemePreference(s.ThemePreference);
        suppressThemeChange = false;
        AutoFillFareToggle.IsOn = s.AutoFillFareEnabled;
        ShowSuicaPreviewPngAndPrintToggle.IsOn = s.ShowSuicaPreviewPngAndPrint;
        VersionText.Text = $"現在のバージョン: {AppVersionReader.GetCurrentVersion()}";
        SettingsStatusText.Text = "";
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        UpdateStatusText.Text = "更新を確認しています...";
        try
        {
            await OnlineUpdateUiHelper.RunFromSettingsAsync(XamlRoot, message => UpdateStatusText.Text = message);
        }
        finally
        {
            CheckUpdateButton.IsEnabled = true;
        }
    }

    private void StatementDateToday_Click(object sender, RoutedEventArgs e) =>
        StatementDatePicker.Date = DateTimeOffset.Now.Date;

    private void FareHistory_Click(object sender, RoutedEventArgs e)
    {
        var window = new FareMemoryWindow();
        window.Activate();
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressThemeChange || ThemeCombo.SelectedIndex < 0)
        {
            return;
        }

        PersistSettings();
        AppThemeHelper.Apply(App.State.Settings);
        SettingsStatusText.Text = $"外観を{ThemeLabel(App.State.Settings.ThemePreference)}にしました。";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        PersistSettings();
        AppThemeHelper.Apply(App.State.Settings);
        SettingsStatusText.Text = "設定を保存しました。";
    }

    private void PersistSettings()
    {
        var current = App.State.Settings;
        App.State.UpdateSettings(new AppSettings
        {
            SuicaCardLabel = CardLabelBox.Text.Trim(),
            StatementDate = IsoDateHelper.ToIsoString(StatementDatePicker.Date) is { Length: > 0 } iso
                ? iso
                : DateTime.Today.ToString("yyyy-MM-dd"),
            EkispertApiKey = current.EkispertApiKey,
            ThemePreference = ReadThemePreference(),
            AutoFillFareEnabled = AutoFillFareToggle.IsOn,
            ShowSuicaPreviewPngAndPrint = ShowSuicaPreviewPngAndPrintToggle.IsOn,
            PrintImageScale = current.PrintImageScale,
            PrintOffsetXmm = current.PrintOffsetXmm,
            PrintOffsetYmm = current.PrintOffsetYmm,
            PrintMarginLeft = current.PrintMarginLeft,
            PrintMarginRight = current.PrintMarginRight,
            PrintMarginTop = current.PrintMarginTop,
            PrintMarginBottom = current.PrintMarginBottom,
            PrintLayoutDefaultsV2Applied = current.PrintLayoutDefaultsV2Applied,
        });
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

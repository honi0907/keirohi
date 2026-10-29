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
        RefreshDriveSyncUi();
    }

    private void RefreshDriveSyncUi()
    {
        var prefs = DriveSyncPreferencesStore.Load();
        if (!CloudSyncSettingsHelper.IsDriveClientConfigured)
        {
            DriveSyncStatusText.Text =
                $"OAuth 未設定 — {CloudSyncSettingsHelper.ClientSecretsHintPath} にクライアント JSON を置いてください。";
        }
        else
        {
            DriveSyncStatusText.Text = prefs.Connected
                ? $"接続済み · 最終同期 {CloudSyncSettingsHelper.FormatSyncTime(prefs.LastSyncedAt)}"
                : "未接続 — Google でログインしてください。";
        }
        DriveFolderBox.Text = prefs.Connected && !string.IsNullOrWhiteSpace(prefs.FolderName)
            ? prefs.FolderName
            : "";
        DriveConnectButton.Content = prefs.Connected ? "アカウント確認" : "Google でログイン";
        DrivePickFolderButton.IsEnabled = prefs.Connected;
        DriveSyncNowButton.IsEnabled =
            CloudSyncSettingsHelper.IsDriveClientConfigured
            && prefs.Connected
            && !string.IsNullOrWhiteSpace(prefs.FolderName);
        DriveDisconnectButton.IsEnabled = prefs.Connected;

        var manifest = LocalDataSyncBackupService.GetPreSyncBackupManifest();
        LocalBackupStatusText.Text = manifest is null
            ? "同期前バックアップ: なし（「今すぐ同期」で自動作成）"
            : $"同期前バックアップ: {CloudSyncSettingsHelper.FormatBackupTime(manifest.CreatedAt)}";
        RestoreLocalButton.IsEnabled = LocalDataSyncBackupService.HasPreSyncBackup;
    }

    private async void DriveConnect_Click(object? sender, RoutedEventArgs e)
    {
        DriveConnectButton.IsEnabled = false;
        try
        {
            if (!CloudSyncSettingsHelper.IsDriveClientConfigured)
            {
                UpdateStatusText.Text = $"OAuth JSON を配置してください: {CloudSyncSettingsHelper.ClientSecretsHintPath}";
                return;
            }

            var (ok, message) = await CloudSyncSettingsHelper.TrySignInAsync();
            UpdateStatusText.Text = message;
            if (!ok)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(DriveSyncPreferencesStore.Load().FolderName))
            {
                var prefs = DriveSyncPreferencesStore.Load();
                prefs.FolderName = "通勤経路費";
                DriveSyncPreferencesStore.Save(prefs);
            }
        }
        finally
        {
            DriveConnectButton.IsEnabled = true;
            RefreshDriveSyncUi();
        }
    }

    private async void DriveDisconnect_Click(object? sender, RoutedEventArgs e)
    {
        await CloudSyncSettingsHelper.SignOutAsync();
        UpdateStatusText.Text = "Google Drive 接続を解除しました。";
        RefreshDriveSyncUi();
    }

    private async void DrivePickFolder_Click(object? sender, RoutedEventArgs e)
    {
        var prefs = DriveSyncPreferencesStore.Load();
        if (!prefs.Connected)
        {
            return;
        }

        var owner = TopLevel.GetTopLevel(this) as Window ?? AppHost.MainWindowInstance;
        var name = await DialogService.ShowPromptAsync(
            "Drive 同期フォルダ",
            prefs.FolderName.Length > 0 ? prefs.FolderName : "通勤経路費",
            "Drive 上のフォルダ名",
            owner);
        if (name is null)
        {
            return;
        }

        prefs.FolderName = name.Trim().Length > 0 ? name.Trim() : "通勤経路費";
        prefs.FolderId = "";
        DriveSyncPreferencesStore.Save(prefs);
        RefreshDriveSyncUi();
        UpdateStatusText.Text = "同期フォルダを設定しました。";
    }

    private async void DriveSyncNow_Click(object? sender, RoutedEventArgs e)
    {
        DriveSyncNowButton.IsEnabled = false;
        UpdateStatusText.Text = "同期しています…";
        try
        {
            UpdateStatusText.Text = await CloudSyncSettingsHelper.RunSyncNowAsync();
            RefreshDriveSyncUi();
        }
        finally
        {
            RefreshDriveSyncUi();
        }
    }

    private async void ManualShareExport_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? AppHost.MainWindowInstance;
        var (success, message) = await ManualDataShareHelper.TryExportAsync(owner);
        UpdateStatusText.Text = message;
        if (success)
        {
            await DialogService.ShowOkAsync("JSON を書き出しました", message, owner);
        }
    }

    private async void ManualShareImport_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? AppHost.MainWindowInstance;
        var (success, message) = await ManualDataShareHelper.TryImportAsync(owner);
        UpdateStatusText.Text = message;
        if (success)
        {
            RefreshDriveSyncUi();
        }
    }

    private async void RestoreLocal_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? AppHost.MainWindowInstance;
        var ok = await DialogService.ShowConfirmAsync(
            "ローカルデータに戻す",
            "同期前に退避したローカルデータに戻します。現在のデータは上書きされます。よろしいですか？",
            "戻す",
            "キャンセル",
            owner);
        if (!ok)
        {
            return;
        }

        var (success, message) = await CloudSyncSettingsHelper.TryRestoreLocalAsync();
        UpdateStatusText.Text = message;
        RefreshDriveSyncUi();
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

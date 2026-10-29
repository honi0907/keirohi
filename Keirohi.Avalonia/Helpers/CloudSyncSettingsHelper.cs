using Keirohi_App.Services;
using Keirohi.Avalonia.Services;

namespace Keirohi.Avalonia.Helpers;

internal static class CloudSyncSettingsHelper
{
    public static bool IsDriveClientConfigured => GoogleDriveSyncService.IsClientConfigured;

    public static string ClientSecretsHintPath => GoogleDriveSyncService.ClientSecretsPath;

    public static string FormatSyncTime(DateTimeOffset? at) =>
        at is null
            ? "未同期"
            : at.Value.LocalDateTime.ToString("yyyy/MM/dd HH:mm");

    public static string FormatBackupTime(DateTimeOffset? at) =>
        at is null
            ? "—"
            : at.Value.LocalDateTime.ToString("yyyy/MM/dd HH:mm:ss");

    public static async Task<string> RunSyncNowAsync()
    {
        if (!GoogleDriveSyncService.IsClientConfigured)
        {
            LocalDataSyncBackupService.CreatePreSyncBackup();
            var prefs = DriveSyncPreferencesStore.Load();
            prefs.LastSyncedAt = DateTimeOffset.Now;
            DriveSyncPreferencesStore.Save(prefs);
            return $"OAuth クライアント JSON 未設定のため Drive には接続できません（ローカル退避のみ実行）。\n{GoogleDriveSyncService.ClientSecretsPath}";
        }

        var (ok, message, reloaded) = await GoogleDriveSyncService.SyncAsync();
        if (reloaded)
        {
            AppHost.State.ReloadAfterExternalRestore();
            AppThemeHelper.Apply(AppHost.State.Settings);
        }

        return ok ? message : message;
    }

    public static Task<(bool Ok, string Message)> TrySignInAsync() =>
        GoogleDriveSyncService.SignInAsync();

    public static async Task SignOutAsync()
    {
        await GoogleDriveSyncService.SignOutAsync();
    }

    public static async Task<(bool Ok, string Message)> TryRestoreLocalAsync()
    {
        if (!LocalDataSyncBackupService.HasPreSyncBackup)
        {
            return (false, "復元できる同期前バックアップがありません。先に「今すぐ同期」を実行してください。");
        }

        await Task.CompletedTask;
        try
        {
            LocalDataSyncBackupService.RestorePreSyncBackup();
            AppHost.State.ReloadAfterExternalRestore();
            AppThemeHelper.Apply(AppHost.State.Settings);
            return (true, "同期前のローカルデータに戻しました。");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}

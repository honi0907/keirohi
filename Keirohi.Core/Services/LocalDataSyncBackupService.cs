using Keirohi_App.Helpers;

namespace Keirohi_App.Services;

public sealed class PreSyncBackupManifest
{
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// クラウド同期の直前にローカル JSON を退避し、同期後に復元するためのサービス。
/// </summary>
public static class LocalDataSyncBackupService
{
    private static IReadOnlyList<string> SyncFileNames => CloudSyncConstants.DataFileNames;

    private static string BackupDirectory =>
        Path.Combine(DataPersistenceStatusHelper.DataDirectory, "backups", "pre-sync");

    private static string ManifestPath => Path.Combine(BackupDirectory, "manifest.json");

    public static bool HasPreSyncBackup => File.Exists(ManifestPath);

    public static PreSyncBackupManifest? GetPreSyncBackupManifest()
    {
        if (!HasPreSyncBackup)
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(ManifestPath);
            return System.Text.Json.JsonSerializer.Deserialize<PreSyncBackupManifest>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        }
        catch
        {
            return null;
        }
    }

    public static void CreatePreSyncBackup()
    {
        var dataDir = DataPersistenceStatusHelper.DataDirectory;
        Directory.CreateDirectory(BackupDirectory);

        foreach (var name in SyncFileNames)
        {
            var source = Path.Combine(dataDir, name);
            var dest = Path.Combine(BackupDirectory, name);
            if (File.Exists(source))
            {
                File.Copy(source, dest, overwrite: true);
            }
            else if (File.Exists(dest))
            {
                File.Delete(dest);
            }
        }

        var manifest = new PreSyncBackupManifest { CreatedAt = DateTimeOffset.Now };
        File.WriteAllText(
            ManifestPath,
            System.Text.Json.JsonSerializer.Serialize(
                manifest,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                }));
    }

    public static void RestorePreSyncBackup()
    {
        if (!HasPreSyncBackup)
        {
            throw new InvalidOperationException("同期前のローカルバックアップがありません。");
        }

        var dataDir = DataPersistenceStatusHelper.DataDirectory;
        Directory.CreateDirectory(dataDir);

        foreach (var name in SyncFileNames)
        {
            var source = Path.Combine(BackupDirectory, name);
            var dest = Path.Combine(dataDir, name);
            if (File.Exists(source))
            {
                File.Copy(source, dest, overwrite: true);
            }
        }
    }
}

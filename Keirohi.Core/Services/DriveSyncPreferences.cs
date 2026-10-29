using Keirohi_App.Helpers;

namespace Keirohi_App.Services;

public sealed class DriveSyncPreferences
{
    public bool Connected { get; set; }

    public string FolderName { get; set; } = "";

    /// <summary>Drive 上のフォルダ ID（API 用）。</summary>
    public string FolderId { get; set; } = "";

    public string? AccountEmail { get; set; }

    public DateTimeOffset? LastSyncedAt { get; set; }
}

public static class DriveSyncPreferencesStore
{
    private static string FilePath =>
        Path.Combine(DataPersistenceStatusHelper.DataDirectory, "drive-sync.json");

    public static DriveSyncPreferences Load()
    {
        if (!File.Exists(FilePath))
        {
            return new DriveSyncPreferences();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            return System.Text.Json.JsonSerializer.Deserialize<DriveSyncPreferences>(
                       json,
                       JsonOptions())
                   ?? new DriveSyncPreferences();
        }
        catch
        {
            return new DriveSyncPreferences();
        }
    }

    public static void Save(DriveSyncPreferences prefs)
    {
        Directory.CreateDirectory(DataPersistenceStatusHelper.DataDirectory);
        File.WriteAllText(FilePath, System.Text.Json.JsonSerializer.Serialize(prefs, JsonOptions()));
    }

    private static System.Text.Json.JsonSerializerOptions JsonOptions() =>
        new()
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
}

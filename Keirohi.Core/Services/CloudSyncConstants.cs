namespace Keirohi_App.Services;

public static class CloudSyncConstants
{
    public const string SyncMetaFileName = "sync-meta.json";

    public static IReadOnlyList<string> DataFileNames { get; } =
    [
        "entry-sets.json",
        "settings.json",
        "patterns.json",
        "fare-memory.json",
    ];
}

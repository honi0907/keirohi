using System.Reflection;
using System.Text.Json;

namespace Keirohi_App.Updates;

public static class ReleaseNotesCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static IReadOnlyList<ReleaseNotesEntry>? cache;

    public static IReadOnlyList<ReleaseNotesEntry> GetAll()
    {
        if (cache is not null)
        {
            return cache;
        }

        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Keirohi_App.Assets.release-notes.json");
        if (stream is null)
        {
            cache = [];
            return cache;
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var doc = JsonSerializer.Deserialize<ReleaseNotesFile>(json, JsonOptions);
        cache = doc?.Versions ?? [];
        return cache;
    }

    public static ReleaseNotesEntry? GetForVersion(string version)
    {
        var normalized = VersionComparer.Normalize(version);
        if (normalized is null)
        {
            return null;
        }

        return GetAll().FirstOrDefault(v =>
            string.Equals(VersionComparer.Normalize(v.Version), normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ShouldPromptOnStartup(string currentVersion, string? acknowledgedVersion)
    {
        var current = VersionComparer.Normalize(currentVersion);
        if (current is null)
        {
            return false;
        }

        if (GetForVersion(current) is null)
        {
            return false;
        }

        var ack = VersionComparer.Normalize(acknowledgedVersion ?? "");
        if (ack is null)
        {
            return true;
        }

        return VersionComparer.Compare(current, ack) > 0;
    }

    private sealed class ReleaseNotesFile
    {
        public List<ReleaseNotesEntry> Versions { get; init; } = [];
    }
}

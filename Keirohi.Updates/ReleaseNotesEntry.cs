namespace Keirohi_App.Updates;

public sealed class ReleaseNotesEntry
{
    public string Version { get; init; } = "";

    public string? Date { get; init; }

    public string? Title { get; init; }

    public List<string> Items { get; init; } = [];
}

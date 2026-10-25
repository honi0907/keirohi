using Keirohi_App.Models;

namespace Keirohi_App.Services;

public sealed class MobileSuicaPdfParseResult
{
    public required IReadOnlyList<TripEntry> Entries { get; init; }

    public int SkippedLines { get; init; }
}

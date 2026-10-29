namespace Keirohi_App.Models;

/// <summary>モバイル Web と共通の手動 JSON 共有形式（formatVersion 1）。</summary>
public sealed class ManualDataShareBundle
{
    public int FormatVersion { get; set; } = 1;

    public DateTimeOffset ExportedAt { get; set; }

    public string App { get; set; } = "";

    public ManualDataShareData? Data { get; set; }
}

public sealed class ManualDataShareData
{
    public EntrySetStore? EntrySets { get; set; }

    public List<RoutePattern>? Patterns { get; set; }

    public List<FareMemoryRow>? FareMemory { get; set; }

    /// <summary>モバイル UI 用。デスクトップからの書き出しでは省略可。</summary>
    public string? SelectedPatternId { get; set; }
}

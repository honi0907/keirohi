namespace Keirohi_App.Models;

public sealed class EntrySet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = "";

    /// <summary>「名前変更」でユーザーが付けた名称を維持する。</summary>
    public bool NameManuallySet { get; set; }

    public List<TripEntry> Entries { get; set; } = [];
}

public sealed class EntrySetStore
{
    public string ActiveSetId { get; set; } = "";

    /// <summary>旧データの誤った手動名称フラグを一度だけ解除済みか。</summary>
    public bool AutoNameLegacyUnlockApplied { get; set; }

    public List<EntrySet> Sets { get; set; } = [];
}

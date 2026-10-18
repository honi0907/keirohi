namespace Keirohi_App.Models;

public sealed class EntrySet
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = "";

    public List<TripEntry> Entries { get; set; } = [];
}

public sealed class EntrySetStore
{
    public string ActiveSetId { get; set; } = "";

    public List<EntrySet> Sets { get; set; } = [];
}

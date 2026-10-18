using System.Text.Json;
using Keirohi_App.Models;

namespace Keirohi_App.Services;

public sealed class FareIndexService
{
    private static readonly JsonSerializerOptions SeedJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly Dictionary<string, int> map = new(StringComparer.Ordinal);

    public static async Task<FareIndexService> BuildAsync(IEnumerable<TripEntry> entries, string? seedJsonPath)
    {
        var index = new FareIndexService();
        await LoadSeedAsync(index, seedJsonPath);
        ApplyEntries(index, entries);
        return index;
    }

    public static FareIndexService BuildFrom(IEnumerable<TripEntry> entries, string? seedJsonPath)
    {
        var index = new FareIndexService();
        LoadSeed(index, seedJsonPath);
        ApplyEntries(index, entries);
        return index;
    }

    public static FareIndexService BuildFromMemory(IEnumerable<FareMemoryRow> rows)
    {
        var index = new FareIndexService();
        foreach (var row in rows)
        {
            index.Set(row.Departure, row.Arrival, row.Amount);
        }

        return index;
    }

    private static void ApplyEntries(FareIndexService index, IEnumerable<TripEntry> entries)
    {
        foreach (var entry in entries)
        {
            index.Set(entry.Departure, entry.Arrival, entry.Amount);
        }
    }

    private static async Task LoadSeedAsync(FareIndexService index, string? seedJsonPath)
    {
        if (string.IsNullOrEmpty(seedJsonPath) || !File.Exists(seedJsonPath))
        {
            return;
        }

        try
        {
            await using var stream = File.OpenRead(seedJsonPath);
            var seed = await JsonSerializer.DeserializeAsync<List<FareSeedRow>>(stream, SeedJsonOptions);
            ApplySeedRows(index, seed);
        }
        catch
        {
            // seed optional
        }
    }

    private static void LoadSeed(FareIndexService index, string? seedJsonPath)
    {
        if (string.IsNullOrEmpty(seedJsonPath) || !File.Exists(seedJsonPath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(seedJsonPath);
            var seed = JsonSerializer.Deserialize<List<FareSeedRow>>(json, SeedJsonOptions);
            ApplySeedRows(index, seed);
        }
        catch
        {
            // seed optional
        }
    }

    private static void ApplySeedRows(FareIndexService index, List<FareSeedRow>? seed)
    {
        if (seed is null)
        {
            return;
        }

        foreach (var row in seed)
        {
            index.Set(row.Departure, row.Arrival, row.Amount);
        }
    }

    public int? Lookup(string departure, string arrival)
    {
        var key = PairKey(departure, arrival);
        return map.TryGetValue(key, out var amount) ? amount : null;
    }

    internal void Set(string departure, string arrival, int amount)
    {
        if (string.IsNullOrWhiteSpace(departure) || string.IsNullOrWhiteSpace(arrival) || amount <= 0)
        {
            return;
        }
        map[PairKey(departure, arrival)] = amount;
    }

    private static string PairKey(string departure, string arrival) =>
        $"{departure.Trim()}\u0001{arrival.Trim()}";

    private sealed class FareSeedRow
    {
        public string Departure { get; set; } = "";
        public string Arrival { get; set; } = "";
        public int Amount { get; set; }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Keirohi_App.Models;

namespace Keirohi_App.Services;

public sealed class AppStateService
{
    public const int MaxSuicaEntries = 54;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string dataDir;
    private readonly string legacyEntriesPath;
    private readonly string entrySetsPath;
    private readonly string settingsPath;
    private readonly string patternsPath;
    private readonly string fareMemoryPath;

    private EntrySetStore entrySetStore = new();
    private List<FareMemoryRow> fareMemoryRows = [];

    public AppStateService()
    {
        dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Keirohi");
        Directory.CreateDirectory(dataDir);
        legacyEntriesPath = Path.Combine(dataDir, "entries.json");
        entrySetsPath = Path.Combine(dataDir, "entry-sets.json");
        settingsPath = Path.Combine(dataDir, "settings.json");
        patternsPath = Path.Combine(dataDir, "patterns.json");
        fareMemoryPath = Path.Combine(dataDir, "fare-memory.json");
        Load();
    }

    /// <summary>現在選択中のセットの明細（従来 API）。</summary>
    public List<TripEntry> Entries => GetActiveSet().Entries;

    public string ActiveEntrySetId => entrySetStore.ActiveSetId;

    public IReadOnlyList<EntrySet> EntrySets => entrySetStore.Sets;

    public EntrySet? ActiveEntrySet =>
        entrySetStore.Sets.FirstOrDefault(s => s.Id == entrySetStore.ActiveSetId);

    public AppSettings Settings { get; private set; } = new();

    public List<RoutePattern> Patterns { get; private set; } = [];

    public DateTimeOffset? DataLastWrittenAt { get; private set; }

    public event EventHandler? Changed;

    public event EventHandler? DataPersisted;

    public void Load()
    {
        LoadEntrySets();
        Settings = LoadJson<AppSettings>(settingsPath) ?? new AppSettings();
        if (ApplyPrintLayoutDefaultsV2(Settings))
        {
            SaveJson(settingsPath, Settings);
        }

        Patterns = LoadJson<List<RoutePattern>>(patternsPath) ?? [];
        LoadFareMemory();
        RefreshDataLastWrittenFromDisk();
    }

    public FareIndexService GetFareIndex() => FareIndexService.BuildFromMemory(fareMemoryRows);

    public void RememberFare(string departure, string arrival, int amount)
    {
        if (!UpsertFareMemoryRow(departure, arrival, amount))
        {
            return;
        }

        SaveJson(fareMemoryPath, fareMemoryRows);
    }

    private bool UpsertFareMemoryRow(string departure, string arrival, int amount)
    {
        if (string.IsNullOrWhiteSpace(departure) || string.IsNullOrWhiteSpace(arrival) || amount <= 0)
        {
            return false;
        }

        var dep = departure.Trim();
        var arr = arrival.Trim();
        var existing = fareMemoryRows.FindIndex(r =>
            string.Equals(r.Departure, dep, StringComparison.Ordinal)
            && string.Equals(r.Arrival, arr, StringComparison.Ordinal));
        if (existing >= 0)
        {
            if (fareMemoryRows[existing].Amount == amount)
            {
                return false;
            }

            fareMemoryRows[existing].Amount = amount;
        }
        else
        {
            fareMemoryRows.Add(new FareMemoryRow
            {
                Departure = dep,
                Arrival = arr,
                Amount = amount,
            });
        }

        return true;
    }

    public void RefreshDataLastWrittenFromDisk()
    {
        DateTimeOffset? latest = null;
        foreach (var path in new[] { patternsPath, entrySetsPath, settingsPath, fareMemoryPath })
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var written = new DateTimeOffset(File.GetLastWriteTime(path));
            if (latest is null || written > latest)
            {
                latest = written;
            }
        }

        DataLastWrittenAt = latest;
    }

    public void Save()
    {
        SaveEntrySets();
        SaveJson(settingsPath, Settings);
        SaveJson(patternsPath, Patterns);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SelectEntrySet(string setId)
    {
        if (setId == entrySetStore.ActiveSetId)
        {
            return;
        }

        if (entrySetStore.Sets.All(s => s.Id != setId))
        {
            return;
        }

        entrySetStore.ActiveSetId = setId;
        SaveEntrySets();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public EntrySet AddEntrySet(string? name = null)
    {
        var set = new EntrySet
        {
            Name = name ?? NextEntrySetName(),
        };
        entrySetStore.Sets.Add(set);
        entrySetStore.ActiveSetId = set.Id;
        SaveEntrySets();
        Changed?.Invoke(this, EventArgs.Empty);
        return set;
    }

    public void RenameEntrySet(string setId, string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        var set = entrySetStore.Sets.FirstOrDefault(s => s.Id == setId);
        if (set is null)
        {
            return;
        }

        set.Name = trimmed;
        set.NameManuallySet = true;
        SaveEntrySets();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool TryRemoveEntrySet(string setId)
    {
        if (entrySetStore.Sets.Count <= 1)
        {
            return false;
        }

        var index = entrySetStore.Sets.FindIndex(s => s.Id == setId);
        if (index < 0)
        {
            return false;
        }

        entrySetStore.Sets.RemoveAt(index);
        if (entrySetStore.ActiveSetId == setId)
        {
            entrySetStore.ActiveSetId = entrySetStore.Sets[0].Id;
        }

        SaveEntrySets();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void AddEntry(TripEntry entry)
    {
        if (Entries.Count >= MaxSuicaEntries)
        {
            throw new InvalidOperationException($"Suica明細は{MaxSuicaEntries}件までです。");
        }

        Entries.Add(entry);
        RememberFare(entry.Departure, entry.Arrival, entry.Amount);
        SaveEntrySets();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <returns>実際に追加した件数</returns>
    public int AddEntries(IEnumerable<TripEntry> newEntries)
    {
        var room = MaxSuicaEntries - Entries.Count;
        if (room <= 0)
        {
            return 0;
        }

        var batch = newEntries.Take(room).ToList();
        if (batch.Count == 0)
        {
            return 0;
        }

        Entries.AddRange(batch);
        foreach (var entry in batch)
        {
            RememberFare(entry.Departure, entry.Arrival, entry.Amount);
        }

        SaveEntrySets();
        Changed?.Invoke(this, EventArgs.Empty);
        return batch.Count;
    }

    public void UpdatePatterns(IEnumerable<RoutePattern> patterns)
    {
        Patterns = patterns.ToList();
        SaveJson(patternsPath, Patterns);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveEntry(string id)
    {
        Entries.RemoveAll(e => e.Id == id);
        SaveEntrySets();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateEntry(TripEntry updated)
    {
        var index = Entries.FindIndex(e => e.Id == updated.Id);
        if (index < 0)
        {
            return;
        }

        Entries[index] = updated;
        RememberFare(updated.Departure, updated.Arrival, updated.Amount);
        SaveEntrySets();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void LoadFareMemory()
    {
        fareMemoryRows = LoadJson<List<FareMemoryRow>>(fareMemoryPath) ?? [];
        if (fareMemoryRows.Count > 0)
        {
            return;
        }

        var changed = false;
        foreach (var set in entrySetStore.Sets)
        {
            foreach (var entry in set.Entries)
            {
                changed |= UpsertFareMemoryRow(entry.Departure, entry.Arrival, entry.Amount);
            }
        }

        if (changed)
        {
            SaveJson(fareMemoryPath, fareMemoryRows);
        }
    }

    /// <summary>同一日付内の Suica 行順（↑↓）用。</summary>
    public void MoveEntry(string id, int delta)
    {
        var index = Entries.FindIndex(e => e.Id == id);
        if (index < 0)
        {
            return;
        }

        var target = index + delta;
        if (target < 0 || target >= Entries.Count)
        {
            return;
        }

        (Entries[index], Entries[target]) = (Entries[target], Entries[index]);
        SaveEntrySets();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ReorderEntries(IReadOnlyList<TripEntry> ordered)
    {
        if (ordered.Count != Entries.Count)
        {
            return;
        }

        var idSet = new HashSet<string>(ordered.Select(e => e.Id));
        if (idSet.Count != Entries.Count || !Entries.All(e => idSet.Contains(e.Id)))
        {
            return;
        }

        var sameOrder = true;
        for (var i = 0; i < ordered.Count; i++)
        {
            if (Entries[i].Id != ordered[i].Id)
            {
                sameOrder = false;
                break;
            }
        }

        if (sameOrder)
        {
            return;
        }

        Entries.Clear();
        Entries.AddRange(ordered);
        SaveEntrySets();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ClearEntries()
    {
        Entries.Clear();
        SaveEntrySets();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public int ImportEntries(IEnumerable<TripEntry> imported) => AddEntries(imported);

    public void UpdateSettings(AppSettings settings)
    {
        Settings = settings;
        Save();
    }

    private EntrySet GetActiveSet()
    {
        var active = entrySetStore.Sets.FirstOrDefault(s => s.Id == entrySetStore.ActiveSetId);
        if (active is not null)
        {
            return active;
        }

        if (entrySetStore.Sets.Count == 0)
        {
            var fallback = new EntrySet { Name = "セット 1" };
            entrySetStore.Sets.Add(fallback);
            entrySetStore.ActiveSetId = fallback.Id;
            return fallback;
        }

        entrySetStore.ActiveSetId = entrySetStore.Sets[0].Id;
        return entrySetStore.Sets[0];
    }

    private void LoadEntrySets()
    {
        var store = LoadJson<EntrySetStore>(entrySetsPath);
        if (store is { Sets.Count: > 0 })
        {
            entrySetStore = store;
            if (entrySetStore.Sets.All(s => s.Id != entrySetStore.ActiveSetId))
            {
                entrySetStore.ActiveSetId = entrySetStore.Sets[0].Id;
            }

            var refreshed = RepairLegacyEntrySetAutoNameFlags();
            refreshed |= RefreshAutoEntrySetNames();
            if (refreshed)
            {
                SaveEntrySets();
                Changed?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        var legacy = LoadJson<List<TripEntry>>(legacyEntriesPath) ?? [];
        var initial = new EntrySet
        {
            Name = "セット 1",
            Entries = legacy,
        };
        entrySetStore = new EntrySetStore
        {
            ActiveSetId = initial.Id,
            Sets = [initial],
        };
        SaveEntrySets();
    }

    private void SaveEntrySets()
    {
        foreach (var set in entrySetStore.Sets)
        {
            ApplyAutoEntrySetName(set);
        }

        SaveJson(entrySetsPath, entrySetStore);
    }

    private static void ApplyAutoEntrySetName(EntrySet set)
    {
        if (set.NameManuallySet)
        {
            return;
        }

        var auto = DateFormatHelper.FormatEntrySetAutoName(set.Entries);
        if (auto.Length > 0)
        {
            set.Name = auto;
        }
    }

    private bool RepairLegacyEntrySetAutoNameFlags()
    {
        if (entrySetStore.AutoNameLegacyUnlockApplied)
        {
            return false;
        }

        entrySetStore.AutoNameLegacyUnlockApplied = true;
        foreach (var set in entrySetStore.Sets)
        {
            set.NameManuallySet = false;
        }

        return true;
    }

    private bool RefreshAutoEntrySetNames()
    {
        var changed = false;
        foreach (var set in entrySetStore.Sets)
        {
            if (set.NameManuallySet)
            {
                continue;
            }

            var auto = DateFormatHelper.FormatEntrySetAutoName(set.Entries);
            if (auto.Length == 0)
            {
                continue;
            }

            if (!string.Equals(set.Name, auto, StringComparison.Ordinal))
            {
                set.Name = auto;
                changed = true;
            }
        }

        return changed;
    }

    private string NextEntrySetName()
    {
        var used = new HashSet<string>(entrySetStore.Sets.Select(s => s.Name), StringComparer.Ordinal);
        for (var n = entrySetStore.Sets.Count + 1; n < 1000; n++)
        {
            var candidate = $"セット {n}";
            if (!used.Contains(candidate))
            {
                return candidate;
            }
        }

        return Guid.NewGuid().ToString("N")[..8];
    }

    private static bool ApplyPrintLayoutDefaultsV2(AppSettings settings)
    {
        if (settings.PrintLayoutDefaultsV2Applied)
        {
            return false;
        }

        settings.PrintLayoutDefaultsV2Applied = true;
        if (settings.PrintImageScale == 1f
            && settings.PrintOffsetXmm == 0f
            && settings.PrintOffsetYmm == 0f)
        {
            settings.PrintImageScale = 0.92f;
            settings.PrintOffsetYmm = 11f;
        }

        return true;
    }

    private static T? LoadJson<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private void SaveJson<T>(string path, T value)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions));
        NotifyDataPersisted();
    }

    private void NotifyDataPersisted()
    {
        RefreshDataLastWrittenFromDisk();
        DataPersisted?.Invoke(this, EventArgs.Empty);
    }
}

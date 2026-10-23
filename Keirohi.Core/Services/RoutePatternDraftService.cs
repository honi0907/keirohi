using Keirohi_App.Models;

namespace Keirohi_App.Services;

public sealed class RoutePatternDraftService
{
    private readonly Func<AppStateService> getState;
    private readonly List<RoutePattern> draft = [];
    private bool loaded;

    public RoutePatternDraftService(Func<AppStateService> getState)
    {
        this.getState = getState;
    }

    private AppStateService State => getState();

    public event EventHandler? Changed;

    public event EventHandler? StructureChanged;

    public event EventHandler? SelectionChanged;

    public event EventHandler? EditorWorkingChanged;

    public event EventHandler? EditorWorkingLegsChanged;

    public IReadOnlyList<RoutePattern> Draft => draft;

    public RoutePattern? Selected { get; private set; }

    public RoutePattern? EditorWorking { get; private set; }

    public PatternDraftSaveState GetPatternSaveState(RoutePattern pattern)
    {
        if (!IsSavable(pattern))
        {
            return PatternDraftSaveState.EmptyDraft;
        }

        var savedById = State.Patterns.ToDictionary(p => p.Id);
        if (!savedById.TryGetValue(pattern.Id, out var saved))
        {
            return PatternDraftSaveState.New;
        }

        return SnapshotContentEquals(pattern, saved)
            ? PatternDraftSaveState.Saved
            : PatternDraftSaveState.Modified;
    }

    public void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        ReloadFromSaved();
    }

    public void ReloadFromSaved()
    {
        var previousSelectedId = Selected?.Id;
        draft.Clear();
        foreach (var p in State.Patterns)
        {
            draft.Add(ClonePattern(p));
        }

        if (draft.Count == 0)
        {
            draft.Add(NewPattern());
        }

        var next = previousSelectedId is not null
            ? draft.FirstOrDefault(p => p.Id == previousSelectedId)
            : null;
        SetSelected(next ?? draft[0], raiseSelection: false);
        RaiseAll();
    }

    public string GetCountLabel()
    {
        var displayCount = CountForDisplay(draft);
        return displayCount == draft.Count
            ? $"登録パターン {displayCount} 件"
            : $"登録パターン {displayCount} 件（一覧 {draft.Count} 行）";
    }

    public void Select(RoutePattern? pattern)
    {
        if (Selected == pattern)
        {
            return;
        }

        SetSelected(pattern);
    }

    private void SetSelected(RoutePattern? pattern, bool raiseSelection = true)
    {
        Selected = pattern;
        ReloadEditorWorking();
        if (raiseSelection)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void NotifyDraftChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public void RequestEditorReloadFromDraft() => ReloadEditorWorking();

    public void NotifyEditorWorkingChanged() =>
        EditorWorkingChanged?.Invoke(this, EventArgs.Empty);

    public void NotifyEditorWorkingLegsChanged() =>
        EditorWorkingLegsChanged?.Invoke(this, EventArgs.Empty);

    public void ReloadEditorWorking()
    {
        EditorWorking = Selected is null ? null : ClonePattern(Selected);
        EditorWorkingChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CommitEditorWorking()
    {
        if (Selected is null || EditorWorking is null)
        {
            return;
        }

        ApplyEditorContent(Selected, EditorWorking);
    }

    public void ApplyEditorContent(RoutePattern target, RoutePattern source)
    {
        target.Name = source.Name;
        target.Legs = source.Legs.Select(l => new RoutePatternLeg
        {
            Departure = l.Departure,
            Arrival = l.Arrival,
            Mode = l.Mode,
            Note = l.Note,
            Amount = l.Amount,
        }).ToList();
        NotifyDraftChanged();
    }

    public RoutePattern AddNewPattern()
    {
        var pattern = NewPattern();
        draft.Add(pattern);
        SetSelected(pattern, raiseSelection: false);
        RaiseAll();
        return pattern;
    }

    public void DeleteSelected()
    {
        if (Selected is null)
        {
            return;
        }

        draft.Remove(Selected);
        if (draft.Count == 0)
        {
            draft.Add(NewPattern());
        }

        SetSelected(draft[0], raiseSelection: false);
        RaiseAll();
    }

    public void SyncDraftOrder(IReadOnlyList<RoutePattern> orderedPatterns)
    {
        if (orderedPatterns.Count != draft.Count)
        {
            return;
        }

        var byId = draft.ToDictionary(p => p.Id);
        var orderedIds = orderedPatterns.Select(p => p.Id).ToList();
        if (orderedIds.Count != byId.Count || orderedIds.Any(id => !byId.ContainsKey(id)))
        {
            return;
        }

        var currentIds = draft.Select(p => p.Id).ToList();
        if (orderedIds.SequenceEqual(currentIds))
        {
            return;
        }

        CommitEditorWorking();

        draft.Clear();
        foreach (var id in orderedIds)
        {
            draft.Add(byId[id]);
        }

        PersistDraftToDisk();
    }

    public void SetPatternListColor(RoutePattern pattern, string? listColorHex)
    {
        pattern.ListColor = string.IsNullOrWhiteSpace(listColorHex) ? null : listColorHex.Trim();
        PersistDraftToDisk();
    }

    /// <summary>
    /// 一覧の色・並びなど、エディタ外の変更をその場で PC に書き込む（ReloadFromSaved は行わない）。
    /// </summary>
    public void PersistDraftToDisk()
    {
        NormalizeDraftRows();
        var toSave = draft.Where(IsSavable).Select(ClonePattern).ToList();
        State.UpdatePatterns(toSave);
        NotifyDraftChanged();
    }

    public void OverwritePattern(RoutePattern target, RoutePattern source)
    {
        target.Name = source.Name;
        target.Legs = source.Legs.Select(l => new RoutePatternLeg
        {
            Departure = l.Departure,
            Arrival = l.Arrival,
            Mode = l.Mode,
            Note = l.Note,
            Amount = l.Amount,
        }).ToList();
        target.ListColor = source.ListColor;
        SetSelected(target, raiseSelection: false);
        RaiseAll();
    }

    public RoutePattern? FindByName(string name) =>
        draft.FirstOrDefault(p =>
            string.Equals(p.Name.Trim(), name.Trim(), StringComparison.Ordinal));

    /// <summary>
    /// 保存するとディスク上の同名 ID パターンを上書きするものの表示名（内容が変わっているもののみ）。
    /// </summary>
    public IReadOnlyList<string> GetOverwriteSavePatternNames(
        RoutePattern? editorOverlay = null,
        RoutePattern? editorTarget = null)
    {
        var savedById = State.Patterns.ToDictionary(p => p.Id);
        var names = new List<string>();
        foreach (var p in draft.Where(IsSavable))
        {
            var effective = editorOverlay is not null &&
                            editorTarget is not null &&
                            ReferenceEquals(p, editorTarget)
                ? editorOverlay
                : p;
            if (savedById.TryGetValue(p.Id, out var saved) && !ContentEquals(effective, saved))
            {
                names.Add(DisplayName(effective));
            }
        }

        return names;
    }

    /// <summary>
    /// 保存済みパターンへの変更を、元のパターンはそのままに新規 ID の別パターンとして下書きへ追加する。
    /// </summary>
    public (int AddedCount, string? LastAddedPatternId) SaveChangedPatternsAsSeparate()
    {
        var savedById = State.Patterns.ToDictionary(p => p.Id);
        var added = new List<RoutePattern>();

        foreach (var p in draft.Where(IsSavable).ToList())
        {
            if (!savedById.TryGetValue(p.Id, out var saved) || ContentEquals(p, saved))
            {
                continue;
            }

            var separate = ClonePattern(p);
            separate.Id = Guid.NewGuid().ToString();
            separate.ListColor = null;
            added.Add(separate);
            RestorePatternContent(p, saved);
        }

        foreach (var pattern in added)
        {
            draft.Add(pattern);
        }

        if (added.Count > 0)
        {
            SetSelected(added[^1], raiseSelection: false);
            StructureChanged?.Invoke(this, EventArgs.Empty);
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        return (added.Count, added.Count > 0 ? added[^1].Id : null);
    }

    public int SaveToDisk(string? selectPatternIdAfter = null)
    {
        NormalizeDraftRows();

        var focusId = selectPatternIdAfter ?? Selected?.Id;
        var toSave = draft.Where(IsSavable).Select(ClonePattern).ToList();

        State.UpdatePatterns(toSave);
        ApplySavedPatternsToDraftInPlace(toSave);

        var next = !string.IsNullOrEmpty(focusId)
            ? draft.FirstOrDefault(p => p.Id == focusId)
            : null;
        next ??= Selected is not null && draft.Contains(Selected) ? Selected : draft.FirstOrDefault();

        if (next is not null && !ReferenceEquals(Selected, next))
        {
            SetSelected(next, raiseSelection: false);
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ReloadEditorWorking();
        }

        StructureChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);

        NotifyDraftChanged();
        return toSave.Count;
    }

    /// <summary>
    /// 保存後も一覧の並び・空行の位置を維持し、内容だけディスク上のデータに合わせる。
    /// </summary>
    private void ApplySavedPatternsToDraftInPlace(IReadOnlyList<RoutePattern> saved)
    {
        var savedById = saved.ToDictionary(p => p.Id);
        for (var i = 0; i < draft.Count; i++)
        {
            if (savedById.TryGetValue(draft[i].Id, out var fromDisk))
            {
                draft[i] = ClonePattern(fromDisk);
            }
        }

        var selectedId = Selected?.Id;
        if (selectedId is not null)
        {
            Selected = draft.FirstOrDefault(p => p.Id == selectedId);
        }
    }

    private void NormalizeDraftRows()
    {
        foreach (var p in draft)
        {
            p.Name = p.Name.Trim();
            p.Legs = p.Legs
                .Where(l => !string.IsNullOrWhiteSpace(l.Departure) || !string.IsNullOrWhiteSpace(l.Arrival))
                .ToList();
            if (p.Legs.Count == 0)
            {
                p.Legs.Add(new RoutePatternLeg());
            }
        }
    }

    private static bool IsSavable(RoutePattern p) =>
        !string.IsNullOrWhiteSpace(p.Name) ||
        p.Legs.Any(l =>
            !string.IsNullOrWhiteSpace(l.Departure) && !string.IsNullOrWhiteSpace(l.Arrival));

    private static string DisplayName(RoutePattern p) =>
        string.IsNullOrWhiteSpace(p.Name) ? "(名称未設定)" : p.Name.Trim();

    private static void RestorePatternContent(RoutePattern target, RoutePattern source)
    {
        target.Name = source.Name;
        target.ListColor = source.ListColor;
        target.Legs = source.Legs.Select(l => new RoutePatternLeg
        {
            Departure = l.Departure,
            Arrival = l.Arrival,
            Mode = l.Mode,
            Note = l.Note,
            Amount = l.Amount,
        }).ToList();
    }

    private static bool SnapshotContentEquals(RoutePattern a, RoutePattern b)
    {
        if (!ContentEquals(a, b))
        {
            return false;
        }

        return string.Equals(a.ListColor ?? "", b.ListColor ?? "", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContentEquals(RoutePattern a, RoutePattern b)
    {
        if (!string.Equals(a.Name.Trim(), b.Name.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        var legsA = NormalizeLegsForCompare(a);
        var legsB = NormalizeLegsForCompare(b);
        if (legsA.Count != legsB.Count)
        {
            return false;
        }

        for (var i = 0; i < legsA.Count; i++)
        {
            var (depA, arrA, modeA, amountA) = legsA[i];
            var (depB, arrB, modeB, amountB) = legsB[i];
            if (depA != depB || arrA != arrB || modeA != modeB || amountA != amountB)
            {
                return false;
            }
        }

        return true;
    }

    private static List<(string Departure, string Arrival, TransportMode Mode, int Amount)> NormalizeLegsForCompare(
        RoutePattern p) =>
        p.Legs
            .Where(l => !string.IsNullOrWhiteSpace(l.Departure) || !string.IsNullOrWhiteSpace(l.Arrival))
            .Select(l => (l.Departure.Trim(), l.Arrival.Trim(), l.Mode, l.Amount))
            .ToList();

    private void RaiseAll()
    {
        StructureChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static int CountForDisplay(IEnumerable<RoutePattern> patterns) =>
        patterns.Count(p =>
            !string.IsNullOrWhiteSpace(p.Name) ||
            p.Legs.Any(l =>
                !string.IsNullOrWhiteSpace(l.Departure) || !string.IsNullOrWhiteSpace(l.Arrival)));

    private static RoutePattern NewPattern() =>
        new()
        {
            Name = "",
            Legs = [new RoutePatternLeg()],
        };

    public static RoutePattern ClonePattern(RoutePattern source) =>
        new()
        {
            Id = source.Id,
            Name = source.Name,
            ListColor = source.ListColor,
            Legs = source.Legs.Select(l => new RoutePatternLeg
            {
                Departure = l.Departure,
                Arrival = l.Arrival,
                Mode = l.Mode,
                Note = l.Note,
                Amount = l.Amount,
            }).ToList(),
        };
}

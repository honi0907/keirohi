using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Keirohi.Avalonia.Helpers;
using Keirohi.Avalonia.Models;
using Keirohi.Avalonia.Windows;
using static Keirohi.Avalonia.Helpers.EntryRowBackgroundBrushes;

namespace Keirohi.Avalonia.Controls;

public partial class RegistrationPreviewPanel : UserControl
{
    public ObservableCollection<TripEntryItem> Items { get; } = [];

    private bool suppressSetSelectionChange;
    private Func<Task>? registerAction;
    private string? registrationNavTag;
    private Button? registerActionButton;
    private readonly ListBoxDragReorderController<TripEntryItem> entryReorder;

    public RegistrationPreviewPanel()
    {
        InitializeComponent();
        EntryList.ItemsSource = Items;
        entryReorder = new ListBoxDragReorderController<TripEntryItem>(
            EntryList,
            Items,
            item => item.Id,
            ApplyEntryOrder,
            TrySyncEntryStateOrder);
        AppHost.State.Changed += (_, _) =>
        {
            ReloadFromState();
            UpdateRegisterButtonEnabled();
        };
        Loaded += (_, _) => ReloadFromState();
    }

    public Task<string?> TryApplyPatternToEntriesAsync() =>
        PatternApplyPanel.TryApplyToEntriesAsync();

    public void ConfigureRegistration(string? navTag, Func<Task>? action)
    {
        registrationNavTag = navTag;
        registerAction = action;
        RegisterMessageBanner.Hide();

        var active = navTag is "input" or "patterns";
        RegisterColumn.Opacity = active ? 1.0 : 0.45;
        PatternApplyPanel.IsEnabled = active && navTag == "patterns";

        InactiveRegisterHint.IsVisible = !active;
        InputRegisterHint.IsVisible = navTag == "input";
        PatternApplyPanel.IsVisible = navTag == "patterns";

        UpdateRegisterButtonEnabled();
    }

    public void ShowRegisterMessage(string message, MessageBannerSeverity severity = MessageBannerSeverity.Success) =>
        RegisterMessageBanner.Show(message, severity);

    public void SetRegisterButton(Button registerButton)
    {
        registerActionButton = registerButton;
        UpdateRegisterButtonEnabled();
    }

    public async Task PerformRegisterAsync()
    {
        if (registerAction is null)
        {
            return;
        }

        RegisterMessageBanner.Hide();
        await registerAction();
        UpdateRegisterButtonEnabled();
    }

    public async Task OpenPdfPreviewAsync()
    {
        if (AppHost.State.Entries.Count == 0)
        {
            await DialogService.ShowOkAsync("プレビュー", "登録がありません。");
            return;
        }

        var owner = AppHost.MainWindowInstance;
        if (owner is null)
        {
            return;
        }

        var window = new SuicaPreviewWindow();
        await window.ShowDialog(owner);
    }

    private void UpdateRegisterButtonEnabled()
    {
        if (registerActionButton is null)
        {
            return;
        }

        if (registrationNavTag is not ("input" or "patterns"))
        {
            registerActionButton.IsEnabled = false;
            return;
        }

        registerActionButton.IsEnabled =
            AppHost.State.Entries.Count < AppStateService.MaxSuicaEntries;
    }

    private void ReloadFromState()
    {
        ReloadEntrySetCombo();
        ReloadEntries();
    }

    private void ReloadEntrySetCombo()
    {
        var activeId = AppHost.State.ActiveEntrySetId;
        var items = AppHost.State.EntrySets
            .Select(s => new EntrySetListItem
            {
                Id = s.Id,
                Label = $"{s.Name}（{s.Entries.Count} 件）",
            })
            .ToList();

        suppressSetSelectionChange = true;
        EntrySetCombo.ItemsSource = items;
        EntrySetCombo.ItemTemplate = new FuncDataTemplate<EntrySetListItem>((item, _) =>
            new TextBlock { Text = item?.Label ?? "" });
        var index = items.FindIndex(i => i.Id == activeId);
        EntrySetCombo.SelectedIndex = index >= 0 ? index : items.Count > 0 ? 0 : -1;
        suppressSetSelectionChange = false;
    }

    private void ReloadEntries()
    {
        var selectedId = EntryList.SelectedItem is TripEntryItem selected ? selected.Id : null;
        Items.Clear();
        string? lastDate = null;
        var useAlternate = false;
        foreach (var entry in AppHost.State.Entries)
        {
            if (lastDate is not null && !string.Equals(entry.Date, lastDate, StringComparison.Ordinal))
            {
                useAlternate = !useAlternate;
            }

            var dateChanged = lastDate is not null &&
                              !string.Equals(entry.Date, lastDate, StringComparison.Ordinal);
            lastDate = entry.Date;
            var item = TripEntryItem.From(entry);
            item.RowBackground = useAlternate ? Alternate : Default;
            item.ItemMargin = dateChanged ? new Thickness(0, 8, 0, 0) : default;
            Items.Add(item);
        }

        UpdateEntrySelectionStates();
        if (selectedId is not null)
        {
            Reselect(selectedId);
        }

        var export = DateFormatHelper.EntriesForSuicaSheet(AppHost.State.Entries);
        var count = AppHost.State.Entries.Count;
        var total = AppHost.State.Entries.Sum(e => e.Amount);
        var exportCount = export.Count;
        var period = DateFormatHelper.FormatPeriodLabel(export);
        var setName = AppHost.State.ActiveEntrySet?.Name ?? "";
        var setPrefix = string.IsNullOrEmpty(setName) ? "" : $"「{setName}」";
        SummaryText.Text = count == 0
            ? $"{setPrefix}0 件 / 合計 ¥0"
            : exportCount < count
                ? $"{setPrefix}{count} 件 / 合計 ¥{total:N0} / 明細 {exportCount} 件\n期間 {period}"
                : $"{setPrefix}{count} 件 / 合計 ¥{total:N0}\n期間 {period}";

        var dateOutOfOrder = DateFormatHelper.HasDateOrderAnomalyInDisplayOrder(AppHost.State.Entries);
        DateOrderWarningBorder.IsVisible = dateOutOfOrder;
        DateOrderWarningText.Text = dateOutOfOrder
            ? "一覧の並びが日付順ではありません（後の日付が前の行より上にあるなど）。↑↓で直せます。Excel・印刷では日付順に並べ替えて出力します。"
            : "";

        UpdateEntryLimitWarning(count);
    }

    private void UpdateEntryLimitWarning(int count)
    {
        var max = AppStateService.MaxSuicaEntries;
        if (count >= max)
        {
            EntryLimitWarningBorder.IsVisible = true;
            EntryLimitWarningText.Text =
                $"{max}件に達したため、これ以上は登録できません。Excel・印刷で出力したあと、新しいセットを作って続けてください。";
            return;
        }

        if (count == max - 1)
        {
            EntryLimitWarningBorder.IsVisible = true;
            EntryLimitWarningText.Text = $"あと1件で{max}件の上限です。";
            return;
        }

        EntryLimitWarningBorder.IsVisible = false;
        EntryLimitWarningText.Text = "";
    }

    private void GoToExportPage_Click(object? sender, RoutedEventArgs e) =>
        AppHost.MainWindowInstance?.NavigateToNavTag("export");

    private void EntrySetCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (suppressSetSelectionChange)
        {
            return;
        }

        if (EntrySetCombo.SelectedItem is not EntrySetListItem item)
        {
            return;
        }

        AppHost.State.SelectEntrySet(item.Id);
    }

    private void NewEntrySet_Click(object? sender, RoutedEventArgs e) =>
        AppHost.State.AddEntrySet();

    private async void RenameEntrySet_Click(object? sender, RoutedEventArgs e)
    {
        var active = AppHost.State.ActiveEntrySet;
        if (active is null)
        {
            return;
        }

        var name = await DialogService.ShowPromptAsync("セット名の変更", active.Name, "例: 3月 前半");
        if (name is not null)
        {
            AppHost.State.RenameEntrySet(active.Id, name);
        }
    }

    private async void DeleteEntrySet_Click(object? sender, RoutedEventArgs e)
    {
        if (EntrySetCombo.SelectedItem is not EntrySetListItem item)
        {
            return;
        }

        if (AppHost.State.EntrySets.Count <= 1)
        {
            await DialogService.ShowOkAsync("削除", "最後の1つは削除できません。");
            return;
        }

        var set = AppHost.State.EntrySets.FirstOrDefault(s => s.Id == item.Id);
        var name = set?.Name ?? "このセット";
        var confirm = await DialogService.ShowConfirmAsync(
            "セットを削除",
            $"「{name}」とその中の登録をすべて削除しますか？",
            "削除",
            "キャンセル");
        if (confirm && !AppHost.State.TryRemoveEntrySet(item.Id))
        {
            await DialogService.ShowOkAsync("削除", "削除できませんでした。");
        }
    }

    private TripEntry? GetSelectedEntry()
    {
        if (EntryList.SelectedItem is not TripEntryItem item)
        {
            return null;
        }

        return AppHost.State.Entries.FirstOrDefault(e => e.Id == item.Id);
    }

    private async void EditSelected_Click(object? sender, RoutedEventArgs e)
    {
        var entry = GetSelectedEntry();
        if (entry is null)
        {
            await DialogService.ShowOkAsync("編集", "行を選択してください。");
            return;
        }

        var updated = await TripEntryEditDialog.ShowAsync(entry);
        if (updated is not null)
        {
            AppHost.State.UpdateEntry(updated);
        }
    }

    private void MoveUp_Click(object? sender, RoutedEventArgs e)
    {
        if (EntryList.SelectedItem is TripEntryItem item)
        {
            AppHost.State.MoveEntry(item.Id, -1);
            Reselect(item.Id);
        }
    }

    private void MoveDown_Click(object? sender, RoutedEventArgs e)
    {
        if (EntryList.SelectedItem is TripEntryItem item)
        {
            AppHost.State.MoveEntry(item.Id, 1);
            Reselect(item.Id);
        }
    }

    private void DeleteSelected_Click(object? sender, RoutedEventArgs e)
    {
        if (EntryList.SelectedItem is TripEntryItem item)
        {
            AppHost.State.RemoveEntry(item.Id);
        }
    }

    private async void ClearAll_Click(object? sender, RoutedEventArgs e)
    {
        if (AppHost.State.Entries.Count == 0)
        {
            return;
        }

        var setName = AppHost.State.ActiveEntrySet?.Name ?? "現在のセット";
        var confirm = await DialogService.ShowConfirmAsync(
            "すべて削除",
            $"「{setName}」の登録済み区間をすべて削除しますか？",
            "削除",
            "キャンセル");
        if (confirm)
        {
            AppHost.State.ClearEntries();
        }
    }

    private void Reselect(string id)
    {
        var index = Items.ToList().FindIndex(i => i.Id == id);
        if (index >= 0)
        {
            EntryList.SelectedIndex = index;
        }
    }

    private void EntryList_SelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateEntrySelectionStates();

    private void UpdateEntrySelectionStates()
    {
        var selectedId = EntryList.SelectedItem is TripEntryItem selected ? selected.Id : null;
        foreach (var item in Items)
        {
            item.IsSelected = item.Id == selectedId;
        }
    }

    private bool TrySyncEntryStateOrder()
    {
        if (StateMatchesItems())
        {
            return false;
        }

        ApplyEntryOrder(Items.ToList());
        return true;
    }

    private void ApplyEntryOrder(IReadOnlyList<TripEntryItem> orderedItems)
    {
        var byId = AppHost.State.Entries.ToDictionary(e => e.Id);
        var orderedEntries = orderedItems
            .Select(i => byId.TryGetValue(i.Id, out var entry) ? entry : null)
            .Where(e => e is not null)
            .Cast<TripEntry>()
            .ToList();
        if (orderedEntries.Count != Items.Count)
        {
            return;
        }

        AppHost.State.ReorderEntries(orderedEntries);
    }

    private bool StateMatchesItems()
    {
        if (AppHost.State.Entries.Count != Items.Count)
        {
            return false;
        }

        for (var i = 0; i < Items.Count; i++)
        {
            if (AppHost.State.Entries[i].Id != Items[i].Id)
            {
                return false;
            }
        }

        return true;
    }
}

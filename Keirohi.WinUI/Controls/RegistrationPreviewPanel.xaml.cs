using System.Collections.ObjectModel;
using Keirohi_App.Helpers;
using static Keirohi_App.Helpers.EntryRowBackgroundBrushes;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using WinRT.Interop;

namespace Keirohi_App.Controls;

public sealed partial class RegistrationPreviewPanel : UserControl
{
    public ObservableCollection<TripEntryItem> Items { get; } = [];

    private bool suppressSetSelectionChange;
    private Func<Task>? registerAction;
    private string? registrationNavTag;
    private Button? registerActionButton;
    private readonly ListViewDragReorderController<TripEntryItem> entryReorder;

    public RegistrationPreviewPanel()
    {
        InitializeComponent();
        EntryList.ItemsSource = Items;
        entryReorder = new ListViewDragReorderController<TripEntryItem>(
            EntryList,
            Items,
            ApplyEntryOrder,
            TrySyncEntryStateOrder);
        App.State.Changed += (_, _) =>
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

        InactiveRegisterHint.Visibility = active ? Visibility.Collapsed : Visibility.Visible;
        InputRegisterHint.Visibility = navTag == "input" ? Visibility.Visible : Visibility.Collapsed;
        PatternApplyPanel.Visibility = navTag == "patterns" ? Visibility.Visible : Visibility.Collapsed;

        UpdateRegisterButtonEnabled();
    }

    public void ShowRegisterMessage(string message, InfoBarSeverity severity = InfoBarSeverity.Success) =>
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
        if (App.State.Entries.Count == 0)
        {
            await ShowMessageAsync("プレビュー", "登録がありません。");
            return;
        }

        if (App.MainWindowInstance is null)
        {
            return;
        }

        var capture = new SuicaPreviewWindow();
        capture.AppWindow.Move(new Windows.Graphics.PointInt32(-24000, -24000));
        capture.Activate();
        try
        {
            var png = await capture.CapturePngAsync();
            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            capture.Close();
            SuicaGdiPrint.Show(hwnd, png);
        }
        catch (Exception ex)
        {
            capture.Close();
            await ShowMessageAsync("プレビュー", $"印刷プレビューを開けませんでした。\n\n{ex.Message}");
        }
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
            App.State.Entries.Count < AppStateService.MaxSuicaEntries;
    }

    private void ReloadFromState()
    {
        ReloadEntrySetCombo();
        ReloadEntries();
    }

    private void ReloadEntrySetCombo()
    {
        var activeId = App.State.ActiveEntrySetId;
        var items = App.State.EntrySets
            .Select(s => new EntrySetListItem
            {
                Id = s.Id,
                Label = $"{s.Name}（{s.Entries.Count} 件）",
            })
            .ToList();

        suppressSetSelectionChange = true;
        EntrySetCombo.ItemsSource = items;
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
        foreach (var entry in App.State.Entries)
        {
            if (lastDate is not null && !string.Equals(entry.Date, lastDate, StringComparison.Ordinal))
            {
                useAlternate = !useAlternate;
            }

            lastDate = entry.Date;
            var item = TripEntryItem.From(entry);
            item.RowBackground = useAlternate ? Alternate : Default;
            Items.Add(item);
        }

        UpdateEntrySelectionStates();
        if (selectedId is not null)
        {
            Reselect(selectedId);
        }

        var export = DateFormatHelper.EntriesForSuicaSheet(App.State.Entries);
        var count = App.State.Entries.Count;
        var total = App.State.Entries.Sum(e => e.Amount);
        var exportCount = export.Count;
        var period = DateFormatHelper.FormatPeriodLabel(export);
        var setName = App.State.ActiveEntrySet?.Name ?? "";
        var setPrefix = string.IsNullOrEmpty(setName) ? "" : $"「{setName}」";
        SummaryText.Text = count == 0
            ? $"{setPrefix}0 件 / 合計 ¥0"
            : exportCount < count
                ? $"{setPrefix}{count} 件 / 合計 ¥{total:N0} / 明細 {exportCount} 件\n期間 {period}"
                : $"{setPrefix}{count} 件 / 合計 ¥{total:N0}\n期間 {period}";

        var dateOutOfOrder = DateFormatHelper.HasDateOrderAnomalyInDisplayOrder(App.State.Entries);
        DateOrderWarningBorder.Visibility = dateOutOfOrder ? Visibility.Visible : Visibility.Collapsed;
        DateOrderWarningText.Text = dateOutOfOrder
            ? "一覧の並びが日付順ではありません（後の日付が前の行より上にあるなど）。↑↓やドラッグで直せます。Excel・印刷では日付順に並べ替えて出力します。"
            : "";

        UpdateEntryLimitWarning(count);
    }

    private void UpdateEntryLimitWarning(int count)
    {
        var max = AppStateService.MaxSuicaEntries;
        if (count >= max)
        {
            EntryLimitWarningBorder.Visibility = Visibility.Visible;
            EntryLimitWarningText.Text =
                $"{max}件に達したため、これ以上は登録できません。Excel・印刷で出力したあと、新しいセットを作って続けてください。";
            return;
        }

        if (count == max - 1)
        {
            EntryLimitWarningBorder.Visibility = Visibility.Visible;
            EntryLimitWarningText.Text = $"あと1件で{max}件の上限です。";
            return;
        }

        EntryLimitWarningBorder.Visibility = Visibility.Collapsed;
        EntryLimitWarningText.Text = "";
    }

    private void GoToExportPage_Click(object sender, RoutedEventArgs e) =>
        App.MainWindowInstance?.NavigateToNavTag("export");

    private void EntrySetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressSetSelectionChange)
        {
            return;
        }

        if (EntrySetCombo.SelectedItem is not EntrySetListItem item)
        {
            return;
        }

        App.State.SelectEntrySet(item.Id);
    }

    private void NewEntrySet_Click(object sender, RoutedEventArgs e)
    {
        App.State.AddEntrySet();
    }

    private async void RenameEntrySet_Click(object sender, RoutedEventArgs e)
    {
        var active = App.State.ActiveEntrySet;
        if (active is null)
        {
            return;
        }

        var box = new TextBox
        {
            Text = active.Name,
            PlaceholderText = "例: 3月 前半",
            SelectionStart = active.Name.Length,
        };
        var dialog = new ContentDialog
        {
            Title = "セット名の変更",
            Content = box,
            PrimaryButtonText = "保存",
            CloseButtonText = "キャンセル",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        App.State.RenameEntrySet(active.Id, box.Text);
    }

    private async void DeleteEntrySet_Click(object sender, RoutedEventArgs e)
    {
        if (EntrySetCombo.SelectedItem is not EntrySetListItem item)
        {
            return;
        }

        if (App.State.EntrySets.Count <= 1)
        {
            await ShowMessageAsync("削除", "最後の1つは削除できません。");
            return;
        }

        var set = App.State.EntrySets.FirstOrDefault(s => s.Id == item.Id);
        var name = set?.Name ?? "このセット";
        var dialog = new ContentDialog
        {
            Title = "セットを削除",
            Content = $"「{name}」とその中の登録をすべて削除しますか？",
            PrimaryButtonText = "削除",
            CloseButtonText = "キャンセル",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            if (!App.State.TryRemoveEntrySet(item.Id))
            {
                await ShowMessageAsync("削除", "削除できませんでした。");
            }
        }
    }

    private TripEntry? GetSelectedEntry()
    {
        if (EntryList.SelectedItem is not TripEntryItem item)
        {
            return null;
        }

        return App.State.Entries.FirstOrDefault(e => e.Id == item.Id);
    }

    private async void EditSelected_Click(object sender, RoutedEventArgs e)
    {
        var entry = GetSelectedEntry();
        if (entry is null)
        {
            await ShowMessageAsync("編集", "行を選択してください。");
            return;
        }

        var updated = await TripEntryEditDialog.ShowAsync(XamlRoot, entry);
        if (updated is not null)
        {
            App.State.UpdateEntry(updated);
        }
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (EntryList.SelectedItem is TripEntryItem item)
        {
            App.State.MoveEntry(item.Id, -1);
            Reselect(item.Id);
        }
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (EntryList.SelectedItem is TripEntryItem item)
        {
            App.State.MoveEntry(item.Id, 1);
            Reselect(item.Id);
        }
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (EntryList.SelectedItem is TripEntryItem item)
        {
            App.State.RemoveEntry(item.Id);
        }
    }

    private async void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        if (App.State.Entries.Count == 0)
        {
            return;
        }

        var setName = App.State.ActiveEntrySet?.Name ?? "現在のセット";
        var dialog = new ContentDialog
        {
            Title = "すべて削除",
            Content = $"「{setName}」の登録済み区間をすべて削除しますか？",
            PrimaryButtonText = "削除",
            CloseButtonText = "キャンセル",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            App.State.ClearEntries();
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

    private void EntryList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateEntrySelectionStates();

    private void UpdateEntrySelectionStates()
    {
        var selectedId = EntryList.SelectedItem is TripEntryItem selected ? selected.Id : null;
        foreach (var item in Items)
        {
            item.IsSelected = item.Id == selectedId;
        }
    }

    private void EntryList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args) =>
        entryReorder.OnContainerContentChanging(sender, args);

    private void EntryList_DragItemsStarting(object sender, DragItemsStartingEventArgs e) =>
        entryReorder.OnDragItemsStarting(e);

    private void EntryList_DragOver(object sender, DragEventArgs e) =>
        entryReorder.OnDragOver(e);

    private void EntryList_Drop(object sender, DragEventArgs e) =>
        entryReorder.OnDrop(e);

    private void EntryList_DragItemsCompleted(object sender, DragItemsCompletedEventArgs e) =>
        entryReorder.OnDragItemsCompleted(e);

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
        var byId = App.State.Entries.ToDictionary(e => e.Id);
        var orderedEntries = orderedItems
            .Select(i => byId.TryGetValue(i.Id, out var entry) ? entry : null)
            .Where(e => e is not null)
            .Cast<TripEntry>()
            .ToList();
        if (orderedEntries.Count != Items.Count)
        {
            return;
        }

        App.State.ReorderEntries(orderedEntries);
    }

    private bool StateMatchesItems()
    {
        if (App.State.Entries.Count != Items.Count)
        {
            return false;
        }

        for (var i = 0; i < Items.Count; i++)
        {
            if (App.State.Entries[i].Id != Items[i].Id)
            {
                return false;
            }
        }

        return true;
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot,
        };
        await dialog.ShowAsync();
    }
}

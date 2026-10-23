using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Keirohi.Avalonia.Helpers;
using Keirohi.Avalonia.Models;

namespace Keirohi.Avalonia.Controls;

public partial class RoutePatternListPane : UserControl
{
    private readonly RoutePatternDraftService draft = AppHost.RoutePatternDraft;
    private readonly ObservableCollection<RoutePatternListRow> rows = [];
    private FareIndexService? fareIndex;
    private bool suppressSelectionSync;
    private readonly ListBoxDragReorderController<RoutePatternListRow> patternReorder;

    public RoutePatternListPane()
    {
        InitializeComponent();
        PatternList.ItemsSource = rows;
        patternReorder = new ListBoxDragReorderController<RoutePatternListRow>(
            PatternList,
            rows,
            row => row.Pattern.Id,
            ApplyPatternOrder,
            TrySyncPatternDraftOrder);
        BuildColorMenu();
        draft.StructureChanged += (_, _) => RebuildRows();
        draft.Changed += (_, _) => RefreshRowLabels();
        draft.SelectionChanged += (_, _) => SyncListSelection();
        draft.EditorWorkingChanged += (_, _) => RefreshRowLabels();
        draft.EditorWorkingLegsChanged += (_, _) => RefreshRowLabels();
        Loaded += (_, _) =>
        {
            draft.EnsureLoaded();
            RebuildRows();
        };
    }

    private void BuildColorMenu()
    {
        ColorContextMenu.Items.Clear();
        foreach (var preset in RoutePatternListColorHelper.Presets)
        {
            var captured = preset;
            var item = new MenuItem
            {
                Header = captured.Label,
                Icon = CreateColorSwatch(captured.Hex),
            };
            item.Click += (_, _) =>
            {
                if (PatternList.SelectedItem is RoutePatternListRow row)
                {
                    draft.SetPatternListColor(row.Pattern, captured.Hex);
                    row.Refresh();
                }
            };
            ColorContextMenu.Items.Add(item);
        }
    }

    private static Border CreateColorSwatch(string? hex)
    {
        var swatch = new Border
        {
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (hex is null)
        {
            swatch.Background = Brushes.Transparent;
            swatch.BorderBrush = new SolidColorBrush(Color.Parse("#FF8C8C8C"));
            return swatch;
        }

        swatch.Background = RoutePatternListColorHelper.ToRowBackgroundBrush(hex);
        swatch.BorderBrush = RoutePatternListColorHelper.ToAccentBarBrush(hex);
        return swatch;
    }

    private void RebuildRows()
    {
        RefreshFareIndex();
        rows.Clear();
        foreach (var pattern in draft.Draft)
        {
            var row = new RoutePatternListRow(pattern);
            row.Refresh(fareIndex, AmountSourceFor(row.Pattern));
            rows.Add(row);
        }

        PatternCountText.Text = draft.GetCountLabel();
        SyncListSelection();
    }

    private void RefreshRowLabels()
    {
        RefreshFareIndex();
        PatternCountText.Text = draft.GetCountLabel();
        foreach (var row in rows)
        {
            row.Refresh(fareIndex, AmountSourceFor(row.Pattern));
        }
    }

    private RoutePattern AmountSourceFor(RoutePattern pattern) =>
        ReferenceEquals(pattern, draft.Selected) && draft.EditorWorking is not null
            ? draft.EditorWorking
            : pattern;

    private void RefreshFareIndex() => fareIndex = AppHost.State.GetFareIndex();

    private void SyncListSelection()
    {
        var selected = draft.Selected;
        suppressSelectionSync = true;
        PatternList.SelectedItem = selected is null
            ? null
            : rows.FirstOrDefault(r => ReferenceEquals(r.Pattern, selected));
        suppressSelectionSync = false;
        UpdateRowSelectionStates();
    }

    private void UpdateRowSelectionStates()
    {
        var selected = draft.Selected;
        foreach (var row in rows)
        {
            row.IsSelected = ReferenceEquals(row.Pattern, selected);
        }
    }

    private void PatternList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (suppressSelectionSync)
        {
            return;
        }

        if (PatternList.SelectedItem is RoutePatternListRow row)
        {
            draft.Select(row.Pattern);
        }

        UpdateRowSelectionStates();
    }

    private void PatternList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (PatternList.SelectedItem is RoutePatternListRow row &&
            ReferenceEquals(draft.Selected, row.Pattern))
        {
            draft.RequestEditorReloadFromDraft();
        }
    }

    private void NewPattern_Click(object? sender, RoutedEventArgs e) => draft.AddNewPattern();

    private async void DeletePattern_Click(object? sender, RoutedEventArgs e)
    {
        if (draft.Selected is null)
        {
            return;
        }

        var confirm = await DialogService.ShowConfirmAsync(
            "パターン削除",
            "選択中のパターンを削除しますか？",
            "削除",
            "キャンセル");
        if (confirm)
        {
            draft.DeleteSelected();
        }
    }

    private bool TrySyncPatternDraftOrder()
    {
        if (DraftMatchesRows())
        {
            return false;
        }

        draft.SyncDraftOrder(rows.Select(r => r.Pattern).ToList());
        return true;
    }

    private void ApplyPatternOrder(IReadOnlyList<RoutePatternListRow> orderedRows)
    {
        for (var targetIndex = 0; targetIndex < orderedRows.Count; targetIndex++)
        {
            var row = orderedRows[targetIndex];
            var currentIndex = rows.IndexOf(row);
            if (currentIndex >= 0 && currentIndex != targetIndex)
            {
                rows.Move(currentIndex, targetIndex);
            }
        }

        draft.SyncDraftOrder(orderedRows.Select(r => r.Pattern).ToList());
    }

    private bool DraftMatchesRows()
    {
        var savedOrder = draft.Draft;
        if (savedOrder.Count != rows.Count)
        {
            return false;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            if (!ReferenceEquals(rows[i].Pattern, savedOrder[i]))
            {
                return false;
            }
        }

        return true;
    }
}

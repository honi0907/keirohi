using System.Collections.ObjectModel;
using System.Linq;
using Keirohi_App.Helpers;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace Keirohi_App.Controls;

public sealed partial class RoutePatternListPane : UserControl
{
    private readonly RoutePatternDraftService draft = App.RoutePatternDraft;
    private readonly ObservableCollection<RoutePatternListRow> rows = [];
    private FareIndexService? fareIndex;
    private static readonly SolidColorBrush TransparentMenuBrush = new(Color.FromArgb(0, 0, 0, 0));
    private static readonly SolidColorBrush MenuHoverBrush = new(Color.FromArgb(28, 0, 0, 0));
    private static readonly SolidColorBrush SwatchOutlineBrush = new(Color.FromArgb(255, 140, 140, 140));

    private bool suppressSelectionSync;
    private readonly ListViewDragReorderController<RoutePatternListRow> patternReorder;

    public RoutePatternListPane()
    {
        InitializeComponent();
        PatternList.ItemsSource = rows;
        patternReorder = new ListViewDragReorderController<RoutePatternListRow>(
            PatternList,
            rows,
            ApplyPatternOrder,
            TrySyncPatternDraftOrder);
        PatternList.ContainerContentChanging += PatternList_ContainerContentChanging;
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

    private void RefreshFareIndex() => fareIndex = App.State.GetFareIndex();

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

    private void PatternList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args) =>
        patternReorder.OnContainerContentChanging(sender, args);

    private void PatternList_DragItemsStarting(object sender, DragItemsStartingEventArgs e) =>
        patternReorder.OnDragItemsStarting(e);

    private void PatternList_DragOver(object sender, DragEventArgs e) =>
        patternReorder.OnDragOver(e);

    private void PatternList_Drop(object sender, DragEventArgs e) =>
        patternReorder.OnDrop(e);

    private void PatternList_DragItemsCompleted(object sender, DragItemsCompletedEventArgs e) =>
        patternReorder.OnDragItemsCompleted(e);

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

    private void PatternList_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var row = FindRowFromSource(e.OriginalSource as DependencyObject);
        if (row is null)
        {
            return;
        }

        e.Handled = true;
        draft.Select(row.Pattern);
        ShowListColorMenu(e.GetPosition(PatternList), row);
    }

    private void ShowListColorMenu(Point position, RoutePatternListRow row)
    {
        var panel = new StackPanel { Spacing = 2, MinWidth = 168 };
        var flyout = new Flyout { Content = panel };
        foreach (var preset in RoutePatternListColorHelper.Presets)
        {
            var hex = preset.Hex;
            var item = new Border
            {
                Padding = new Thickness(8, 5, 10, 5),
                CornerRadius = new CornerRadius(4),
                Background = TransparentMenuBrush,
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Children =
                    {
                        CreateColorSwatch(hex),
                        new TextBlock
                        {
                            Text = preset.Label,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 14,
                        },
                    },
                },
            };
            item.PointerEntered += (_, _) => item.Background = MenuHoverBrush;
            item.PointerExited += (_, _) => item.Background = TransparentMenuBrush;
            item.Tapped += (_, _) =>
            {
                draft.SetPatternListColor(row.Pattern, hex);
                row.Refresh();
                flyout.Hide();
            };
            panel.Children.Add(item);
        }

        flyout.ShowAt(PatternList, new FlyoutShowOptions { Position = position });
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
            swatch.Background = TransparentMenuBrush;
            swatch.BorderBrush = SwatchOutlineBrush;
            return swatch;
        }

        swatch.Background = RoutePatternListColorHelper.ToRowBackgroundBrush(hex);
        swatch.BorderBrush = RoutePatternListColorHelper.ToAccentBarBrush(hex);
        return swatch;
    }

    private static RoutePatternListRow? FindRowFromSource(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is FrameworkElement { DataContext: RoutePatternListRow row })
            {
                return row;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private void PatternList_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

    private void PatternList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RoutePatternListRow row &&
            ReferenceEquals(draft.Selected, row.Pattern))
        {
            draft.RequestEditorReloadFromDraft();
        }
    }

    private void NewPattern_Click(object sender, RoutedEventArgs e) => draft.AddNewPattern();

    private async void DeletePattern_Click(object sender, RoutedEventArgs e)
    {
        if (draft.Selected is null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "パターン削除",
            Content = "選択中のパターンを削除しますか？",
            PrimaryButtonText = "削除",
            CloseButtonText = "キャンセル",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            draft.DeleteSelected();
        }
    }
}

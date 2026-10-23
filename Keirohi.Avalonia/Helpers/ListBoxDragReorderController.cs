using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Keirohi.Avalonia.Helpers;

/// <summary>
/// ListBox 内のドラッグ並べ替え（WinUI ListViewDragReorderController 相当）。
/// </summary>
public sealed class ListBoxDragReorderController<T> where T : class
{
    private static readonly DataFormat<string> ReorderIdFormat =
        DataFormat.CreateStringApplicationFormat("Keirohi.ListReorder");

    private readonly ListBox listBox;
    private readonly IList<T> items;
    private readonly Func<T, string> getItemKey;
    private readonly Action<IReadOnlyList<T>> applyOrder;
    private readonly Func<bool>? tryResolveExternalOrderMismatch;

    private T? draggingItem;
    private double lastDragY = double.NaN;
    private int lastInsertIndex = -1;
    private Visual? coordinateRoot;
    private bool reorderCommitted;
    private Point? dragStart;
    private Canvas? overlayCanvas;
    private Border? insertLine;
    private Border? dragGhost;
    private bool dragVisualActive;
    private double overlayLineY = double.NaN;
    private double overlayLineTargetY;
    private double ghostGrabOffsetY;
    private double ghostRowHeight = 28;
    private DispatcherTimer? overlayTimer;
    private bool overlayTimerActive;

    private const double OverlayLerp = 0.28;

    public ListBoxDragReorderController(
        ListBox listBox,
        IList<T> items,
        Func<T, string> getItemKey,
        Action<IReadOnlyList<T>> applyOrder,
        Func<bool>? tryResolveExternalOrderMismatch = null)
    {
        this.listBox = listBox;
        this.items = items;
        this.getItemKey = getItemKey;
        this.applyOrder = applyOrder;
        this.tryResolveExternalOrderMismatch = tryResolveExternalOrderMismatch;

        DragDrop.SetAllowDrop(listBox, true);
        listBox.AddHandler(DragDrop.DragOverEvent, OnListDragOver, RoutingStrategies.Bubble, handledEventsToo: true);
        listBox.AddHandler(DragDrop.DragLeaveEvent, OnListDragLeave, RoutingStrategies.Bubble, handledEventsToo: true);
        listBox.AddHandler(DragDrop.DropEvent, OnListDrop, RoutingStrategies.Bubble, handledEventsToo: true);
        listBox.ContainerPrepared += OnContainerPrepared;
        listBox.ContainerClearing += OnContainerClearing;
        listBox.AttachedToVisualTree += (_, _) => AttachExistingContainers();
    }

    private Visual CoordinateRoot => coordinateRoot ??= ResolveCoordinateRoot();

    private Visual ResolveCoordinateRoot()
    {
        foreach (var visual in listBox.GetVisualDescendants())
        {
            if (visual is ItemsPresenter presenter)
            {
                return presenter;
            }
        }

        return listBox;
    }

    private void AttachExistingContainers()
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (listBox.ContainerFromIndex(i) is ListBoxItem item)
            {
                ResetRowChrome(item);
                AttachContainerHandlers(item);
            }
        }
    }

    private void OnContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is ListBoxItem item)
        {
            ResetRowChrome(item);
            AttachContainerHandlers(item);
        }
    }

    private void OnContainerClearing(object? sender, ContainerClearingEventArgs e)
    {
        if (e.Container is ListBoxItem item)
        {
            DetachContainerHandlers(item);
        }
    }

    private void AttachContainerHandlers(ListBoxItem item)
    {
        if (item.Tag is ReorderHookTag)
        {
            return;
        }

        item.Tag = new ReorderHookTag();
        DragDrop.SetAllowDrop(item, true);
        item.AddHandler(InputElement.PointerPressedEvent, OnItemPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        item.AddHandler(InputElement.PointerMovedEvent, OnItemPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        item.AddHandler(InputElement.PointerReleasedEvent, OnItemPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        item.AddHandler(DragDrop.DragOverEvent, OnItemDragOver, RoutingStrategies.Bubble, handledEventsToo: true);
        item.AddHandler(DragDrop.DropEvent, OnItemDrop, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private static void DetachContainerHandlers(ListBoxItem item)
    {
        item.Tag = null;
    }

    private static void ResetRowChrome(ListBoxItem item)
    {
        item.Opacity = 1.0;
        item.RenderTransform = null;
    }

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ListBoxItem { DataContext: T item })
        {
            return;
        }

        if (!e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed)
        {
            return;
        }

        reorderCommitted = false;
        draggingItem = item;
        dragStart = e.GetPosition(CoordinateRoot);
        lastDragY = dragStart.Value.Y;
        lastInsertIndex = IndexOfItem(item);
    }

    private async void OnItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (draggingItem is null || dragStart is null)
        {
            return;
        }

        var pt = e.GetCurrentPoint(listBox);
        if (!pt.Properties.IsLeftButtonPressed)
        {
            draggingItem = null;
            dragStart = null;
            return;
        }

        var pos = e.GetPosition(CoordinateRoot);
        if (Math.Abs(pos.Y - dragStart.Value.Y) < 8)
        {
            return;
        }

        var item = draggingItem;
        dragStart = null;
        reorderCommitted = false;
        lastDragY = pos.Y;
        lastInsertIndex = InsertIndexFromPointerY(lastDragY);

        EnsureOverlayHost();
        dragVisualActive = true;
        CaptureGhostMetrics(item, pos.Y);
        ResetOverlayAnimationState();
        UpdateOverlayTargets();
        StartOverlayTimer();
        SnapOverlayToTargets();

        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.Create(ReorderIdFormat, getItemKey(item)));
        try
        {
            await DragDrop.DoDragDropAsync(e, transfer, DragDropEffects.Move);
        }
        finally
        {
            ClearDragVisuals();
        }

        draggingItem = item;
        await TryCommitAsync();
    }

    private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (draggingItem is null)
        {
            dragStart = null;
        }
    }

    private void OnListDragOver(object? sender, DragEventArgs e)
    {
        RememberDragY(e);
    }

    private void OnItemDragOver(object? sender, DragEventArgs e)
    {
        RememberDragY(e, sender as ListBoxItem);
    }

    private void OnListDragLeave(object? sender, DragEventArgs e)
    {
        // 子 ListBoxItem へ入ったときも ListBox で Leave が飛ぶため、ドラッグ中は消さない
        if (dragVisualActive)
        {
            return;
        }

        HideOverlay();
    }

    private void OnListDrop(object? sender, DragEventArgs e)
    {
        RememberDragY(e);
        TryCommit();
        ClearDragVisuals();
        e.Handled = true;
    }

    private void OnItemDrop(object? sender, DragEventArgs e)
    {
        OnListDrop(sender, e);
    }

    private void RememberDragY(DragEventArgs e, ListBoxItem? overItem = null)
    {
        var transfer = e.DataTransfer;
        if (!transfer.Contains(ReorderIdFormat))
        {
            return;
        }

        var id = transfer.TryGetValue(ReorderIdFormat);
        if (!string.IsNullOrEmpty(id))
        {
            draggingItem = FindByKey(id);
        }

        lastDragY = e.GetPosition(CoordinateRoot).Y;
        lastInsertIndex = ResolveInsertIndex(e, overItem);
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;

        if (!dragVisualActive)
        {
            dragVisualActive = true;
            EnsureOverlayHost();
            if (draggingItem is not null)
            {
                CaptureGhostMetrics(draggingItem, lastDragY);
            }

            ResetOverlayAnimationState();
            StartOverlayTimer();
        }

        UpdateOverlayTargets();
    }

    private int ResolveInsertIndex(DragEventArgs e, ListBoxItem? overItem)
    {
        if (overItem is not null)
        {
            var index = listBox.IndexFromContainer(overItem);
            if (index >= 0)
            {
                var yInItem = e.GetPosition(overItem).Y;
                var height = overItem.Bounds.Height;
                if (height <= 0)
                {
                    height = 40;
                }

                return yInItem < height / 2 ? index : index + 1;
            }
        }

        return InsertIndexFromPointerY(lastDragY);
    }

    private void EnsureOverlayHost()
    {
        if (overlayCanvas is not null)
        {
            return;
        }

        if (listBox.Parent is Border border && ReferenceEquals(border.Child, listBox))
        {
            var grid = new Grid();
            border.Child = null;
            grid.Children.Add(listBox);
            overlayCanvas = CreateOverlayCanvas();
            grid.Children.Add(overlayCanvas);
            border.Child = grid;
        }
        else if (listBox.Parent is Panel panel)
        {
            overlayCanvas = CreateOverlayCanvas();
            if (panel is Grid grid)
            {
                Grid.SetRow(overlayCanvas, Grid.GetRow(listBox));
                Grid.SetColumn(overlayCanvas, Grid.GetColumn(listBox));
                Grid.SetRowSpan(overlayCanvas, Grid.GetRowSpan(listBox));
                Grid.SetColumnSpan(overlayCanvas, Grid.GetColumnSpan(listBox));
            }

            overlayCanvas.ZIndex = 100;
            panel.Children.Add(overlayCanvas);
        }

        if (overlayCanvas is null)
        {
            return;
        }

        insertLine = new Border
        {
            Height = 3,
            CornerRadius = new CornerRadius(1.5),
            Background = new SolidColorBrush(Color.Parse("#FF0078D4")),
            IsVisible = false,
            IsHitTestVisible = false,
        };
        dragGhost = new Border
        {
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.FromArgb(0x55, 0x00, 0x78, 0xD4)),
            BorderBrush = new SolidColorBrush(Color.Parse("#FF0078D4")),
            BorderThickness = new Thickness(1),
            IsVisible = false,
            IsHitTestVisible = false,
        };
        overlayCanvas.Children.Add(dragGhost);
        overlayCanvas.Children.Add(insertLine);
    }

    private static Canvas CreateOverlayCanvas() => new()
    {
        IsHitTestVisible = false,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
    };

    private void StartOverlayTimer()
    {
        overlayTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        if (overlayTimerActive)
        {
            return;
        }

        overlayTimer.Tick -= OnOverlayTimerTick;
        overlayTimer.Tick += OnOverlayTimerTick;
        overlayTimer.Start();
        overlayTimerActive = true;
    }

    private void StopOverlayTimer()
    {
        if (overlayTimer is null || !overlayTimerActive)
        {
            return;
        }

        overlayTimer.Tick -= OnOverlayTimerTick;
        overlayTimer.Stop();
        overlayTimerActive = false;
    }

    private void OnOverlayTimerTick(object? sender, EventArgs e)
    {
        if (!dragVisualActive)
        {
            StopOverlayTimer();
            return;
        }

        ApplyOverlayFrame();
    }

    private void UpdateOverlayTargets()
    {
        if (insertLine is null || dragGhost is null || overlayCanvas is null || items.Count == 0 || lastInsertIndex < 0)
        {
            return;
        }

        var insertIndex = Math.Clamp(lastInsertIndex, 0, items.Count);
        overlayLineTargetY = GetLineYForInsertIndex(insertIndex) - 1.5;
    }

    private void CaptureGhostMetrics(T item, double pointerYInRoot)
    {
        ghostRowHeight = 28;
        ghostGrabOffsetY = 0;

        var from = IndexOfItem(item);
        if (from < 0 || listBox.ContainerFromIndex(from) is not Visual source)
        {
            return;
        }

        ghostRowHeight = Math.Max(28, source.Bounds.Height);
        var top = source.TranslatePoint(new Point(0, 0), CoordinateRoot)?.Y ?? pointerYInRoot;
        ghostGrabOffsetY = pointerYInRoot - top;
    }

    private void SnapOverlayToTargets()
    {
        overlayLineY = overlayLineTargetY;
        ApplyOverlayFrame();
    }

    private void ApplyOverlayFrame()
    {
        if (insertLine is null || dragGhost is null || overlayCanvas is null || !dragVisualActive)
        {
            return;
        }

        if (double.IsNaN(overlayLineY))
        {
            overlayLineY = overlayLineTargetY;
        }
        else
        {
            overlayLineY += (overlayLineTargetY - overlayLineY) * OverlayLerp;
        }

        var width = Math.Max(40, listBox.Bounds.Width - 12);
        Canvas.SetLeft(insertLine, 6);
        Canvas.SetTop(insertLine, overlayLineY);
        insertLine.Width = width;
        insertLine.IsVisible = true;

        if (draggingItem is not null && !double.IsNaN(lastDragY))
        {
            var ghostTopInRoot = lastDragY - ghostGrabOffsetY;
            var ghostTop = CoordinateRoot.TranslatePoint(new Point(0, ghostTopInRoot), overlayCanvas)?.Y ?? ghostTopInRoot;
            Canvas.SetLeft(dragGhost, 6);
            Canvas.SetTop(dragGhost, ghostTop);
            dragGhost.Width = width;
            dragGhost.Height = ghostRowHeight;
            dragGhost.IsVisible = true;
        }
        else if (dragGhost.IsVisible)
        {
            dragGhost.IsVisible = false;
        }
    }

    private void ResetOverlayAnimationState()
    {
        overlayLineY = double.NaN;
    }

    private double GetLineYForInsertIndex(int insertIndex)
    {
        if (overlayCanvas is null)
        {
            return 0;
        }

        if (insertIndex <= 0)
        {
            if (listBox.ContainerFromIndex(0) is Visual first)
            {
                return first.TranslatePoint(new Point(0, 0), overlayCanvas)?.Y ?? 0;
            }

            return 0;
        }

        if (insertIndex >= items.Count)
        {
            var lastIndex = items.Count - 1;
            if (listBox.ContainerFromIndex(lastIndex) is Visual last)
            {
                var bottom = last.TranslatePoint(new Point(0, last.Bounds.Height), overlayCanvas);
                return bottom?.Y ?? 0;
            }

            return 0;
        }

        if (listBox.ContainerFromIndex(insertIndex) is Visual target)
        {
            return target.TranslatePoint(new Point(0, 0), overlayCanvas)?.Y ?? 0;
        }

        return 0;
    }

    private void HideOverlay()
    {
        if (insertLine is not null)
        {
            insertLine.IsVisible = false;
        }

        if (dragGhost is not null)
        {
            dragGhost.IsVisible = false;
        }
    }

    private void ClearDragVisuals()
    {
        dragVisualActive = false;
        StopOverlayTimer();
        HideOverlay();
        ResetOverlayAnimationState();
        ResetAllRowChrome();
    }

    private void ResetAllRowChrome()
    {
        foreach (var visual in listBox.GetVisualDescendants())
        {
            if (visual is ListBoxItem row)
            {
                ResetRowChrome(row);
            }
        }
    }

    private T? FindByKey(string id)
    {
        foreach (var item in items)
        {
            if (string.Equals(getItemKey(item), id, StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    private async Task TryCommitAsync()
    {
        await Task.Yield();
        TryCommit();
    }

    private bool TryCommit()
    {
        if (reorderCommitted)
        {
            return true;
        }

        if (draggingItem is null || lastInsertIndex < 0)
        {
            if (tryResolveExternalOrderMismatch?.Invoke() == true)
            {
                reorderCommitted = true;
                draggingItem = null;
                return true;
            }

            return false;
        }

        var from = IndexOfItem(draggingItem);
        if (from < 0)
        {
            return false;
        }

        var rawInsert = Math.Clamp(lastInsertIndex, 0, items.Count);
        if (rawInsert == from || rawInsert == from + 1)
        {
            return false;
        }

        var insertAt = rawInsert;
        if (insertAt > from)
        {
            insertAt--;
        }

        if (insertAt < 0 || insertAt >= items.Count)
        {
            return false;
        }

        var ordered = items.ToList();
        var row = ordered[from];
        ordered.RemoveAt(from);
        ordered.Insert(insertAt, row);
        applyOrder(ordered);
        ResetAllRowChrome();
        reorderCommitted = true;
        draggingItem = null;
        lastInsertIndex = -1;
        return true;
    }

    private int InsertIndexFromPointerY(double y)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (listBox.ContainerFromIndex(i) is not Control container)
            {
                continue;
            }

            var top = container.TranslatePoint(new Point(0, 0), CoordinateRoot)?.Y ?? i * 40;
            var height = container.Bounds.Height;
            if (height <= 0)
            {
                height = 40;
            }

            if (y < top + (height / 2))
            {
                return i;
            }
        }

        return items.Count;
    }

    private int IndexOfItem(T item)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (ReferenceEquals(items[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    private sealed class ReorderHookTag;
}

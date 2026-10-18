using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Keirohi_App.Helpers;

/// <summary>
/// ListView のドラッグ並べ替え。見た目だけ動いて ItemsSource が更新されないケースを補う。
/// </summary>
public sealed class ListViewDragReorderController<T> where T : class
{
    private readonly ListView listView;
    private readonly IList<T> items;
    private readonly Action<IReadOnlyList<T>> applyOrder;
    private readonly Func<bool>? tryResolveExternalOrderMismatch;

    private T? draggingItem;
    private double lastDragY = double.NaN;
    private bool reorderCommitted;

    public ListViewDragReorderController(
        ListView listView,
        IList<T> items,
        Action<IReadOnlyList<T>> applyOrder,
        Func<bool>? tryResolveExternalOrderMismatch = null)
    {
        this.listView = listView;
        this.items = items;
        this.applyOrder = applyOrder;
        this.tryResolveExternalOrderMismatch = tryResolveExternalOrderMismatch;
    }

    public void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue || args.ItemContainer is not ListViewItem item)
        {
            return;
        }

        item.AllowDrop = true;
        item.DragOver -= OnItemDragOver;
        item.DragOver += OnItemDragOver;
        item.Drop -= OnItemDrop;
        item.Drop += OnItemDrop;
    }

    public void OnDragItemsStarting(DragItemsStartingEventArgs e)
    {
        reorderCommitted = false;
        lastDragY = double.NaN;
        draggingItem = e.Items.OfType<T>().FirstOrDefault();
        e.Data.RequestedOperation = DataPackageOperation.Move;
    }

    public void OnDragOver(DragEventArgs e) => RememberDragY(e);

    public void OnDrop(DragEventArgs e)
    {
        RememberDragY(e);
        TryCommit();
        e.Handled = true;
    }

    public void OnDragItemsCompleted(DragItemsCompletedEventArgs e)
    {
        draggingItem ??= e.Items.OfType<T>().FirstOrDefault();
        if (TryCommit())
        {
            draggingItem = null;
            return;
        }

        var item = draggingItem;
        var y = lastDragY;
        _ = listView.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            draggingItem ??= item;
            if (double.IsNaN(lastDragY))
            {
                lastDragY = y;
            }

            TryCommit();
            draggingItem = null;
        });
    }

    private void OnItemDragOver(object sender, DragEventArgs e) => RememberDragY(e);

    private void OnItemDrop(object sender, DragEventArgs e)
    {
        RememberDragY(e);
        TryCommit();
        e.Handled = true;
    }

    private void RememberDragY(DragEventArgs e)
    {
        lastDragY = e.GetPosition(listView).Y;
        e.AcceptedOperation = DataPackageOperation.Move;
    }

    private bool TryCommit()
    {
        if (reorderCommitted)
        {
            return true;
        }

        var visualOrder = OrderByVisualY();
        if (visualOrder.Count == items.Count && !SameOrder(visualOrder, items.ToList()))
        {
            applyOrder(visualOrder);
            reorderCommitted = true;
            return true;
        }

        if (tryResolveExternalOrderMismatch?.Invoke() == true)
        {
            reorderCommitted = true;
            return true;
        }

        if (draggingItem is null || double.IsNaN(lastDragY))
        {
            return false;
        }

        var from = IndexOfItem(draggingItem);
        if (from < 0)
        {
            return false;
        }

        var insertAt = InsertIndexFromPointerY(lastDragY);
        if (insertAt > from)
        {
            insertAt--;
        }

        if (insertAt == from || insertAt < 0 || insertAt >= items.Count)
        {
            return false;
        }

        var ordered = items.ToList();
        var row = ordered[from];
        ordered.RemoveAt(from);
        ordered.Insert(insertAt, row);
        applyOrder(ordered);
        reorderCommitted = true;
        return true;
    }

    private List<T> OrderByVisualY()
    {
        var placed = new List<(double Y, int Index, T Item)>();
        for (var i = 0; i < items.Count; i++)
        {
            var y = (double)i;
            if (listView.ContainerFromIndex(i) is FrameworkElement container)
            {
                y = container.TransformToVisual(listView).TransformPoint(default).Y;
            }

            placed.Add((y, i, items[i]));
        }

        return placed
            .OrderBy(p => p.Y)
            .ThenBy(p => p.Index)
            .Select(p => p.Item)
            .ToList();
    }

    private int InsertIndexFromPointerY(double y)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (listView.ContainerFromIndex(i) is not FrameworkElement container)
            {
                continue;
            }

            var top = container.TransformToVisual(listView).TransformPoint(default).Y;
            if (y < top + (container.ActualHeight / 2))
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

    private static bool SameOrder(IReadOnlyList<T> left, IReadOnlyList<T> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (!ReferenceEquals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }
}

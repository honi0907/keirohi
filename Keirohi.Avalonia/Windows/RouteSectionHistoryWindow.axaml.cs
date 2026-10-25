using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Keirohi_App.Models;

namespace Keirohi.Avalonia.Windows;

public partial class RouteSectionHistoryWindow : Window
{
    private readonly IReadOnlyList<RouteSectionHistoryEntry> entries;
    private List<RouteSectionHistoryEntry> filtered = [];

    public RouteSectionHistoryEntry? SelectedEntry { get; private set; }

    /// <summary>デザイナ / AVLN3001 用。</summary>
    public RouteSectionHistoryWindow()
    {
        InitializeComponent();
        entries = [];
    }

    public RouteSectionHistoryWindow(IReadOnlyList<RouteSectionHistoryEntry> entries)
    {
        InitializeComponent();
        this.entries = entries;
        RefreshFilter();
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e) => RefreshFilter();

    private void RefreshFilter()
    {
        var query = SearchBox.Text?.Trim() ?? "";
        filtered = string.IsNullOrEmpty(query)
            ? entries.ToList()
            : entries.Where(e => MatchesQuery(e, query)).ToList();

        HistoryList.ItemsSource = filtered.Select(e => e.DisplayLine).ToList();
        if (filtered.Count > 0)
        {
            HistoryList.SelectedIndex = 0;
        }
        else
        {
            HistoryList.SelectedIndex = -1;
        }

        FilterStatusText.Text = string.IsNullOrEmpty(query)
            ? $"{entries.Count} 件"
            : $"{filtered.Count} 件 / 全 {entries.Count} 件";
    }

    private static bool MatchesQuery(RouteSectionHistoryEntry entry, string query)
    {
        var tokens = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return true;
        }

        var haystack = string.Join(
            '\u0001',
            entry.DisplayLine,
            entry.Departure,
            entry.Arrival,
            entry.Amount.ToString());

        foreach (var token in tokens)
        {
            if (!haystack.Contains(token, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private void Apply_Click(object? sender, RoutedEventArgs e) => TryApplySelection();

    private void HistoryList_DoubleTapped(object? sender, TappedEventArgs e) => TryApplySelection();

    private void TryApplySelection()
    {
        var index = HistoryList.SelectedIndex;
        if (index < 0 || index >= filtered.Count)
        {
            return;
        }

        SelectedEntry = filtered[index];
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}

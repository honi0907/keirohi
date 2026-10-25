using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Keirohi.Avalonia.Helpers;
using Keirohi.Avalonia.Models;

namespace Keirohi.Avalonia.Windows;

public partial class MobileSuicaPdfImportConfirmWindow : Window
{
    private readonly List<MobileSuicaPdfImportRowItem> rows = [];

    public IReadOnlyList<TripEntry> SelectedEntries { get; private set; } = [];

    public MobileSuicaPdfImportConfirmWindow()
    {
        InitializeComponent();
    }

    public MobileSuicaPdfImportConfirmWindow(IReadOnlyList<TripEntry> entries, int skippedLines)
    {
        InitializeComponent();
        SummaryText.Text =
            $"PDF から {entries.Count} 件（スキップ {skippedLines} 件）を読み取りました。チェックした行だけ現在のセットに追加します。";

        for (var i = 0; i < entries.Count; i++)
        {
            rows.Add(new MobileSuicaPdfImportRowItem(
                entries[i],
                MobileSuicaPdfImportPlanner.FormatPreviewLine(entries[i], i)));
        }

        PreviewItems.ItemsSource = rows;
        WireSelectionSummaryRefresh();
        RefreshSelectionSummary();
    }

    private void WireSelectionSummaryRefresh()
    {
        foreach (var row in rows)
        {
            row.PropertyChanged += Row_PropertyChanged;
        }
    }

    private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MobileSuicaPdfImportRowItem.IsChecked))
        {
            RefreshSelectionSummary();
        }
    }

    private void RefreshSelectionSummary()
    {
        var checkedCount = rows.Count(r => r.IsChecked);
        SelectionSummaryText.Text =
            MobileSuicaPdfImportPlanner.FormatImportSelectionSummary(checkedCount, rows.Count);
    }

    private async void Import_Click(object? sender, RoutedEventArgs e)
    {
        var selected = rows.Where(r => r.IsChecked).Select(r => r.Entry).ToList();
        if (selected.Count == 0)
        {
            await DialogService.ShowOkAsync("PDF 取込", "1件以上チェックしてください。", this);
            return;
        }

        SelectedEntries = selected;
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}

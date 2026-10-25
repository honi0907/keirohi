using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Keirohi.Avalonia.Helpers;
using Keirohi.Avalonia.Models;

namespace Keirohi.Avalonia.Windows;

public partial class MobileSuicaPdfSplitWindow : Window
{
    private readonly IReadOnlyList<TripEntry> entries;
    private readonly MobileSuicaPdfSplitPlan plan;
    private readonly List<MobileSuicaPdfImportRowItem> rows = [];

    public IReadOnlyList<TripEntry> SelectedEntries { get; private set; } = [];

    public int? FirstBatchCount { get; private set; }

    /// <summary>デザイナ / AVLN3001 用。実行時は引数付きコンストラクタを使う。</summary>
    public MobileSuicaPdfSplitWindow()
    {
        InitializeComponent();
        entries = [];
        plan = new MobileSuicaPdfSplitPlan(0, 0, 0, UsesCurrentSet: true, RoomInCurrentSet: 0);
    }

    public MobileSuicaPdfSplitWindow(
        IReadOnlyList<TripEntry> entries,
        MobileSuicaPdfSplitPlan plan,
        int skippedLines)
    {
        InitializeComponent();
        this.entries = entries;
        this.plan = plan;

        for (var i = 0; i < entries.Count; i++)
        {
            rows.Add(new MobileSuicaPdfImportRowItem(
                entries[i],
                MobileSuicaPdfImportPlanner.FormatPreviewLine(entries[i], i)));
        }

        PreviewItems.ItemsSource = rows;
        WireSelectionSummaryRefresh();

        SplitSlider.Minimum = plan.MinFirstBatchCount;
        SplitSlider.Maximum = plan.MaxFirstBatchCount;
        SplitSlider.Value = plan.DefaultFirstBatchCount;
        SplitSlider.ValueChanged += (_, _) => UpdateSummary(skippedLines);
        UpdateSummary(skippedLines);
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
            UpdateSummary();
        }
    }

    private int skippedLines;

    private void UpdateSummary(int skippedLines)
    {
        this.skippedLines = skippedLines;
        RefreshSelectionSummary();
        UpdateSplitSummary();
    }

    private void UpdateSummary() => UpdateSummary(skippedLines);

    private void RefreshSelectionSummary()
    {
        var checkedCount = rows.Count(r => r.IsChecked);
        SelectionSummaryText.Text =
            MobileSuicaPdfImportPlanner.FormatImportSelectionSummary(checkedCount, rows.Count);
    }

    private void UpdateSplitSummary()
    {
        var first = (int)SplitSlider.Value;
        var second = entries.Count - first;
        if (plan.UsesCurrentSet)
        {
            SplitHintText.Text =
                $"先頭 {first} 件 → 現在のセット（空き {plan.RoomInCurrentSet} 件）、残り {second} 件 → 新しいセット";
            SummaryText.Text =
                $"取込 {entries.Count} 件（スキップ {skippedLines} 件）。現在 {AppHost.State.Entries.Count} 件 + 先頭 {first} 件 = {AppHost.State.Entries.Count + first} 件";
        }
        else
        {
            SplitHintText.Text = $"先頭 {first} 件 → 新しいセット1、残り {second} 件 → 新しいセット2（現在のセットは変更しません）";
            SummaryText.Text = $"取込 {entries.Count} 件（スキップ {skippedLines} 件）。現在のセットは満杯のため新規2セットに分けます。";
        }
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
        var slider = (int)SplitSlider.Value;
        FirstBatchCount = Math.Clamp(slider, 0, selected.Count);
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Keirohi_App.Models;
using Keirohi.Avalonia.Helpers;

namespace Keirohi.Avalonia.Windows;

public partial class DestinationBatchWindow : Window
{
    private readonly IReadOnlyList<DestinationDateSection> sections;
    private readonly Dictionary<DestinationLegDraft, TextBox> destinationBoxes = new();
    private readonly List<SectionUi> sectionUis = [];
    private bool suppressCascade;

    /// <summary>デザイナ / AVLN3001 用。</summary>
    public DestinationBatchWindow()
    {
        InitializeComponent();
        sections = [];
    }

    public DestinationBatchWindow(IReadOnlyList<DestinationDateSection> sections)
    {
        InitializeComponent();
        this.sections = sections;
        BuildUi();
        RefreshHint();
        ApplyMissingOnlyFilter();
    }

    private void ShowMissingOnlyCheck_Changed(object? sender, RoutedEventArgs e)
    {
        ApplyMissingOnlyFilter();
        RefreshHint();
    }

    private void BuildUi()
    {
        foreach (var section in sections)
        {
            var dateBlock = new TextBlock
            {
                FontWeight = FontWeight.SemiBold,
                Text = section.Date,
                Margin = new Thickness(0, 4, 0, 0),
            };
            SectionHost.Children.Add(dateBlock);

            var sectionUi = new SectionUi(section, dateBlock);
            sectionUis.Add(sectionUi);

            foreach (var leg in section.Legs)
            {
                var mode = leg.Mode == TransportMode.Bus ? "バス" : "電車";
                var route = leg.Mode == TransportMode.Bus
                    ? leg.Departure
                    : $"{leg.Departure} → {leg.Arrival}";

                var grid = new Grid
                {
                    ColumnDefinitions =
                    [
                        new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                        new ColumnDefinition(GridLength.Auto),
                    ],
                    Margin = new Thickness(0, 0, 0, 6),
                    ColumnSpacing = 8,
                };

                grid.Children.Add(new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = $"{mode}  {route}",
                    TextWrapping = TextWrapping.Wrap,
                });

                var countBlock = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0.75,
                    Text = $"{leg.Count} 件",
                };
                Grid.SetColumn(countBlock, 1);
                grid.Children.Add(countBlock);

                var box = new TextBox
                {
                    Watermark = "例: 自宅→センター",
                    Text = leg.SuggestedDestination,
                    MinHeight = 36,
                    Margin = new Thickness(0, 0, 0, 4),
                };
                var rowIndex = sectionUi.Rows.Count;
                box.TextChanged += (_, _) =>
                {
                    if (suppressCascade)
                    {
                        return;
                    }

                    CascadeFillBelow(sectionUi, rowIndex, box.Text ?? "");
                };

                destinationBoxes[leg] = box;
                sectionUi.Rows.Add(new SectionRow(leg, grid, box));

                SectionHost.Children.Add(grid);
                SectionHost.Children.Add(box);
            }
        }
    }

    private void ApplyMissingOnlyFilter()
    {
        var missingOnly = ShowMissingOnlyCheck.IsChecked == true;
        foreach (var sectionUi in sectionUis)
        {
            var anyVisible = false;
            foreach (var row in sectionUi.Rows)
            {
                var show = !missingOnly || row.Leg.IncludesMissingDestination;
                row.RouteGrid.IsVisible = show;
                row.Box.IsVisible = show;
                if (show)
                {
                    anyVisible = true;
                }
            }

            sectionUi.DateBlock.IsVisible = anyVisible;
        }
    }

    private void RefreshHint()
    {
        var entryCount = sections.Sum(s => s.Legs.Sum(l => l.Count));
        var legCount = sections.Sum(s => s.Legs.Count);
        var missingLegCount = sections.Sum(s => s.Legs.Count(l => l.IncludesMissingDestination));
        if (ShowMissingOnlyCheck.IsChecked == true)
        {
            HintText.Text = $"表示 {missingLegCount} 区間（未入力） / 全 {entryCount} 件・{legCount} 区間";
            return;
        }

        HintText.Text = missingLegCount > 0
            ? $"対象 {entryCount} 件 / {legCount} 区間（未入力 {missingLegCount} 区間）"
            : $"対象 {entryCount} 件 / {legCount} 区間";
    }

    private void CascadeFillBelow(SectionUi sectionUi, int fromRowIndex, string text)
    {
        suppressCascade = true;
        try
        {
            for (var i = fromRowIndex + 1; i < sectionUi.Rows.Count; i++)
            {
                sectionUi.Rows[i].Box.Text = text;
            }
        }
        finally
        {
            suppressCascade = false;
        }
    }

    private void Apply_Click(object? sender, RoutedEventArgs e)
    {
        var map = new Dictionary<string, string>();
        foreach (var (leg, box) in destinationBoxes)
        {
            var dest = box.Text?.Trim() ?? "";
            if (dest.Length == 0)
            {
                continue;
            }

            foreach (var id in leg.EntryIds)
            {
                map[id] = dest;
            }
        }

        AppHost.State.UpdateEntryDestinations(map);
        Close(true);
    }

    private void Later_Click(object? sender, RoutedEventArgs e) => Close(false);

    private sealed class SectionUi(DestinationDateSection section, TextBlock dateBlock)
    {
        public DestinationDateSection Section { get; } = section;

        public TextBlock DateBlock { get; } = dateBlock;

        public List<SectionRow> Rows { get; } = [];
    }

    private sealed record SectionRow(DestinationLegDraft Leg, Grid RouteGrid, TextBox Box);
}

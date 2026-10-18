using Keirohi_App.Models;
using Keirohi_App.Services;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Keirohi_App.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Text;

namespace Keirohi_App.Controls;

public sealed partial class SuicaDocumentPreview : UserControl
{
    private const int AmountColumn = 10;
    private const int AmountColumnSpan = 2;

    private static readonly FontFamily SuicaFont = new(SuicaLayoutConstants.FontName);

    private Grid? layoutTarget;

    private Grid ActiveGrid => layoutTarget ?? SheetGrid;

    private int contentVersion;
    private int cachedPrintVersion = -1;
    private byte[]? cachedPng;
    private Task<byte[]>? printCacheTask;

    public SuicaDocumentPreview()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
        App.State.Changed += (_, _) => Rebuild();
    }

    public UIElement PrintRoot => SheetBorder;

    public (int WidthPx, int HeightPx) GetPrintPixelSize()
    {
        SheetBorder.UpdateLayout();
        SheetGrid.UpdateLayout();

        var widthDip = SheetGrid.ActualWidth > 1
            ? SheetGrid.ActualWidth
            : SuicaLayoutConstants.SheetWidthDip;
        var heightDip = SheetGrid.ActualHeight > 1
            ? SheetGrid.ActualHeight
            : SuicaLayoutConstants.EstimateSheetHeightDip(
                DateFormatHelper.EntriesForSuicaSheet(App.State.Entries).Count);

        var widthPx = (int)Math.Max(1, Math.Ceiling(widthDip));
        var heightPx = (int)Math.Max(1, Math.Ceiling(heightDip));
        return (widthPx, heightPx);
    }

    public Task<byte[]> WarmPrintCacheAsync()
    {
        printCacheTask ??= BuildPrintCacheAsync();
        return printCacheTask;
    }

    public async Task<byte[]> GetPrintPngAsync()
    {
        if (cachedPng is not null && cachedPrintVersion == contentVersion)
        {
            return cachedPng;
        }

        return await WarmPrintCacheAsync();
    }

    private void InvalidatePrintCache()
    {
        contentVersion++;
        cachedPrintVersion = -1;
        cachedPng = null;
        printCacheTask = null;
    }

    private void QueuePrintCacheWarmup()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => _ = WarmPrintCacheAsync());
    }

    private async Task<byte[]> BuildPrintCacheAsync()
    {
        await Task.Yield();

        while (true)
        {
            var version = contentVersion;
            var page = CreatePrintPageElement();
            PrintHost.Child = page;
            var scale = XamlRoot?.RasterizationScale ?? 1d;
            page.Measure(new Size(SuicaLayoutConstants.SheetWidthDip, double.PositiveInfinity));
            var widthDip = Math.Max(1, page.DesiredSize.Width);
            var heightDip = Math.Max(1, page.DesiredSize.Height);
            page.Arrange(new Rect(0, 0, widthDip, heightDip));
            page.UpdateLayout();

            var rendered = new RenderTargetBitmap();
            await rendered.RenderAsync(
                page,
                (int)Math.Ceiling(widthDip * scale),
                (int)Math.Ceiling(heightDip * scale));
            PrintHost.Child = null;
            var png = await PrintBitmapHelper.ToPngBytesAsync(rendered);

            if (version != contentVersion)
            {
                continue;
            }

            cachedPng = png;
            cachedPrintVersion = version;
            printCacheTask = null;
            return png;
        }
    }

    /// <summary>画面上の要素とは別ツリー（フォールバック用）。</summary>
    public Border CreatePrintPageElement()
    {
        var grid = new Grid();
        var border = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Black),
            BorderThickness = new Thickness(1),
            Child = grid,
        };

        layoutTarget = grid;
        try
        {
            PopulateSheet();
        }
        finally
        {
            layoutTarget = null;
        }

        grid.Width = SuicaLayoutConstants.SheetWidthDip;
        border.Width = SuicaLayoutConstants.SheetWidthDip;
        return border;
    }

    public void Rebuild()
    {
        layoutTarget = null;
        SheetGrid.Children.Clear();
        SheetGrid.RowDefinitions.Clear();
        SheetGrid.ColumnDefinitions.Clear();
        PopulateSheet();
        SheetGrid.Width = SuicaLayoutConstants.SheetWidthDip;
        FitBox.MaxWidth = SuicaLayoutConstants.SheetWidthDip;
        InvalidatePrintCache();
        QueuePrintCacheWarmup();
    }

    private void PopulateSheet()
    {
        var entries = App.State.Entries;
        var settings = App.State.Settings;
        var export = DateFormatHelper.EntriesForSuicaSheet(entries);
        var padded = PadEntries(export);

        ActiveGrid.Children.Clear();
        ActiveGrid.RowDefinitions.Clear();
        ActiveGrid.ColumnDefinitions.Clear();

        foreach (var w in SuicaLayoutConstants.ColumnWidths)
        {
            ActiveGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(w, GridUnitType.Star),
            });
        }

        var rowIndex = 0;
        AddRow(SuicaLayoutConstants.RowTitle);
        PlaceCell(
            rowIndex,
            0,
            12,
            "モバイル　Ｓｕｉｃａ　残高ご利用明細",
            SuicaLayoutConstants.TitleFontPt,
            true,
            HorizontalAlignment.Center,
            topInsetDip: SuicaLayoutConstants.TitleTopInsetDip,
            scaleY: SuicaLayoutConstants.TitleScaleY);
        rowIndex++;

        AddRow(SuicaLayoutConstants.RowCard);
        PlaceCardLabelCell(rowIndex, settings.SuicaCardLabel);
        rowIndex++;

        AddRow(SuicaLayoutConstants.RowSpacer);
        rowIndex++;

        AddRow(SuicaLayoutConstants.RowHistory);
        PlaceCell(
            rowIndex,
            0,
            12,
            $"利用履歴　（{export.Count}件）",
            SuicaLayoutConstants.TableFontPt,
            true,
            HorizontalAlignment.Center,
            trim: false,
            shiftLeftDip: SuicaLayoutConstants.HistoryLeftShiftDip);
        rowIndex++;

        AddRow(SuicaLayoutConstants.RowTableHeader);
        PlaceMonthDayCell(rowIndex, 0, "月");
        PlaceMonthDayCell(rowIndex, 1, "日");
        PlaceTripTypeCell(rowIndex, 3, "種別");
        PlaceTripStationCell(rowIndex, 5, "利用駅");
        PlaceTripTypeCell(rowIndex, 7, "種別");
        PlaceTripStationCell(rowIndex, 8, "利用駅");
        PlaceAmountCell(rowIndex, "入金・利用額");
        rowIndex++;

        for (var i = 0; i < padded.Count; i++)
        {
            var isLast = i == padded.Count - 1;
            AddRow(isLast ? SuicaLayoutConstants.RowDataLast : SuicaLayoutConstants.RowData);
            var entry = padded[i];
            if (entry is not null)
            {
                var (month, day) = DateFormatHelper.FormatSuicaMonthDay(entry.Date);
                PlaceMonthDayCell(rowIndex, 0, month);
                PlaceMonthDayCell(rowIndex, 1, day);

                if (entry.Mode == TransportMode.Bus)
                {
                    PlaceTripTypeCell(rowIndex, 3, "ﾊﾞｽ等");
                    PlaceTripStationCell(rowIndex, 5, entry.Departure);
                    PlaceTripStationCell(rowIndex, 8, entry.Arrival);
                }
                else
                {
                    PlaceTripTypeCell(rowIndex, 3, "入");
                    PlaceTripStationCell(rowIndex, 5, entry.Departure);
                    PlaceTripTypeCell(rowIndex, 7, "出");
                    PlaceTripStationCell(rowIndex, 8, entry.Arrival);
                }

                PlaceAmountCell(rowIndex, entry.Amount.ToString());
            }

            rowIndex++;
        }

        var statementDate = DateFormatHelper.FormatSuicaStatementDate(settings.StatementDate);
        AddRow(SuicaLayoutConstants.RowFooter);
        PlaceCell(rowIndex, 0, 12, $"　　{statementDate}", SuicaLayoutConstants.TableFontPt, true, HorizontalAlignment.Left);
        rowIndex++;

        AddRow(SuicaLayoutConstants.RowFooter);
        PlaceCell(rowIndex, 0, 12, "　　ご利用ありがとうございます。", SuicaLayoutConstants.TableFontPt, true, HorizontalAlignment.Left);
        rowIndex++;

        AddRow(SuicaLayoutConstants.RowFooterNotice);
        PlaceCell(rowIndex, 0, 12, "　　システムの都合上、最新のご利用明細が反映されていない場合があります。", SuicaLayoutConstants.TableFontPt, true, HorizontalAlignment.Left);
        rowIndex++;

        AddRow(SuicaLayoutConstants.RowFooterBottom);
        PlaceCell(rowIndex, 0, 2, " (1/1)", SuicaLayoutConstants.TableFontPt, true, HorizontalAlignment.Left);
        PlaceFooterCompanyCell(rowIndex, "東日本旅客鉄道株式会社");
    }

    private void AddRow(double heightPt)
    {
        ActiveGrid.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(SuicaLayoutConstants.PtToDip(heightPt)),
        });
    }

    private void PlaceFooterCompanyCell(int row, string text) =>
        PlaceCell(
            row,
            9,
            3,
            text,
            SuicaLayoutConstants.TableFontPt,
            true,
            HorizontalAlignment.Left,
            shiftLeftDip: SuicaLayoutConstants.FooterCompanyLeftShiftDip,
            pinToBottom: true);

    private void PlaceCardLabelCell(int row, string text) =>
        PlaceCell(
            row,
            7,
            5,
            text,
            SuicaLayoutConstants.TableFontPt,
            false,
            HorizontalAlignment.Right,
            rightInsetDip: SuicaLayoutConstants.CardLabelLeftShiftDip);

    private void PlaceAmountCell(int row, string text) =>
        PlaceCell(
            row,
            AmountColumn,
            AmountColumnSpan,
            text,
            SuicaLayoutConstants.TableFontPt,
            true,
            HorizontalAlignment.Right,
            rightInsetDip: SuicaLayoutConstants.AmountRightInsetDip);

    private void PlaceMonthDayCell(int row, int column, string text) =>
        PlaceCell(
            row,
            column,
            1,
            text,
            SuicaLayoutConstants.TableFontPt,
            true,
            HorizontalAlignment.Center,
            leftInsetDip: SuicaLayoutConstants.MonthDayLeftInsetDip);

    private void PlaceTripTypeCell(int row, int column, string text) =>
        PlaceCell(
            row,
            column,
            1,
            text,
            SuicaLayoutConstants.TableFontPt,
            true,
            HorizontalAlignment.Left);

    private void PlaceTripStationCell(int row, int column, string text) =>
        PlaceCell(
            row,
            column,
            1,
            text,
            SuicaLayoutConstants.TableFontPt,
            true,
            HorizontalAlignment.Left);

    private void PlaceCell(
        int row,
        int column,
        int columnSpan,
        string text,
        double fontPt,
        bool bold,
        HorizontalAlignment align,
        bool trim = true,
        double topInsetDip = 0,
        double leftInsetDip = 0,
        double rightInsetDip = 0,
        double shiftLeftDip = 0,
        double scaleY = 1,
        bool pinToBottom = false)
    {
        if (shiftLeftDip > 0)
        {
            if (align == HorizontalAlignment.Right)
            {
                rightInsetDip += shiftLeftDip;
            }
        }

        var block = new TextBlock
        {
            Text = text,
            FontFamily = SuicaFont,
            FontSize = SuicaLayoutConstants.PtToDip(fontPt),
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            TextAlignment = ToTextAlignment(align),
            TextTrimming = trim ? TextTrimming.CharacterEllipsis : TextTrimming.None,
            TextWrapping = TextWrapping.NoWrap,
            Padding = new Thickness(1 + leftInsetDip, topInsetDip, 1 + rightInsetDip, 0),
        };

        ApplyRenderTransform(block, align, shiftLeftDip, scaleY);

        if (pinToBottom)
        {
            block.VerticalAlignment = VerticalAlignment.Bottom;
            block.Padding = new Thickness(
                block.Padding.Left,
                block.Padding.Top,
                block.Padding.Right,
                0);
        }
        else if (topInsetDip > 0)
        {
            block.VerticalAlignment = VerticalAlignment.Top;
        }

        Grid.SetRow(block, row);
        Grid.SetColumn(block, column);
        Grid.SetColumnSpan(block, columnSpan);
        ActiveGrid.Children.Add(block);
    }

    private static void ApplyRenderTransform(
        TextBlock block,
        HorizontalAlignment align,
        double shiftLeftDip,
        double scaleY)
    {
        var group = new TransformGroup();
        if (shiftLeftDip > 0 && align != HorizontalAlignment.Right)
        {
            group.Children.Add(new TranslateTransform { X = -shiftLeftDip });
        }

        if (Math.Abs(scaleY - 1) > 0.001)
        {
            block.RenderTransformOrigin = new Point(0.5, 0);
            group.Children.Add(new ScaleTransform { ScaleX = 1, ScaleY = scaleY });
        }

        if (group.Children.Count > 0)
        {
            block.RenderTransform = group;
        }
    }

    private static TextAlignment ToTextAlignment(HorizontalAlignment align) =>
        align switch
        {
            HorizontalAlignment.Right => TextAlignment.Right,
            HorizontalAlignment.Center => TextAlignment.Center,
            HorizontalAlignment.Left => TextAlignment.Left,
            HorizontalAlignment.Stretch => TextAlignment.Center,
            _ => throw new InvalidOperationException($"Unsupported alignment: {align}"),
        };

    private static List<TripEntry?> PadEntries(List<TripEntry> sorted)
    {
        var list = new List<TripEntry?>(AppStateService.MaxSuicaEntries);
        for (var i = 0; i < AppStateService.MaxSuicaEntries; i++)
        {
            list.Add(i < sorted.Count ? sorted[i] : null);
        }

        return list;
    }
}

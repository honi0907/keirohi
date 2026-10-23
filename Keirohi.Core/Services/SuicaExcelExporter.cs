using ClosedXML.Excel;
using Keirohi_App.Models;

namespace Keirohi_App.Services;

public static class SuicaExcelExporter
{
    private const string FontName = "ＭＳ ゴシック";
    private const double FontSizePt = 9;
    private const double TitleFontSizePt = 12;
    private const int HeaderRow = 8;
    private const int FirstDataRow = 9;
    private const int FooterStartRow = 63;
    private const int MaxRows = 54;
    private const int FrameTop = 4;
    private const int FrameBottom = FooterStartRow + 3;
    private const int FrameLeft = 3;
    private const int FrameRight = 14;

    private const string Title = "モバイル　Ｓｕｉｃａ　残高ご利用明細";
    private const string FooterThanks = "ご利用ありがとうございます。";
    private const string FooterNotice = "システムの都合上、最新のご利用明細が反映されていない場合があります。";
    private const string FooterCompany = "東日本旅客鉄道株式会社";

    public static async Task SaveAsync(IEnumerable<TripEntry> entries, AppSettings settings, string filePath)
    {
        var sorted = DateFormatHelper.EntriesForSuicaSheet(entries);
        var period = DateFormatHelper.FormatPeriodLabel(sorted);
        var padded = PadEntries(sorted);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(period);

        sheet.Column(1).Width = 4;
        sheet.Column(2).Width = 4;
        sheet.Column(3).Width = 4;
        sheet.Column(4).Width = 4;
        sheet.Column(5).Width = 2;
        sheet.Column(6).Width = 6;
        sheet.Column(7).Width = 2;
        sheet.Column(8).Width = 14;
        sheet.Column(9).Width = 2;
        sheet.Column(10).Width = 6;
        sheet.Column(11).Width = 14;
        sheet.Column(12).Width = 2;
        sheet.Column(13).Width = 10;
        sheet.Column(14).Width = 2;

        StyleCell(sheet.Cell("E4"), TitleFontSizePt, true);
        sheet.Range("E4:K4").Merge();
        sheet.Cell("E4").Value = Title;
        sheet.Cell("E4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Cell("E4").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        sheet.Range("J5:K5").Merge();
        StyleCell(sheet.Cell("J5"), FontSizePt, false);
        sheet.Cell("J5").Value = $"     {settings.SuicaCardLabel}";
        sheet.Cell("J5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        sheet.Range("E7:K7").Merge();
        StyleCell(sheet.Cell("E7"), FontSizePt, true);
        sheet.Cell("E7").Value = $"利用履歴　（{sorted.Count}件）";
        sheet.Cell("E7").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var headers = new (int Col, string Label)[]
        {
            (3, "月"), (4, "日"), (6, "種別"), (8, "利用駅"), (10, "種別"), (11, "利用駅"), (13, "入金・利用額"),
        };
        foreach (var (col, label) in headers)
        {
            var cell = sheet.Cell(HeaderRow, col);
            cell.Value = label;
            StyleCell(cell, FontSizePt, true);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        for (var index = 0; index < MaxRows; index++)
        {
            WriteDataRow(sheet, FirstDataRow + index, index + 1, padded[index]);
        }

        StyleCell(sheet.Cell(FooterStartRow, 5), FontSizePt, true);
        sheet.Cell(FooterStartRow, 5).Value = $"　　{DateFormatHelper.FormatSuicaStatementDate(settings.StatementDate)}";
        sheet.Cell(FooterStartRow + 1, 5).Value = $"　　{FooterThanks}";
        sheet.Cell(FooterStartRow + 2, 5).Value = $"　　{FooterNotice}";
        sheet.Cell(FooterStartRow + 3, 2).Value = " (1/1)";
        sheet.Cell(FooterStartRow + 3, 11).Value = FooterCompany;
        for (var row = FooterStartRow; row <= FooterStartRow + 3; row++)
        {
            for (var col = 1; col <= 14; col++)
            {
                StyleCell(sheet.Cell(row, col), FontSizePt, true);
            }
        }

        ApplyOuterFrame(sheet);

        await Task.Run(() => workbook.SaveAs(filePath));
    }

    private static List<TripEntry?> PadEntries(List<TripEntry> sorted)
    {
        var list = new List<TripEntry?>(MaxRows);
        for (var i = 0; i < MaxRows; i++)
        {
            list.Add(i < sorted.Count ? sorted[i] : null);
        }
        return list;
    }

    private static void WriteDataRow(IXLWorksheet sheet, int row, int seq, TripEntry? entry)
    {
        sheet.Cell(row, 1).Value = seq;
        sheet.Cell(row, 2).Value = seq;
        for (var col = 1; col <= 14; col++)
        {
            StyleCell(sheet.Cell(row, col), FontSizePt, true);
            sheet.Cell(row, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        if (entry is null)
        {
            return;
        }

        var (month, day) = DateFormatHelper.FormatSuicaMonthDay(entry.Date);
        sheet.Cell(row, 3).Value = month;
        sheet.Cell(row, 4).Value = day;

        if (entry.Mode == TransportMode.Bus)
        {
            sheet.Cell(row, 6).Value = "ﾊﾞｽ等";
            sheet.Cell(row, 8).Value = entry.Departure;
            sheet.Cell(row, 11).Value = entry.Arrival;
        }
        else
        {
            sheet.Cell(row, 6).Value = "入";
            sheet.Cell(row, 8).Value = entry.Departure;
            sheet.Cell(row, 10).Value = "出";
            sheet.Cell(row, 11).Value = entry.Arrival;
        }

        var amountCell = sheet.Cell(row, 13);
        amountCell.Value = -Math.Abs(entry.Amount);
        amountCell.Style.NumberFormat.Format = "0";
        amountCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static void StyleCell(IXLCell cell, double sizePt, bool bold)
    {
        cell.Style.Font.FontName = FontName;
        cell.Style.Font.FontSize = sizePt;
        cell.Style.Font.Bold = bold;
    }

    private static void ApplyOuterFrame(IXLWorksheet sheet)
    {
        for (var row = FrameTop; row <= FrameBottom; row++)
        {
            for (var col = FrameLeft; col <= FrameRight; col++)
            {
                var cell = sheet.Cell(row, col);
                if (row == FrameTop)
                {
                    cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                }
                if (row == FrameBottom)
                {
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                }
                if (col == FrameLeft)
                {
                    cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                }
                if (col == FrameRight)
                {
                    cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                }
            }
        }
    }
}

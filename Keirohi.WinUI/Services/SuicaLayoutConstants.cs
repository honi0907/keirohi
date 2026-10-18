namespace Keirohi_App.Services;

/// <summary>0901~0919 シート計測値（Web 版 suicaLayout.ts と同等）。</summary>
public static class SuicaLayoutConstants
{
    public const string FontName = "ＭＳ ゴシック";
    public const double TableFontPt = 9;
    public const double TitleFontPt = 12;

    /// <summary>タイトル行の上余白（枠線との隙間、pt）。</summary>
    public const double TitleTopInsetPt = 2.5;

    public static double TitleTopInsetDip => PtToDip(TitleTopInsetPt);

    /// <summary>タイトル文字の縦方向スケール（1 = 等倍）。</summary>
    public const double TitleScaleY = 1.06;

    /// <summary>利用履歴行を左へずらす幅（9pt 全角約12文字分）。</summary>
    public const double HistoryLeftShiftChars = 12;

    public static double HistoryLeftShiftDip => PtToDip(TableFontPt * HistoryLeftShiftChars);

    /// <summary>フッター「東日本旅客鉄道株式会社」を左へずらす幅（9pt 約5文字）。</summary>
    public const double FooterCompanyLeftShiftChars = 5;

    public static double FooterCompanyLeftShiftDip => PtToDip(TableFontPt * FooterCompanyLeftShiftChars);

    /// <summary>入金・利用額列を右端から左へずらす幅（9pt 全角約3文字分）。</summary>
    public const double AmountRightInsetChars = 3;

    public static double AmountRightInsetDip => PtToDip(TableFontPt * AmountRightInsetChars);

    /// <summary>月・日列を左端から右へずらす幅（9pt 全角約1文字分）。</summary>
    public const double MonthDayLeftInsetChars = 1;

    public static double MonthDayLeftInsetDip => PtToDip(TableFontPt * MonthDayLeftInsetChars);

    /// <summary>種別・利用駅列を左へずらす幅（9pt 全角約1文字分）。</summary>
    public const double TripColumnLeftShiftChars = 1;

    public static double TripColumnLeftShiftDip => PtToDip(TableFontPt * TripColumnLeftShiftChars);

    /// <summary>Suica カード表示（明細右上）を左へずらす幅（9pt 全角約11文字分）。</summary>
    public const double CardLabelLeftShiftChars = 11;

    public static double CardLabelLeftShiftDip => PtToDip(TableFontPt * CardLabelLeftShiftChars);

    public static readonly double[] ColumnWidths =
    [
        4.88, 4, 3, 7.13, 1.13, 8.38, 2.13, 8.63, 8.38, 1.25, 12.5, 2.88,
    ];

    public const double RowTitle = 18.75;
    public const double RowCard = 8.5;
    public const double RowSpacer = 15;
    public const double RowHistory = 18.75;
    public const double RowTableHeader = 15;
    public const double RowData = 11.25;
    public const double RowDataLast = 12.75;
    public const double RowFooter = 12;
    public const double RowFooterNotice = 16.5;
    public const double RowFooterBottom = 14;

    public const double PageMarginPt = 70.8661417322835;
    public const double PrintableWidthPt = 595.32 - PageMarginPt * 2;

    public static double PtToDip(double pt) => pt * 96.0 / 72.0;

    public static double SheetWidthDip => PtToDip(PrintableWidthPt);

    public static double EstimateSheetHeightDip(int dataRowCount = AppStateService.MaxSuicaEntries)
    {
        var heightPt = RowTitle + RowCard + RowSpacer + RowHistory + RowTableHeader;
        if (dataRowCount > 0)
        {
            heightPt += (dataRowCount - 1) * RowData + RowDataLast;
        }

        heightPt += RowFooter * 2 + RowFooterNotice + RowFooterBottom;
        return PtToDip(heightPt);
    }
}

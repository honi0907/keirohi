namespace Keirohi_App.Models;

public sealed class AppSettings
{
    public string SuicaCardLabel { get; set; } = "JE*** **** **** 8841";
    public string StatementDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public string EkispertApiKey { get; set; } = "";

    /// <summary>アプリ外観（Avalonia）: System / Light / Dark。</summary>
    public string ThemePreference { get; set; } = "System";

    /// <summary>出発・到着入力時に記憶した運賃などを自動入力する。</summary>
    public bool AutoFillFareEnabled { get; set; } = true;

    /// <summary>明細プレビューに PNG 保存・印刷ボタンを表示する（オフで非表示）。</summary>
    public bool ShowSuicaPreviewPngAndPrint { get; set; }

    /// <summary>余白内に収まる大きさを 1 とした印刷倍率。</summary>
    public float PrintImageScale { get; set; } = 0.92f;

    public float PrintOffsetXmm { get; set; }

    public float PrintOffsetYmm { get; set; } = 11f;

    /// <summary>印刷位置デフォルト更新（1/0/0 時代）を一度だけ適用済みか。</summary>
    public bool PrintLayoutDefaultsV2Applied { get; set; }

    /// <summary>余白。1/100 インチ。15mm は 59。</summary>
    public int PrintMarginLeft { get; set; } = 59;

    public int PrintMarginRight { get; set; } = 59;

    public int PrintMarginTop { get; set; } = 59;

    public int PrintMarginBottom { get; set; } = 59;
}

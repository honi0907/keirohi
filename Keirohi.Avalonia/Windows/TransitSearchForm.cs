using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace Keirohi.Avalonia.Windows;

/// <summary>
/// WinUI TransitSearchWindow 相当（WebView2 ポップアップ）。
/// </summary>
internal sealed class TransitSearchForm : Form
{
    private readonly Uri searchUri;
    private bool webViewInitialized;

    public TransitSearchForm(Uri searchUri, string routeLabel)
    {
        this.searchUri = searchUri;
        Text = $"Yahoo!乗換案内 — {routeLabel}";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(960, 720);
        MinimumSize = new Size(640, 480);

        var routeLabelControl = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            Height = 40,
            Padding = new Padding(12, 10, 12, 0),
            Text = routeLabel,
        };

        var hintLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = Color.FromArgb(160, 0, 0, 0),
            MaximumSize = new Size(920, 0),
            Padding = new Padding(12, 0, 12, 8),
            Text = "表示された運賃を確認し、入力画面の「金額」に手で入力してください（APIキー不要）。",
        };

        var webView = new WebView2
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 8, 8),
        };

        var webHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 0, 8, 8),
        };
        webHost.Controls.Add(webView);

        Controls.Add(webHost);
        Controls.Add(hintLabel);
        Controls.Add(routeLabelControl);

        Shown += async (_, _) =>
        {
            if (webViewInitialized)
            {
                return;
            }

            webViewInitialized = true;
            await webView.EnsureCoreWebView2Async();
            webView.Source = searchUri;
        };
    }
}

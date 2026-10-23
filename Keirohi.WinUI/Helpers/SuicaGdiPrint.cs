using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;
using Keirohi_App.Services;

namespace Keirohi_App.Helpers;

public static class SuicaGdiPrint
{
    public static void Show(IntPtr owner, byte[] png) => ShowInternal(owner, png);

    /// <summary>保存ダイアログでセット名を提案して PDF 出力。キャンセル時 null。</summary>
    public static string? TrySavePdfWithDialog(IntPtr owner, byte[] png)
    {
        var baseName = SuicaExportNames.SuggestDocumentBaseName(App.State.ActiveEntrySet?.Name);
        using var dialog = new SaveFileDialog
        {
            Title = "PDF を保存",
            Filter = "PDF (*.pdf)|*.pdf",
            DefaultExt = "pdf",
            FileName = baseName + ".pdf",
            AddExtension = true,
        };
        if (dialog.ShowDialog(new HwndOwner(owner)) != DialogResult.OK)
        {
            return null;
        }

        PrintPngToPdfFile(png, dialog.FileName, ReadPrintLayoutFromSettings());
        return dialog.FileName;
    }

    private static void ShowInternal(IntPtr owner, byte[] png)
    {
        using var stream = new MemoryStream(png);
        using var decoded = Image.FromStream(stream);
        using var image = new Bitmap(decoded);
        EnsurePrintResolution(image);
        var initialLayout = ReadPrintLayoutFromSettings();
        var imageScale = initialLayout.ImageScale;
        var offsetXmm = initialLayout.OffsetXmm;
        var offsetYmm = initialLayout.OffsetYmm;
        var documentName = SuicaExportNames.SuggestDocumentBaseName(App.State.ActiveEntrySet?.Name);
        using var document = new PrintDocument();
        document.DocumentName = documentName;
        PreferA4(document, initialLayout.Margins);
        document.PrintPage += (_, e) => DrawPage(e, image, imageScale, offsetXmm, offsetYmm);
        void PersistLayout() => Remember(document, imageScale, offsetXmm, offsetYmm);

        using var form = new Form
        {
            Text = "印刷位置の確認",
            StartPosition = FormStartPosition.CenterScreen,
            Width = 860,
            Height = 920,
        };
        var preview = new PrintPreviewControl
        {
            Document = document,
            Dock = DockStyle.Fill,
            AutoZoom = true,
            UseAntiAlias = false,
        };
        var marginButton = new Button
        {
            Text = "余白の設定…",
            AutoSize = true,
            Margin = new Padding(8, 8, 4, 8),
        };
        var printButton = new Button
        {
            Text = "この配置で印刷…",
            AutoSize = true,
            Margin = new Padding(4, 8, 4, 8),
        };
        var pdfButton = new Button
        {
            Text = "PDFで保存…",
            AutoSize = true,
            Margin = new Padding(4, 8, 4, 8),
        };
        var closeButton = new Button
        {
            Text = "閉じる",
            AutoSize = true,
            Margin = new Padding(4, 8, 8, 8),
            DialogResult = DialogResult.Cancel,
        };
        var shrinkButton = new Button
        {
            Text = "画像を縮小",
            AutoSize = true,
            Margin = new Padding(12, 8, 4, 8),
        };
        var scaleLabel = new Label
        {
            Text = "100%",
            AutoSize = false,
            Width = 52,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 12, 0, 8),
        };
        var enlargeButton = new Button
        {
            Text = "画像を拡大",
            AutoSize = true,
            Margin = new Padding(4, 8, 4, 8),
        };
        var resetScaleButton = new Button
        {
            Text = "100%",
            AutoSize = true,
            Margin = new Padding(4, 8, 8, 8),
        };
        void SetImageScale(float scale)
        {
            imageScale = Math.Clamp((float)Math.Round(scale, 2), 0.4f, 1.6f);
            scaleLabel.Text = $"{(int)Math.Round(imageScale * 100)}%";
            PersistLayout();
            preview.InvalidatePreview();
        }
        shrinkButton.Click += (_, _) => SetImageScale(imageScale - 0.01f);
        enlargeButton.Click += (_, _) => SetImageScale(imageScale + 0.01f);
        resetScaleButton.Click += (_, _) => SetImageScale(1f);
        var offsetLabel = new Label
        {
            Text = "左右 0 mm / 上下 0 mm",
            AutoSize = true,
            Margin = new Padding(8, 12, 8, 8),
        };
        var moveLeftButton = new Button { Text = "左へ", AutoSize = true, Margin = new Padding(4, 8, 4, 8) };
        var moveRightButton = new Button { Text = "右へ", AutoSize = true, Margin = new Padding(4, 8, 4, 8) };
        var moveUpButton = new Button { Text = "上へ", AutoSize = true, Margin = new Padding(4, 8, 4, 8) };
        var moveDownButton = new Button { Text = "下へ", AutoSize = true, Margin = new Padding(4, 8, 4, 8) };
        var resetOffsetButton = new Button { Text = "位置を戻す", AutoSize = true, Margin = new Padding(4, 8, 8, 8) };
        void RefreshOffset()
        {
            offsetLabel.Text = $"{FormatAxis(offsetXmm, "右", "左", "左右")} / {FormatAxis(offsetYmm, "下", "上", "上下")}";
            PersistLayout();
            preview.InvalidatePreview();
        }
        moveLeftButton.Click += (_, _) => { offsetXmm -= 1; RefreshOffset(); };
        moveRightButton.Click += (_, _) => { offsetXmm += 1; RefreshOffset(); };
        moveUpButton.Click += (_, _) => { offsetYmm -= 1; RefreshOffset(); };
        moveDownButton.Click += (_, _) => { offsetYmm += 1; RefreshOffset(); };
        resetOffsetButton.Click += (_, _) =>
        {
            offsetXmm = 0;
            offsetYmm = 0;
            RefreshOffset();
        };
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(4),
        };
        var positionBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(4, 0, 4, 4),
        };
        marginButton.Click += (_, _) =>
        {
            using var setup = new PageSetupDialog
            {
                Document = document,
                EnableMetric = true,
                AllowMargins = true,
                AllowOrientation = true,
                AllowPaper = true,
                AllowPrinter = true,
            };
            if (setup.ShowDialog(form) == DialogResult.OK)
            {
                PersistLayout();
                preview.InvalidatePreview();
            }
        };
        pdfButton.Click += (_, _) =>
        {
            PersistLayout();
            var baseName = SuicaExportNames.SuggestDocumentBaseName(App.State.ActiveEntrySet?.Name);
            using var saveDialog = new SaveFileDialog
            {
                Title = "PDF を保存",
                Filter = "PDF (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                FileName = baseName + ".pdf",
                AddExtension = true,
            };
            if (saveDialog.ShowDialog(form) != DialogResult.OK)
            {
                return;
            }

            try
            {
                PrintPngToPdfFile(
                    png,
                    saveDialog.FileName,
                    LayoutFromDocument(document, imageScale, offsetXmm, offsetYmm));
                MessageBox.Show(
                    form,
                    saveDialog.FileName,
                    "保存しました",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    form,
                    ex.Message,
                    "PDF の保存",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        };
        printButton.Click += (_, _) =>
        {
            PersistLayout();
            using var dialog = new PrintDialog
            {
                Document = document,
                AllowPrintToFile = false,
                AllowSelection = false,
                AllowSomePages = false,
                UseEXDialog = false,
            };
            if (dialog.ShowDialog(form) != DialogResult.OK)
            {
                return;
            }

            preview.InvalidatePreview();
            document.Print();
        };
        bar.Controls.Add(marginButton);
        bar.Controls.Add(printButton);
        bar.Controls.Add(pdfButton);
        bar.Controls.Add(closeButton);
        bar.Controls.Add(shrinkButton);
        bar.Controls.Add(scaleLabel);
        bar.Controls.Add(enlargeButton);
        bar.Controls.Add(resetScaleButton);
        positionBar.Controls.Add(moveLeftButton);
        positionBar.Controls.Add(moveRightButton);
        positionBar.Controls.Add(moveUpButton);
        positionBar.Controls.Add(moveDownButton);
        positionBar.Controls.Add(resetOffsetButton);
        positionBar.Controls.Add(offsetLabel);
        form.Controls.Add(preview);
        form.Controls.Add(positionBar);
        form.Controls.Add(bar);
        form.CancelButton = closeButton;
        SetImageScale(imageScale);
        RefreshOffset();
        form.FormClosed += (_, _) => PersistLayout();
        form.ShowDialog(new HwndOwner(owner));
    }

    private readonly record struct PrintLayout(
        float ImageScale,
        float OffsetXmm,
        float OffsetYmm,
        Margins Margins);

    private static PrintLayout ReadPrintLayoutFromSettings()
    {
        var saved = App.State.Settings;
        return new PrintLayout(
            Math.Clamp(saved.PrintImageScale <= 0 ? 1f : saved.PrintImageScale, 0.4f, 1.6f),
            saved.PrintOffsetXmm,
            saved.PrintOffsetYmm,
            new Margins(
                saved.PrintMarginLeft,
                saved.PrintMarginRight,
                saved.PrintMarginTop,
                saved.PrintMarginBottom));
    }

    private static PrintLayout LayoutFromDocument(
        PrintDocument document,
        float imageScale,
        float offsetXmm,
        float offsetYmm)
    {
        var margins = document.DefaultPageSettings.Margins;
        return new PrintLayout(
            imageScale,
            offsetXmm,
            offsetYmm,
            new Margins(margins.Left, margins.Right, margins.Top, margins.Bottom));
    }

    private static void Remember(PrintDocument document, float imageScale, float offsetXmm, float offsetYmm)
    {
        var settings = App.State.Settings;
        var margins = document.DefaultPageSettings.Margins;
        settings.PrintImageScale = imageScale;
        settings.PrintOffsetXmm = offsetXmm;
        settings.PrintOffsetYmm = offsetYmm;
        settings.PrintMarginLeft = margins.Left;
        settings.PrintMarginRight = margins.Right;
        settings.PrintMarginTop = margins.Top;
        settings.PrintMarginBottom = margins.Bottom;
        App.State.UpdateSettings(settings);
    }

    private static void PrintPngToPdfFile(byte[] png, string pdfPath, PrintLayout layout)
    {
        var printer = FindPdfPrinter()
            ?? throw new InvalidOperationException("PDF 用プリンタ（Microsoft Print to PDF など）が見つかりません。");

        using var stream = new MemoryStream(png);
        using var decoded = Image.FromStream(stream);
        using var image = new Bitmap(decoded);
        EnsurePrintResolution(image);
        var documentName = SuicaExportNames.SuggestDocumentBaseName(App.State.ActiveEntrySet?.Name);
        using var document = CreatePrintDocument(image, layout, documentName);
        document.PrinterSettings.PrinterName = printer;
        document.PrinterSettings.PrintToFile = true;
        document.PrinterSettings.PrintFileName = pdfPath;
        document.Print();
    }

    private static PrintDocument CreatePrintDocument(Bitmap image, PrintLayout layout, string documentName)
    {
        var document = new PrintDocument();
        document.DocumentName = documentName;
        PreferA4(document, layout.Margins);
        document.PrintPage += (_, e) => DrawPage(e, image, layout.ImageScale, layout.OffsetXmm, layout.OffsetYmm);
        return document;
    }

    private static string? FindPdfPrinter()
    {
        foreach (string name in PrinterSettings.InstalledPrinters)
        {
            if (name.Contains("PDF", StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return null;
    }

    private static void PreferA4(PrintDocument document, Margins margins)
    {
        foreach (PaperSize size in document.PrinterSettings.PaperSizes)
        {
            if (size.Kind == PaperKind.A4)
            {
                document.DefaultPageSettings.PaperSize = size;
                break;
            }
        }

        document.DefaultPageSettings.Landscape = false;
        document.OriginAtMargins = false;
        document.DefaultPageSettings.Margins = margins;
    }

    private static string FormatAxis(float mm, string positive, string negative, string zero) =>
        mm == 0 ? $"{zero} 0 mm" : mm > 0 ? $"{positive} {mm:0} mm" : $"{negative} {-mm:0} mm";

    private static void DrawPage(PrintPageEventArgs e, Image image, float imageScale, float offsetXmm, float offsetYmm)
    {
        var bounds = e.MarginBounds;
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            bounds = e.PageBounds;
        }

        var dpiX = image.HorizontalResolution > 1 ? image.HorizontalResolution : 96f;
        var dpiY = image.VerticalResolution > 1 ? image.VerticalResolution : 96f;
        var imageWidthHundredths = image.Width / dpiX * 100f;
        var imageHeightHundredths = image.Height / dpiY * 100f;
        var fit = Math.Min(bounds.Width / imageWidthHundredths, bounds.Height / imageHeightHundredths);
        var scale = fit * imageScale;
        var width = imageWidthHundredths * scale;
        var height = imageHeightHundredths * scale;
        const float mmToHundredths = 100f / 25.4f;
        var x = bounds.Left + (bounds.Width - width) / 2f + offsetXmm * mmToHundredths;
        var y = bounds.Top + offsetYmm * mmToHundredths;

        var graphics = e.Graphics;
        if (graphics is null)
        {
            e.HasMorePages = false;
            return;
        }

        var destScale = width / imageWidthHundredths;
        graphics.InterpolationMode = destScale < 0.99f
            ? InterpolationMode.HighQualityBicubic
            : InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.DrawImage(image, x, y, width, height);
        e.HasMorePages = false;
    }

    private static void EnsurePrintResolution(Bitmap image)
    {
        if (image.HorizontalResolution >= 120 && image.VerticalResolution >= 120)
        {
            return;
        }

        image.SetResolution((float)SuicaLayoutConstants.PrintDpi, (float)SuicaLayoutConstants.PrintDpi);
    }

    private sealed class HwndOwner(IntPtr handle) : IWin32Window
    {
        public IntPtr Handle { get; } = handle;
    }
}

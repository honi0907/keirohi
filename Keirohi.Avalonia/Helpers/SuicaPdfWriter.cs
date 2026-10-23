using Keirohi_App.Services;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Keirohi.Avalonia.Helpers;

internal static class SuicaPdfWriter
{
    public static void WritePngToPdf(string pdfPath, byte[] png, SuicaPrintLayout layout)
    {
        using var document = new PdfDocument();
        document.Info.Title = Path.GetFileNameWithoutExtension(pdfPath);
        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;

        using var gfx = XGraphics.FromPdfPage(page);
        using var stream = new MemoryStream(png);
        using var image = XImage.FromStream(stream);

        var pageWidth = page.Width.Point;
        var pageHeight = page.Height.Point;
        // Margins stored as hundredths of inch (same as System.Drawing.Printing.Margins)
        const float hundredthsToPoint = 72f / 100f;
        var left = layout.MarginLeftHundredthsInch * hundredthsToPoint;
        var right = layout.MarginRightHundredthsInch * hundredthsToPoint;
        var top = layout.MarginTopHundredthsInch * hundredthsToPoint;
        var bottom = layout.MarginBottomHundredthsInch * hundredthsToPoint;

        var boundsWidth = pageWidth - left - right;
        var boundsHeight = pageHeight - top - bottom;
        var dpi = (float)SuicaLayoutConstants.PrintDpi;
        var imageWidthPt = image.PixelWidth / dpi * 72f;
        var imageHeightPt = image.PixelHeight / dpi * 72f;
        var fit = Math.Min(boundsWidth / imageWidthPt, boundsHeight / imageHeightPt);
        var scale = fit * layout.ImageScale;
        var width = imageWidthPt * scale;
        var height = imageHeightPt * scale;
        const float mmToPoint = 72f / 25.4f;
        var x = left + (boundsWidth - width) / 2f + layout.OffsetXmm * mmToPoint;
        var y = top + layout.OffsetYmm * mmToPoint;

        gfx.DrawImage(image, x, y, width, height);
        document.Save(pdfPath);
    }
}

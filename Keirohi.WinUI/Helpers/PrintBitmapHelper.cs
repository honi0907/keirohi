using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using WinRtBuffer = Windows.Storage.Streams.Buffer;

namespace Keirohi_App.Helpers;

public static class PrintBitmapHelper
{
    public static async Task<BitmapImage> FromRenderTargetAsync(RenderTargetBitmap source)
    {
        var pixels = await source.GetPixelsAsync();
        var width = source.PixelWidth;
        var height = source.PixelHeight;
        CryptographicBuffer.CopyToByteArray(pixels, out var pixelBytes);

        using var stream = new InMemoryRandomAccessStream();
        await WritePngAsync(stream, (uint)width, (uint)height, pixelBytes, 96);
        stream.Seek(0);

        var image = new BitmapImage();
        await image.SetSourceAsync(stream);
        return image;
    }

    public static async Task<byte[]> ToPngBytesAsync(RenderTargetBitmap source, double dpi = 96)
    {
        var pixels = await source.GetPixelsAsync();
        var width = source.PixelWidth;
        var height = source.PixelHeight;
        CryptographicBuffer.CopyToByteArray(pixels, out var pixelBytes);

        using var stream = new InMemoryRandomAccessStream();
        await WritePngAsync(stream, (uint)width, (uint)height, pixelBytes, dpi);
        stream.Seek(0);
        var bytes = new byte[stream.Size];
        var buffer = new WinRtBuffer((uint)bytes.Length);
        await stream.ReadAsync(buffer, (uint)bytes.Length, InputStreamOptions.None);
        CryptographicBuffer.CopyToByteArray(buffer, out bytes);
        return bytes;
    }

    public static async Task SaveElementAsPngAsync(
        UIElement element,
        int widthPx,
        int heightPx,
        StorageFile file)
    {
        var rendered = new RenderTargetBitmap();
        await rendered.RenderAsync(element, widthPx, heightPx);
        var pixels = await rendered.GetPixelsAsync();
        CryptographicBuffer.CopyToByteArray(pixels, out var pixelBytes);

        using var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
        await WritePngAsync(stream, (uint)rendered.PixelWidth, (uint)rendered.PixelHeight, pixelBytes, 96);
        await stream.FlushAsync();
    }

    private static async Task WritePngAsync(
        IRandomAccessStream stream,
        uint width,
        uint height,
        byte[] pixelBytes,
        double dpi)
    {
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            width,
            height,
            dpi,
            dpi,
            pixelBytes);
        await encoder.FlushAsync();
    }
}

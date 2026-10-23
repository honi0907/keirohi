using Avalonia.Controls;

namespace Keirohi.Avalonia.Helpers;

/// <summary>印刷・PDF（Windows GDI / クロスプラットフォーム）の入口。</summary>
public static class SuicaPrintService
{
    public static Task ShowPrintConfirmationAsync(byte[] png, Window? owner = null)
    {
#if WINDOWS
        SuicaGdiPrint.Show(png);
        return Task.CompletedTask;
#else
        return SuicaCrossPrint.ShowPrintConfirmationAsync(png, owner);
#endif
    }

    public static Task<string?> TrySavePdfAsync(byte[] png, Window? owner = null)
    {
#if WINDOWS
        return Task.FromResult(SuicaGdiPrint.TrySavePdfWithDialog(png));
#else
        return SuicaCrossPrint.TrySavePdfAsync(png, owner);
#endif
    }
}

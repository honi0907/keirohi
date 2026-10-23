using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Keirohi_App.Services;
using Keirohi.Avalonia.Windows;

namespace Keirohi.Avalonia.Helpers;

internal static class SuicaCrossPrint
{
    public static async Task ShowPrintConfirmationAsync(byte[] png, Window? owner)
    {
        owner ??= AppHost.MainWindowInstance;
        if (owner is null)
        {
            return;
        }

        var window = new SuicaPrintReviewWindow(png);
        await window.ShowDialog(owner);
    }

    public static async Task<string?> TrySavePdfAsync(byte[] png, Window? owner)
    {
        owner ??= AppHost.MainWindowInstance;
        if (owner is null)
        {
            return null;
        }

        var baseName = SuicaExportNames.SuggestDocumentBaseName(AppHost.State.ActiveEntrySet?.Name);
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "PDF を保存",
            SuggestedFileName = baseName + ".pdf",
            FileTypeChoices =
            [
                new FilePickerFileType("PDF") { Patterns = ["*.pdf"] },
            ],
        });

        var path = file?.TryGetLocalPath();
        if (path is null)
        {
            return null;
        }

        var layout = SuicaPrintLayout.FromSettings();
        SuicaPdfWriter.WritePngToPdf(path, png, layout);
        return path;
    }
}

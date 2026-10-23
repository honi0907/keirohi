using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Keirohi_App.Services;
using Keirohi.Avalonia.Helpers;

namespace Keirohi.Avalonia.Windows;

public partial class SuicaPreviewWindow : Window
{
    public SuicaPreviewWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            Preview.Rebuild();
            ApplyPreviewActionVisibility();
        };
    }

    private void ApplyPreviewActionVisibility()
    {
        var showExtra = AppHost.State.Settings.ShowSuicaPreviewPngAndPrint;
        SavePngButton.IsVisible = showExtra;
        PrintButton.IsVisible = showExtra;
    }

    private async void SavePng_Click(object? sender, RoutedEventArgs e)
    {
        var storage = StorageProvider;
        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "PNG を保存",
            SuggestedFileName = SuicaExportNames.SuggestDocumentBaseName(AppHost.State.ActiveEntrySet?.Name),
            FileTypeChoices =
            [
                new FilePickerFileType("PNG") { Patterns = ["*.png"] },
            ],
        });
        var path = file?.TryGetLocalPath();
        if (path is null)
        {
            return;
        }

        try
        {
            var png = await Preview.GetPrintPngAsync();
            await File.WriteAllBytesAsync(path, png);
            await DialogService.ShowOkAsync("保存しました", path);
        }
        catch (Exception ex)
        {
            await DialogService.ShowOkAsync("保存", $"PNG の保存に失敗しました。\n\n{ex.Message}");
        }
    }

    private async void SavePdf_Click(object? sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        if (btn is not null)
        {
            btn.IsEnabled = false;
        }

        try
        {
            var png = await Preview.GetPrintPngAsync();
            var path = SuicaGdiPrint.TrySavePdfWithDialog(png);
            if (path is not null)
            {
                await DialogService.ShowOkAsync("保存しました", path);
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowOkAsync("PDF の保存", $"PDF の保存に失敗しました。\n\n{ex.Message}");
        }
        finally
        {
            if (btn is not null)
            {
                btn.IsEnabled = true;
            }
        }
    }

    private async void Print_Click(object? sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        if (btn is not null)
        {
            btn.IsEnabled = false;
        }

        try
        {
            var png = await Preview.GetPrintPngAsync();
            SuicaGdiPrint.Show(png);
        }
        catch (Exception ex)
        {
            await DialogService.ShowOkAsync("印刷", $"印刷画面を開けませんでした。\n\n{ex.Message}");
        }
        finally
        {
            if (btn is not null)
            {
                btn.IsEnabled = true;
            }
        }
    }
}

using Keirohi_App.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Keirohi_App;

public sealed partial class SuicaPreviewWindow : Window
{
    public SuicaPreviewWindow()
    {
        InitializeComponent();
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 720;
            presenter.PreferredMinimumHeight = 640;
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(900, 820));
        Activated += (_, _) => _ = Preview.WarmPrintCacheAsync();
    }

    public async Task<byte[]> CapturePngAsync()
    {
        if (!Preview.IsLoaded)
        {
            var loaded = new TaskCompletionSource();
            void OnLoaded(object sender, RoutedEventArgs e)
            {
                Preview.Loaded -= OnLoaded;
                loaded.TrySetResult();
            }

            Preview.Loaded += OnLoaded;
            await loaded.Task;
        }

        return await Preview.GetPrintPngAsync();
    }

    private async void SavePng_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker();
        PickerHelper.BindToWindow(picker, this);
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.SuggestedFileName = $"モバイルSuica明細_{DateTime.Now:yyyyMMdd}";
        picker.FileTypeChoices.Add("PNG", [".png"]);

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        try
        {
            var (widthPx, heightPx) = Preview.GetPrintPixelSize();
            await PrintBitmapHelper.SaveElementAsPngAsync(Preview.PrintRoot, widthPx, heightPx, file);
            await DialogHelper.ShowOkAsync(Content.XamlRoot, "保存しました", file.Path);
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowOkAsync(Content.XamlRoot, "保存", $"PNG の保存に失敗しました。\n\n{ex.Message}");
        }
    }

    private async void Print_Click(object sender, RoutedEventArgs e)
    {
        var printButton = sender as Button;
        if (printButton is not null)
        {
            printButton.IsEnabled = false;
        }

        try
        {
            var png = await Preview.GetPrintPngAsync();
            var hwnd = WindowNative.GetWindowHandle(this);
            SuicaGdiPrint.Show(hwnd, png);
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowOkAsync(
                Content.XamlRoot,
                "印刷",
                $"印刷画面を開けませんでした。\n\n{ex.Message}");
        }
        finally
        {
            if (printButton is not null)
            {
                printButton.IsEnabled = true;
            }
        }
    }
}

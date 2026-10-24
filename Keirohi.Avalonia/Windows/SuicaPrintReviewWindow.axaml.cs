using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Keirohi_App.Services;
using Keirohi.Avalonia.Helpers;

namespace Keirohi.Avalonia.Windows;

public partial class SuicaPrintReviewWindow : Window
{
    private byte[] png = [];
    private float imageScale;
    private float offsetYmm;
    private float offsetXmm;

    public SuicaPrintReviewWindow()
    {
        InitializeComponent();
    }

    public SuicaPrintReviewWindow(byte[] png)
        : this()
    {
        this.png = png;
        var layout = SuicaPrintLayout.FromSettings();
        imageScale = layout.ImageScale;
        offsetXmm = layout.OffsetXmm;
        offsetYmm = layout.OffsetYmm;
        PreviewImage.Source = new Bitmap(new MemoryStream(png));
        RefreshLabels();
        Closed += (_, _) => PersistLayout();
    }

    private SuicaPrintLayout CurrentLayout() => new(
        imageScale,
        offsetXmm,
        offsetYmm,
        AppHost.State.Settings.PrintMarginLeft,
        AppHost.State.Settings.PrintMarginRight,
        AppHost.State.Settings.PrintMarginTop,
        AppHost.State.Settings.PrintMarginBottom);

    private void PersistLayout() => CurrentLayout().Persist();

    private void RefreshLabels()
    {
        ScaleText.Text = $"倍率 {imageScale * 100:0}%";
        OffsetText.Text =
            $"位置: 左右 {FormatAxis(offsetXmm, "右", "左")} / 上下 {FormatAxis(offsetYmm, "下", "上")}";
    }

    private static string FormatAxis(float mm, string positive, string negative) =>
        mm == 0 ? "0 mm" : mm > 0 ? $"{positive} {mm:0} mm" : $"{negative} {-mm:0} mm";

    private void Shrink_Click(object? sender, RoutedEventArgs e)
    {
        imageScale = Math.Max(0.4f, (float)(Math.Round((imageScale - 0.01f) * 100) / 100));
        RefreshLabels();
    }

    private void Enlarge_Click(object? sender, RoutedEventArgs e)
    {
        imageScale = Math.Min(1.6f, (float)(Math.Round((imageScale + 0.01f) * 100) / 100));
        RefreshLabels();
    }

    private void ResetScale_Click(object? sender, RoutedEventArgs e)
    {
        imageScale = 1f;
        RefreshLabels();
    }

    private void MoveLeft_Click(object? sender, RoutedEventArgs e)
    {
        offsetXmm -= 1f;
        RefreshLabels();
    }

    private void MoveRight_Click(object? sender, RoutedEventArgs e)
    {
        offsetXmm += 1f;
        RefreshLabels();
    }

    private void MoveUp_Click(object? sender, RoutedEventArgs e)
    {
        offsetYmm -= 1f;
        RefreshLabels();
    }

    private void MoveDown_Click(object? sender, RoutedEventArgs e)
    {
        offsetYmm += 1f;
        RefreshLabels();
    }

    private void ResetOffset_Click(object? sender, RoutedEventArgs e)
    {
        offsetXmm = 0f;
        offsetYmm = 0f;
        RefreshLabels();
    }

    private async void SavePdf_Click(object? sender, RoutedEventArgs e)
    {
        PersistLayout();
        var baseName = SuicaExportNames.SuggestDocumentBaseName(AppHost.State.ActiveEntrySet?.Name);
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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
            return;
        }

        try
        {
            SuicaPdfWriter.WritePngToPdf(path, png, CurrentLayout());
            await DialogService.ShowOkAsync("保存しました", path, this);
        }
        catch (Exception ex)
        {
            await DialogService.ShowOkAsync("PDF の保存", $"PDF の保存に失敗しました。\n\n{ex.Message}", this);
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}

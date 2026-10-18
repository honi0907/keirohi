using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Keirohi_App.Helpers;

public static class DialogHelper
{
    public static async Task ShowOkAsync(XamlRoot xamlRoot, string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = xamlRoot,
        };
        await dialog.ShowAsync();
    }
}

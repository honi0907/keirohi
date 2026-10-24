using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Keirohi.Avalonia.Helpers;

public static class DialogService
{
    public static Window? Owner => AppHost.MainWindowInstance;

    /// <summary>モーダル子ウィンドウ上から呼ぶときは、その <see cref="Window"/> を渡す（Mac で前面ウィンドウと重なるとクリック不能になる）。</summary>
    public static Task ShowOkAsync(string title, string message, Window? owner = null) =>
        ShowAsync(
            title,
            new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            [("OK", "ok")],
            owner);

    public static async Task<bool> ShowConfirmAsync(
        string title,
        string message,
        string primaryText = "OK",
        string closeText = "キャンセル",
        Window? owner = null)
    {
        var id = await ShowAsync(
            title,
            new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            [(primaryText, "primary"), (closeText, "close")],
            owner);
        return id == "primary";
    }

    public static Task<string?> ShowThreeButtonAsync(
        string title,
        string message,
        string primaryText,
        string secondaryText,
        string closeText,
        Window? owner = null) =>
        ShowAsync(
            title,
            new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            [(primaryText, "primary"), (secondaryText, "secondary"), (closeText, "close")],
            owner);

    public static async Task<string?> ShowPromptAsync(
        string title,
        string initialText,
        string placeholder = "",
        Window? owner = null)
    {
        var box = new TextBox
        {
            Text = initialText,
            Watermark = placeholder,
            MinWidth = 280,
        };
        box.CaretIndex = initialText.Length;
        var panel = new StackPanel { Spacing = 8, Children = { box } };
        var id = await ShowAsync(title, panel, [("保存", "save"), ("キャンセル", "cancel")], owner);
        return id == "save" ? box.Text : null;
    }

    private static async Task<string?> ShowAsync(
        string title,
        Control content,
        IReadOnlyList<(string Label, string Id)> buttons,
        Window? owner = null)
    {
        owner ??= Owner;
        if (owner is null)
        {
            return null;
        }

        var tcs = new TaskCompletionSource<string?>();
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            ShowActivated = true,
        };

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 12, 0, 0),
        };

        foreach (var (label, id) in buttons)
        {
            var captured = id;
            var btn = new Button { Content = label };
            btn.Click += (_, _) =>
            {
                tcs.TrySetResult(captured);
                dialog.Close();
            };
            buttonRow.Children.Add(btn);
        }

        var root = new StackPanel { Margin = new Thickness(16), Spacing = 8, Children = { content, buttonRow } };
        dialog.Content = root;
        dialog.Closed += (_, _) => tcs.TrySetResult(null);
        _ = dialog.ShowDialog(owner);
        return await tcs.Task;
    }
}

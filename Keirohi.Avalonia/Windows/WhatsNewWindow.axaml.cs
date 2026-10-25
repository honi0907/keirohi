using Avalonia.Controls;
using Avalonia.Interactivity;
using Keirohi_App.Updates;

namespace Keirohi.Avalonia.Windows;

public enum WhatsNewCloseResult
{
    Ok,
    DoNotShowAgain,
}

public partial class WhatsNewWindow : Window
{
    public WhatsNewWindow()
    {
        InitializeComponent();
    }

    public WhatsNewWindow(ReleaseNotesEntry entry, string version)
    {
        InitializeComponent();
        var date = string.IsNullOrWhiteSpace(entry.Date) ? "" : $"（{entry.Date}）";
        var title = string.IsNullOrWhiteSpace(entry.Title) ? "更新内容" : entry.Title;
        HeaderText.Text = $"バージョン {version} {date}\n{title}";
        ItemsList.ItemsSource = entry.Items;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e) => Close(WhatsNewCloseResult.Ok);

    private void DoNotShowAgain_Click(object? sender, RoutedEventArgs e) =>
        Close(WhatsNewCloseResult.DoNotShowAgain);
}

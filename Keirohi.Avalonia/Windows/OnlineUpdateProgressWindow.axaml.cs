using Avalonia.Controls;
using Keirohi_App.Updates;

namespace Keirohi.Avalonia.Windows;

public partial class OnlineUpdateProgressWindow : Window
{
    public OnlineUpdateProgressWindow()
    {
        InitializeComponent();
        ProgressBar.IsIndeterminate = true;
        StatusText.Text = "準備中...";
    }

    public static bool ShouldShowForProgress(OnlineUpdateProgress progress)
    {
        if (string.IsNullOrWhiteSpace(progress.Message))
        {
            return progress.Percent > 0;
        }

        var message = progress.Message;
        if (message.Contains("更新を確認", StringComparison.Ordinal))
        {
            return false;
        }

        return message.Contains("ダウンロード", StringComparison.Ordinal)
               || message.Contains("DMG", StringComparison.Ordinal)
               || message.Contains("更新を適用", StringComparison.Ordinal)
               || message.Contains("再起動", StringComparison.Ordinal);
    }

    public void Apply(OnlineUpdateProgress progress)
    {
        StatusText.Text = string.IsNullOrWhiteSpace(progress.Message)
            ? "処理中..."
            : progress.Message;

        if (progress.Percent > 0)
        {
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = Math.Clamp(progress.Percent, 0, 100);
            return;
        }

        ProgressBar.IsIndeterminate = true;
    }
}

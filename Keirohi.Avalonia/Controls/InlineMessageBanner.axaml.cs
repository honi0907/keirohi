using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Keirohi.Avalonia.Controls;

public partial class InlineMessageBanner : UserControl
{
    public InlineMessageBanner()
    {
        InitializeComponent();
    }

    public void Show(string message, MessageBannerSeverity severity = MessageBannerSeverity.Success)
    {
        MessageText.Text = message;
        ApplySeverityStyle(severity);
        RootBorder.IsVisible = true;
    }

    public void Hide() => RootBorder.IsVisible = false;

    private void Dismiss_Click(object? sender, RoutedEventArgs e) => Hide();

    private void ApplySeverityStyle(MessageBannerSeverity severity)
    {
        var (bg, border) = severity switch
        {
            MessageBannerSeverity.Success => ("#FFDFF6DD", "#FF0F7B0F"),
            MessageBannerSeverity.Warning => ("#FFFFF4CE", "#FF797673"),
            MessageBannerSeverity.Error => ("#FFFDE7E9", "#FFC42B1C"),
            MessageBannerSeverity.Informational => ("#FFF3F2F1", "#FF0078D4"),
            _ => ("#FFDFF6DD", "#FF0F7B0F"),
        };

        RootBorder.Background = new SolidColorBrush(Color.Parse(bg));
        RootBorder.BorderBrush = new SolidColorBrush(Color.Parse(border));
    }
}

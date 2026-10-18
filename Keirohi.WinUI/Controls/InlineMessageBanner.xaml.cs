using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Keirohi_App.Controls;

public sealed partial class InlineMessageBanner : UserControl
{
    public InlineMessageBanner()
    {
        InitializeComponent();
    }

    public void Show(string message, InfoBarSeverity severity = InfoBarSeverity.Success)
    {
        MessageText.Text = message;
        ApplySeverityStyle(severity);
        RootBorder.Visibility = Visibility.Visible;
    }

    public void Hide() => RootBorder.Visibility = Visibility.Collapsed;

    private void Dismiss_Click(object sender, RoutedEventArgs e) => Hide();

    private void ApplySeverityStyle(InfoBarSeverity severity)
    {
        var (backgroundKey, borderKey) = severity switch
        {
            InfoBarSeverity.Success => (
                "SystemFillColorSuccessBackgroundBrush",
                "SystemFillColorSuccessBrush"),
            InfoBarSeverity.Warning => (
                "SystemFillColorCautionBackgroundBrush",
                "SystemFillColorCautionBrush"),
            InfoBarSeverity.Error => (
                "SystemFillColorCriticalBackgroundBrush",
                "SystemFillColorCriticalBrush"),
            InfoBarSeverity.Informational => (
                "SystemFillColorAttentionBackgroundBrush",
                "SystemFillColorAttentionBrush"),
            _ => (
                "SystemFillColorSuccessBackgroundBrush",
                "SystemFillColorSuccessBrush"),
        };

        if (Application.Current.Resources.TryGetValue(backgroundKey, out var bg) && bg is Brush background)
        {
            RootBorder.Background = background;
        }

        if (Application.Current.Resources.TryGetValue(borderKey, out var bd) && bd is Brush border)
        {
            RootBorder.BorderBrush = border;
        }
    }
}

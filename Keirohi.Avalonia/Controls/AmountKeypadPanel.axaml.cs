using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Keirohi.Avalonia.Controls;

public partial class AmountKeypadPanel : UserControl
{
    public event EventHandler<string>? DigitPressed;
    public event EventHandler? ClearPressed;
    public event EventHandler? BackspacePressed;
    public event EventHandler? ClosePressed;

    public AmountKeypadPanel()
    {
        InitializeComponent();
    }

    private void Digit_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string digit)
        {
            DigitPressed?.Invoke(this, digit);
        }
    }

    private void Clear_Click(object? sender, RoutedEventArgs e) => ClearPressed?.Invoke(this, EventArgs.Empty);

    private void Backspace_Click(object? sender, RoutedEventArgs e) => BackspacePressed?.Invoke(this, EventArgs.Empty);

    private void Close_Click(object? sender, RoutedEventArgs e) => ClosePressed?.Invoke(this, EventArgs.Empty);
}

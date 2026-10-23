using System.Runtime.CompilerServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Keirohi.Avalonia.Controls;

namespace Keirohi.Avalonia.Helpers;

public static class AmountKeypadAttach
{
    private const int MaxDigits = 6;
    private const int MaxAmount = 999_999;

    private static readonly ConditionalWeakTable<TextBox, KeypadHost> Hosts = new();

    public static void Enable(TextBox textBox)
    {
        if (Hosts.TryGetValue(textBox, out _))
        {
            return;
        }

        Hosts.Add(textBox, new KeypadHost(textBox));
    }

    private sealed class KeypadHost
    {
        private readonly TextBox textBox;
        private readonly Border chrome;
        private readonly EventHandler<PointerEventArgs> outsidePointerPressed;
        private IInputElement? outsideRoot;
        private bool placed;
        private bool suppressShow;

        public KeypadHost(TextBox textBox)
        {
            this.textBox = textBox;
            var keypad = new AmountKeypadPanel();
            keypad.DigitPressed += (_, digit) => ApplyDigit(digit);
            keypad.ClearPressed += (_, _) => SetAmountText("");
            keypad.BackspacePressed += (_, _) => Backspace();
            keypad.ClosePressed += (_, _) => Dismiss();

            chrome = new Border
            {
                Child = keypad,
                Padding = new Thickness(4),
                Margin = new Thickness(0, 4, 0, 0),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.Parse("#FFE0E0E0")),
                Background = new SolidColorBrush(Color.Parse("#FFFDFDFD")),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsVisible = false,
            };

            outsidePointerPressed = OnOutsidePointerPressed;

            textBox.AttachedToVisualTree += (_, _) =>
            {
                PlaceBelowTextBox();
                HookOutsidePointer();
            };
            textBox.DetachedFromVisualTree += (_, _) => UnhookOutsidePointer();

            textBox.GotFocus += (_, _) =>
            {
                if (!suppressShow)
                {
                    Show();
                }
            };

            void OpenFromPointer(object? _, PointerEventArgs e)
            {
                suppressShow = false;
                Show();
            }

            textBox.AddHandler(InputElement.PointerPressedEvent, OpenFromPointer, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            textBox.AddHandler(InputElement.PointerReleasedEvent, OpenFromPointer, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            textBox.Tapped += (_, _) =>
            {
                suppressShow = false;
                Show();
            };

            textBox.LostFocus += (_, _) => ScheduleHideAfterFocusChange();
            textBox.KeyDown += TextBox_KeyDown;
            textBox.TextChanged += TextBox_TextChanged;
        }

        private void ScheduleHideAfterFocusChange()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (IsFocusWithinKeypad())
                {
                    return;
                }

                suppressShow = false;
                Hide();
            }, DispatcherPriority.Input);
        }

        private bool IsFocusWithinKeypad()
        {
            if (TopLevel.GetTopLevel(textBox) is not { } top)
            {
                return false;
            }

            if (!ReferenceEquals(top.FocusManager?.GetFocusedElement(), textBox))
            {
                return chrome.IsPointerOver || textBox.IsPointerOver;
            }

            return false;
        }

        private void PlaceBelowTextBox()
        {
            if (placed || textBox.GetVisualParent() is not Panel parent)
            {
                return;
            }

            if (parent is Grid grid)
            {
                var insertRow = Grid.GetRow(textBox) + Math.Max(1, Grid.GetRowSpan(textBox));
                foreach (var child in grid.Children.OfType<Control>())
                {
                    if (Grid.GetRow(child) >= insertRow)
                    {
                        Grid.SetRow(child, Grid.GetRow(child) + 1);
                    }
                }

                Grid.SetRow(chrome, insertRow);
                Grid.SetColumn(chrome, Grid.GetColumn(textBox));
                Grid.SetColumnSpan(chrome, Math.Max(1, Grid.GetColumnSpan(textBox)));
                grid.Children.Add(chrome);

                var maxRow = grid.Children.OfType<Control>().Select(c => Grid.GetRow(c)).DefaultIfEmpty(0).Max();
                while (grid.RowDefinitions.Count <= maxRow)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }
            }
            else
            {
                var index = parent.Children.IndexOf(textBox);
                parent.Children.Insert(index + 1, chrome);
            }

            placed = true;
        }

        private void HookOutsidePointer()
        {
            var root = textBox.GetVisualRoot() as InputElement;
            if (root is null || ReferenceEquals(outsideRoot, root))
            {
                return;
            }

            UnhookOutsidePointer();
            root.AddHandler(InputElement.PointerPressedEvent, outsidePointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            outsideRoot = root;
        }

        private void UnhookOutsidePointer()
        {
            if (outsideRoot is null)
            {
                return;
            }

            outsideRoot.RemoveHandler(InputElement.PointerPressedEvent, outsidePointerPressed);
            outsideRoot = null;
        }

        private void OnOutsidePointerPressed(object? sender, PointerEventArgs e)
        {
            if (!chrome.IsVisible)
            {
                return;
            }

            if (e.Source is not Visual source)
            {
                return;
            }

            if (IsWithin(textBox, source) || IsWithin(chrome, source))
            {
                return;
            }

            suppressShow = true;
            Hide();
        }

        private static bool IsWithin(Visual ancestor, Visual node)
        {
            var current = node;
            while (current is not null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = current.GetVisualParent();
            }

            return false;
        }

        private void Dismiss()
        {
            suppressShow = true;
            Hide();
            textBox.Focus();
        }

        private void Show()
        {
            PlaceBelowTextBox();
            HookOutsidePointer();
            chrome.IsVisible = true;
        }

        private void Hide() => chrome.IsVisible = false;

        private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            var sanitized = SanitizeDigits(textBox.Text);
            if (sanitized == textBox.Text)
            {
                return;
            }

            var caret = textBox.CaretIndex;
            textBox.Text = sanitized;
            textBox.CaretIndex = Math.Min(caret, sanitized.Length);
        }

        private static string SanitizeDigits(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                if (c is >= '0' and <= '9')
                {
                    sb.Append(c);
                }
                else if (c is >= '０' and <= '９')
                {
                    sb.Append((char)(c - '０' + '0'));
                }
            }

            return sb.ToString();
        }

        private void TextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && chrome.IsVisible)
            {
                Dismiss();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Back)
            {
                Backspace();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete)
            {
                SetAmountText("");
                e.Handled = true;
                return;
            }

            var digit = e.Key switch
            {
                Key.D0 or Key.NumPad0 => "0",
                Key.D1 or Key.NumPad1 => "1",
                Key.D2 or Key.NumPad2 => "2",
                Key.D3 or Key.NumPad3 => "3",
                Key.D4 or Key.NumPad4 => "4",
                Key.D5 or Key.NumPad5 => "5",
                Key.D6 or Key.NumPad6 => "6",
                Key.D7 or Key.NumPad7 => "7",
                Key.D8 or Key.NumPad8 => "8",
                Key.D9 or Key.NumPad9 => "9",
                _ => null,
            };

            if (digit is not null)
            {
                ApplyDigit(digit);
                e.Handled = true;
            }
        }

        private void ApplyDigit(string digit)
        {
            var text = textBox.Text ?? string.Empty;
            var start = textBox.CaretIndex;
            var length = textBox.SelectionEnd - textBox.SelectionStart;
            if (length > 0)
            {
                text = text.Remove(start, length);
            }

            var next = text + digit;
            if (next.Length > MaxDigits || !int.TryParse(next, out var value) || value > MaxAmount)
            {
                return;
            }

            SetAmountText(next);
        }

        private void Backspace()
        {
            var text = textBox.Text ?? string.Empty;
            if (text.Length == 0)
            {
                return;
            }

            var start = textBox.CaretIndex;
            var length = textBox.SelectionEnd - textBox.SelectionStart;
            if (length > 0)
            {
                SetAmountText(text.Remove(start, length));
            }
            else if (start > 0)
            {
                SetAmountText(text.Remove(start - 1, 1));
                textBox.CaretIndex = start - 1;
            }
            else
            {
                SetAmountText(text[..^1]);
            }
        }

        private void SetAmountText(string value)
        {
            textBox.Text = value;
            textBox.CaretIndex = value.Length;
        }
    }
}

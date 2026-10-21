using System.Runtime.CompilerServices;
using System.Text;
using Keirohi_App.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Keirohi_App.Helpers;

/// <summary>
/// 金額欄の直下にテンキーを置く。Popup や画面全体のオーバーレイは WinUI が落ちるため使わない。
/// </summary>
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

        textBox.InputScope = AmountInputScope;
        var host = new KeypadHost(textBox);
        Hosts.Add(textBox, host);
    }

    private static InputScope AmountInputScope
    {
        get
        {
            var scope = new InputScope();
            scope.Names.Add(new InputScopeName { NameValue = InputScopeNameValue.Number });
            return scope;
        }
    }

    private sealed class KeypadHost
    {
        private readonly TextBox _textBox;
        private readonly Border _chrome;
        private readonly PointerEventHandler _outsidePointerPressed;
        private UIElement? _outsideRoot;
        private bool _placed;
        private bool _suppressShow;

        public KeypadHost(TextBox textBox)
        {
            _textBox = textBox;
            var keypad = new AmountKeypadPanel();
            keypad.DigitPressed += (_, digit) => ApplyDigit(digit);
            keypad.ClearPressed += (_, _) => SetAmountText("");
            keypad.BackspacePressed += (_, _) => Backspace();
            keypad.ClosePressed += (_, _) => Dismiss();

            _chrome = new Border
            {
                Child = keypad,
                Padding = new Thickness(4),
                Margin = new Thickness(0, 4, 0, 0),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Visibility = Visibility.Collapsed,
            };
            if (Application.Current.Resources.TryGetValue("CardBackgroundFillColorDefaultBrush", out var bg)
                && bg is Brush backgroundBrush)
            {
                _chrome.Background = backgroundBrush;
            }

            if (Application.Current.Resources.TryGetValue("CardStrokeColorDefaultBrush", out var stroke)
                && stroke is Brush borderBrush)
            {
                _chrome.BorderBrush = borderBrush;
            }

            _outsidePointerPressed = OnOutsidePointerPressed;
            _textBox.Loaded += (_, _) =>
            {
                PlaceBelowTextBox();
                HookOutsidePointer();
            };
            _textBox.Unloaded += (_, _) => UnhookOutsidePointer();
            _textBox.GotFocus += (_, _) =>
            {
                if (_suppressShow)
                {
                    return;
                }

                Show();
            };
            _textBox.AddHandler(
                UIElement.PointerPressedEvent,
                new PointerEventHandler((_, _) =>
                {
                    _suppressShow = false;
                    Show();
                }),
                handledEventsToo: true);
            _textBox.LostFocus += (_, _) =>
            {
                _suppressShow = false;
                Hide();
            };
            _textBox.KeyDown += TextBox_KeyDown;
            _textBox.TextChanged += TextBox_TextChanged;
            PlaceBelowTextBox();
        }

        private void PlaceBelowTextBox()
        {
            if (_placed || VisualTreeHelper.GetParent(_textBox) is not Panel parent)
            {
                return;
            }

            if (parent is Grid grid)
            {
                var insertRow = Grid.GetRow(_textBox) + Math.Max(1, Grid.GetRowSpan(_textBox));
                foreach (var child in grid.Children.OfType<FrameworkElement>())
                {
                    if (Grid.GetRow(child) >= insertRow)
                    {
                        Grid.SetRow(child, Grid.GetRow(child) + 1);
                    }
                }

                Grid.SetRow(_chrome, insertRow);
                Grid.SetColumn(_chrome, Grid.GetColumn(_textBox));
                Grid.SetColumnSpan(_chrome, Math.Max(1, Grid.GetColumnSpan(_textBox)));
                grid.Children.Add(_chrome);

                var maxRow = grid.Children.OfType<FrameworkElement>().Select(child => Grid.GetRow(child)).DefaultIfEmpty(0).Max();
                while (grid.RowDefinitions.Count <= maxRow)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }
            }
            else
            {
                var index = parent.Children.IndexOf(_textBox);
                parent.Children.Insert(index + 1, _chrome);
            }

            _placed = true;
        }

        private void Dismiss()
        {
            _suppressShow = true;
            Hide();
            _textBox.Focus(FocusState.Programmatic);
        }

        private void Show()
        {
            PlaceBelowTextBox();
            HookOutsidePointer();
            _chrome.Visibility = Visibility.Visible;
        }

        private void HookOutsidePointer()
        {
            if (_textBox.XamlRoot?.Content is not UIElement root || ReferenceEquals(_outsideRoot, root))
            {
                return;
            }

            UnhookOutsidePointer();
            root.AddHandler(UIElement.PointerPressedEvent, _outsidePointerPressed, handledEventsToo: true);
            _outsideRoot = root;
        }

        private void UnhookOutsidePointer()
        {
            if (_outsideRoot is null)
            {
                return;
            }

            _outsideRoot.RemoveHandler(UIElement.PointerPressedEvent, _outsidePointerPressed);
            _outsideRoot = null;
        }

        private void OnOutsidePointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_chrome.Visibility != Visibility.Visible || e.OriginalSource is not DependencyObject source)
            {
                return;
            }

            if (IsWithin(_textBox, source) || IsWithin(_chrome, source))
            {
                return;
            }

            _suppressShow = true;
            Hide();
        }

        private static bool IsWithin(DependencyObject ancestor, DependencyObject node)
        {
            var current = node;
            while (current is not null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private void Hide() => _chrome.Visibility = Visibility.Collapsed;

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var sanitized = SanitizeDigits(_textBox.Text);
            if (sanitized == _textBox.Text)
            {
                return;
            }

            var caret = _textBox.SelectionStart;
            _textBox.Text = sanitized;
            _textBox.SelectionStart = Math.Min(caret, sanitized.Length);
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

        private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key is VirtualKey.Escape && _chrome.Visibility == Visibility.Visible)
            {
                Dismiss();
                e.Handled = true;
                return;
            }

            if (e.Key is VirtualKey.Back)
            {
                Backspace();
                e.Handled = true;
                return;
            }

            if (e.Key is VirtualKey.Delete)
            {
                SetAmountText("");
                e.Handled = true;
                return;
            }

            var digit = e.Key switch
            {
                VirtualKey.Number0 or VirtualKey.NumberPad0 => "0",
                VirtualKey.Number1 or VirtualKey.NumberPad1 => "1",
                VirtualKey.Number2 or VirtualKey.NumberPad2 => "2",
                VirtualKey.Number3 or VirtualKey.NumberPad3 => "3",
                VirtualKey.Number4 or VirtualKey.NumberPad4 => "4",
                VirtualKey.Number5 or VirtualKey.NumberPad5 => "5",
                VirtualKey.Number6 or VirtualKey.NumberPad6 => "6",
                VirtualKey.Number7 or VirtualKey.NumberPad7 => "7",
                VirtualKey.Number8 or VirtualKey.NumberPad8 => "8",
                VirtualKey.Number9 or VirtualKey.NumberPad9 => "9",
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
            var text = _textBox.Text ?? string.Empty;
            var start = _textBox.SelectionStart;
            var length = _textBox.SelectionLength;
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
            var text = _textBox.Text ?? string.Empty;
            if (text.Length == 0)
            {
                return;
            }

            var start = _textBox.SelectionStart;
            var length = _textBox.SelectionLength;
            if (length > 0)
            {
                SetAmountText(text.Remove(start, length));
            }
            else if (start > 0)
            {
                SetAmountText(text.Remove(start - 1, 1));
                _textBox.SelectionStart = start - 1;
            }
            else
            {
                SetAmountText(text[..^1]);
            }
        }

        private void SetAmountText(string value)
        {
            _textBox.Text = value;
            _textBox.SelectionStart = value.Length;
        }
    }
}

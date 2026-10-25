using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Keirohi_App.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Keirohi_App;

public sealed partial class FareMemoryWindow : Window
{
    private readonly ObservableCollection<FareMemoryEditItem> rows = [];

    public FareMemoryWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(640, 520));
        foreach (var row in App.State.GetFareMemory())
        {
            rows.Add(new FareMemoryEditItem
            {
                Departure = row.Departure,
                Arrival = row.Arrival,
                AmountText = row.Amount.ToString(),
            });
        }

        RowList.ItemsSource = rows;
        UpdateStatus();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        rows.Add(new FareMemoryEditItem());
        UpdateStatus();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: FareMemoryEditItem row })
        {
            return;
        }

        rows.Remove(row);
        UpdateStatus();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var parsed = new List<FareMemoryRow>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var departure = row.Departure.Trim();
            var arrival = row.Arrival.Trim();
            var amountText = row.AmountText.Trim();
            if (departure.Length == 0 && arrival.Length == 0 && amountText.Length == 0)
            {
                continue;
            }

            if (departure.Length == 0 || arrival.Length == 0)
            {
                await ShowOkAsync("運賃の履歴", "出発と到着は両方入力してください。");
                return;
            }

            if (!int.TryParse(amountText, out var amount) || amount <= 0)
            {
                await ShowOkAsync("運賃の履歴", "金額は1円以上の数字で入力してください。");
                return;
            }

            var key = $"{departure}\u0001{arrival}";
            if (!seen.Add(key))
            {
                await ShowOkAsync("運賃の履歴", $"同じ区間が重複しています。\n{departure} → {arrival}");
                return;
            }

            parsed.Add(new FareMemoryRow
            {
                Departure = departure,
                Arrival = arrival,
                Amount = amount,
            });
        }

        App.State.ReplaceFareMemory(parsed);
        StatusText.Text = $"{parsed.Count} 件を保存しました。";
    }

    private void UpdateStatus() =>
        StatusText.Text = rows.Count == 0 ? "履歴はありません。" : $"{rows.Count} 件";

    private async Task ShowOkAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };
        await dialog.ShowAsync();
    }
}

public sealed class FareMemoryEditItem : INotifyPropertyChanged
{
    private string departure = "";
    private string arrival = "";
    private string amountText = "";

    public string Departure
    {
        get => departure;
        set
        {
            if (departure == value)
            {
                return;
            }

            departure = value;
            OnPropertyChanged();
        }
    }

    public string Arrival
    {
        get => arrival;
        set
        {
            if (arrival == value)
            {
                return;
            }

            arrival = value;
            OnPropertyChanged();
        }
    }

    public string AmountText
    {
        get => amountText;
        set
        {
            if (amountText == value)
            {
                return;
            }

            amountText = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

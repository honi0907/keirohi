using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Keirohi_App.Models;
using Keirohi.Avalonia.Helpers;

namespace Keirohi.Avalonia.Windows;

public partial class FareMemoryWindow : Window
{
    private readonly ObservableCollection<FareMemoryEditItem> rows = [];

    public FareMemoryWindow()
    {
        InitializeComponent();
        foreach (var row in AppHost.State.GetFareMemory())
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

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        rows.Add(new FareMemoryEditItem());
        UpdateStatus();
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: FareMemoryEditItem row })
        {
            return;
        }

        rows.Remove(row);
        UpdateStatus();
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
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
                await DialogService.ShowOkAsync("運賃の履歴", "出発と到着は両方入力してください。", this);
                return;
            }

            if (!int.TryParse(amountText, out var amount) || amount <= 0)
            {
                await DialogService.ShowOkAsync("運賃の履歴", "金額は1円以上の数字で入力してください。", this);
                return;
            }

            var key = $"{departure}\u0001{arrival}";
            if (!seen.Add(key))
            {
                await DialogService.ShowOkAsync("運賃の履歴", $"同じ区間が重複しています。\n{departure} → {arrival}", this);
                return;
            }

            parsed.Add(new FareMemoryRow
            {
                Departure = departure,
                Arrival = arrival,
                Amount = amount,
            });
        }

        AppHost.State.ReplaceFareMemory(parsed);
        StatusText.Text = $"{parsed.Count} 件を保存しました。";
    }

    private void UpdateStatus() =>
        StatusText.Text = rows.Count == 0 ? "履歴はありません。" : $"{rows.Count} 件";
}

public sealed class FareMemoryEditItem
{
    public string Departure { get; set; } = "";

    public string Arrival { get; set; } = "";

    public string AmountText { get; set; } = "";
}

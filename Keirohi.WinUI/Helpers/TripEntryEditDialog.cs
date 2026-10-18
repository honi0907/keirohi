using Keirohi_App.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Keirohi_App.Helpers;

public static class TripEntryEditDialog
{
    public static async Task<TripEntry?> ShowAsync(XamlRoot xamlRoot, TripEntry entry)
    {
        var datePicker = new CalendarDatePicker
        {
            Header = "日付",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            IsTodayHighlighted = true,
            PlaceholderText = "日付を選択",
        };
        IsoDateHelper.SetFromIso(datePicker, entry.Date);
        IsoDateHelper.EnsureTodayIfEmpty(datePicker);

        var destBox = new TextBox { Header = "目的地", Text = entry.Destination };
        var depBox = new TextBox { Header = "出発", Text = entry.Departure };
        var arrBox = new TextBox { Header = "到着", Text = entry.Arrival };
        var amountBox = new TextBox { Header = "金額（円）", Text = entry.Amount.ToString() };
        var noteBox = new TextBox { Header = "備考", Text = entry.Note, AcceptsReturn = true };
        var modeBox = new ComboBox
        {
            Header = "種別",
            SelectedIndex = entry.Mode == TransportMode.Bus ? 1 : 0,
        };
        modeBox.Items.Add(new ComboBoxItem { Content = "電車", Tag = "train" });
        modeBox.Items.Add(new ComboBoxItem { Content = "バス等", Tag = "bus" });

        var panel = new StackPanel { Spacing = 10, MinWidth = 360 };
        panel.Children.Add(datePicker);
        panel.Children.Add(destBox);
        panel.Children.Add(depBox);
        panel.Children.Add(arrBox);
        panel.Children.Add(modeBox);
        panel.Children.Add(amountBox);
        panel.Children.Add(noteBox);

        var dialog = new ContentDialog
        {
            Title = "区間を編集",
            Content = panel,
            PrimaryButtonText = "保存",
            CloseButtonText = "キャンセル",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return null;
        }

        if (!int.TryParse(amountBox.Text.Trim(), out var amount) || amount <= 0)
        {
            return null;
        }

        var date = IsoDateHelper.ToIsoString(datePicker.Date);
        if (string.IsNullOrEmpty(date))
        {
            return null;
        }

        var mode = modeBox.SelectedIndex == 1 ? TransportMode.Bus : TransportMode.Train;
        return new TripEntry
        {
            Id = entry.Id,
            Date = date,
            Destination = destBox.Text.Trim(),
            Departure = depBox.Text.Trim(),
            Arrival = arrBox.Text.Trim(),
            Note = noteBox.Text.Trim(),
            Amount = amount,
            Mode = mode,
        };
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Keirohi_App.Helpers;
using Keirohi_App.Models;

namespace Keirohi.Avalonia.Helpers;

public static class TripEntryEditDialog
{
    public static async Task<TripEntry?> ShowAsync(TripEntry entry)
    {
        var datePicker = new CalendarDatePicker
        {
            SelectedDate = IsoDateFormats.ParseIso(entry.Date)?.LocalDateTime.Date ?? DateTime.Today,
            MinHeight = 36,
            Watermark = "日付を選択",
            IsTodayHighlighted = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        IsoDateHelper.EnsureTodayIfEmpty(datePicker);

        var destBox = new TextBox { Watermark = "目的地", Text = entry.Destination };
        var depBox = new TextBox { Watermark = "出発", Text = entry.Departure };
        var arrBox = new TextBox { Watermark = "到着", Text = entry.Arrival };
        var amountBox = new TextBox { Watermark = "金額（円）", Text = entry.Amount.ToString() };
        AmountKeypadAttach.Enable(amountBox);
        var noteBox = new TextBox { Watermark = "備考", Text = entry.Note, AcceptsReturn = true, MinHeight = 60 };
        var modeBox = new ComboBox { SelectedIndex = entry.Mode == TransportMode.Bus ? 1 : 0 };
        modeBox.Items.Add(new ComboBoxItem { Content = "電車", Tag = "train" });
        modeBox.Items.Add(new ComboBoxItem { Content = "バス等", Tag = "bus" });

        var panel = new StackPanel
        {
            Spacing = 10,
            MinWidth = 360,
            Children =
            {
                datePicker,
                destBox,
                depBox,
                arrBox,
                modeBox,
                amountBox,
                noteBox,
            },
        };

        var owner = DialogService.Owner;
        if (owner is null)
        {
            return null;
        }

        var tcs = new TaskCompletionSource<TripEntry?>();
        var dialog = new Window
        {
            Title = "区間を編集",
            Width = 400,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel(),
        };

        var saveBtn = new Button { Content = "保存" };
        var cancelBtn = new Button { Content = "キャンセル" };
        saveBtn.Click += (_, _) =>
        {
            if (!int.TryParse(amountBox.Text?.Trim(), out var amount) || amount <= 0)
            {
                tcs.TrySetResult(null);
                dialog.Close();
                return;
            }

            var date = IsoDateHelper.ToIsoString(datePicker.SelectedDate);
            if (string.IsNullOrEmpty(date))
            {
                tcs.TrySetResult(null);
                dialog.Close();
                return;
            }

            var mode = modeBox.SelectedIndex == 1 ? TransportMode.Bus : TransportMode.Train;
            tcs.TrySetResult(new TripEntry
            {
                Id = entry.Id,
                Date = date,
                Destination = destBox.Text?.Trim() ?? "",
                Departure = depBox.Text?.Trim() ?? "",
                Arrival = arrBox.Text?.Trim() ?? "",
                Note = noteBox.Text?.Trim() ?? "",
                Amount = amount,
                Mode = mode,
            });
            dialog.Close();
        };
        cancelBtn.Click += (_, _) =>
        {
            tcs.TrySetResult(null);
            dialog.Close();
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 12, 0, 0),
            Children = { saveBtn, cancelBtn },
        };
        var root = new StackPanel { Margin = new Thickness(16), Children = { panel, buttons } };
        dialog.Content = root;
        dialog.Closed += (_, _) => tcs.TrySetResult(null);
        await dialog.ShowDialog(owner);
        return await tcs.Task;
    }
}

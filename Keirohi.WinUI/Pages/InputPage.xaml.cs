using Keirohi_App.Helpers;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Keirohi_App.Pages;

public sealed partial class InputPage : Page
{
    private readonly DispatcherTimer fareTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private int fareRequestId;
    private bool amountTouched;
    private bool suppressAmountTouched;

    public InputPage()
    {
        InitializeComponent();
        fareTimer.Tick += FareTimer_Tick;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RefreshLimitUi();
        IsoDateHelper.EnsureTodayIfEmpty(DatePicker);

        var lastDest = App.State.Entries.Count > 0
            ? App.State.Entries[^1].Destination
            : "";
        if (string.IsNullOrWhiteSpace(DestinationBox.Text))
        {
            DestinationBox.Text = lastDest;
        }
    }

    private void RefreshLimitUi()
    {
        var atLimit = App.State.Entries.Count >= AppStateService.MaxSuicaEntries;
        LimitText.Visibility = atLimit ? Visibility.Visible : Visibility.Collapsed;
        LimitText.Text = $"Suica明細は{AppStateService.MaxSuicaEntries}件までです。出力後に新しい期間として登録してください。";
    }

    private void StationFields_TextChanged(object sender, TextChangedEventArgs e)
    {
        amountTouched = false;
        ScheduleFareLookup();
    }

    private void ModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            return;
        }

        amountTouched = false;
        ScheduleFareLookup();
    }

    private void AmountBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (suppressAmountTouched)
        {
            return;
        }

        amountTouched = true;
        FareHint.Text = "";
    }

    private void SetAmountFromFare(int amount, string hint)
    {
        suppressAmountTouched = true;
        AmountBox.Text = amount.ToString();
        suppressAmountTouched = false;
        amountTouched = false;
        FareHint.Text = hint;
    }

    private void ScheduleFareLookup()
    {
        fareTimer.Stop();
        fareTimer.Start();
    }

    private async void FareTimer_Tick(object? sender, object e)
    {
        fareTimer.Stop();
        if (amountTouched || !App.State.Settings.AutoFillFareEnabled)
        {
            if (!App.State.Settings.AutoFillFareEnabled)
            {
                FareHint.Text = "";
            }

            return;
        }

        var dep = DepartureBox.Text.Trim();
        var arr = ArrivalBox.Text.Trim();
        if (string.IsNullOrEmpty(dep) || string.IsNullOrEmpty(arr))
        {
            FareHint.Text = "";
            return;
        }

        var requestId = ++fareRequestId;
        var index = App.State.GetFareIndex();
        var fromMemory = index.Lookup(dep, arr);
        if (requestId != fareRequestId)
        {
            return;
        }

        if (fromMemory is not null)
        {
            SetAmountFromFare(fromMemory.Value, "運賃: 記憶から自動入力");
            return;
        }

        var mode = GetSelectedMode();
        var key = App.State.Settings.EkispertApiKey;
        if (mode == TransportMode.Train && !string.IsNullOrWhiteSpace(key))
        {
            var fromApi = await EkispertFareService.FetchFareAsync(dep, arr, key);
            if (requestId != fareRequestId)
            {
                return;
            }

            if (fromApi is not null)
            {
                SetAmountFromFare(fromApi.Value, "運賃: 駅すぱあと API から自動入力");
                return;
            }
        }

        FareHint.Text = "";
    }

    private void AmountBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!amountTouched)
        {
            return;
        }

        var dep = DepartureBox.Text.Trim();
        var arr = ArrivalBox.Text.Trim();
        if (string.IsNullOrEmpty(dep) || string.IsNullOrEmpty(arr))
        {
            return;
        }

        if (int.TryParse(AmountBox.Text.Trim(), out var amount) && amount > 0)
        {
            App.State.RememberFare(dep, arr, amount);
        }
    }

    private TransportMode GetSelectedMode()
    {
        if (ModeBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            return tag == "bus" ? TransportMode.Bus : TransportMode.Train;
        }
        return TransportMode.Train;
    }

    /// <summary>右列の「明細に登録」から呼ばれる。成功時は表示用メッセージ、失敗時は null（ダイアログ済み）。</summary>
    public async Task<string?> TryRegisterEntryAsync()
    {
        if (await SuicaEntryLimitHelper.BlockIfAtLimitWithExportOfferAsync(XamlRoot))
        {
            return null;
        }

        if (!int.TryParse(AmountBox.Text.Trim(), out var amount) || amount <= 0)
        {
            await ShowDialogAsync("入力エラー", "金額を正しく入力してください。");
            return null;
        }

        var date = IsoDateHelper.ToIsoString(DatePicker.Date);
        var destination = DestinationBox.Text.Trim();
        var departure = DepartureBox.Text.Trim();
        var arrival = ArrivalBox.Text.Trim();
        if (string.IsNullOrEmpty(date))
        {
            await ShowDialogAsync("入力エラー", "日付を選択してください。");
            return null;
        }

        if (string.IsNullOrEmpty(destination) || string.IsNullOrEmpty(departure) || string.IsNullOrEmpty(arrival))
        {
            await ShowDialogAsync("入力エラー", "目的地・出発・到着は必須です。");
            return null;
        }

        var wasBelowLimit = App.State.Entries.Count < AppStateService.MaxSuicaEntries;
        App.State.AddEntry(new TripEntry
        {
            Date = date,
            Destination = destination,
            Departure = departure,
            Arrival = arrival,
            Note = NoteBox.Text.Trim(),
            Amount = amount,
            Mode = GetSelectedMode(),
        });

        DepartureBox.Text = "";
        ArrivalBox.Text = "";
        AmountBox.Text = "";
        NoteBox.Text = "";
        FareHint.Text = "";
        amountTouched = false;
        DestinationBox.Text = destination;
        RefreshLimitUi();
        if (wasBelowLimit && App.State.Entries.Count >= AppStateService.MaxSuicaEntries)
        {
            await SuicaEntryLimitHelper.OfferExportAfterFillAsync(
                XamlRoot,
                "これ以上は登録できません。出力してから新しいセットで続けてください。");
        }

        return "1件を明細に登録しました。";
    }

    private async void OpenYahooTransit_Click(object sender, RoutedEventArgs e)
    {
        await TransitSearchLauncher.OpenYahooTransitAsync(
            XamlRoot,
            DepartureBox.Text,
            ArrivalBox.Text,
            GetSelectedMode());
    }

    private static async Task ShowDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = App.MainWindowInstance!.Content.XamlRoot,
        };
        await dialog.ShowAsync();
    }
}

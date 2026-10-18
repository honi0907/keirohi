using System.ComponentModel;
using System.Runtime.CompilerServices;
using Keirohi_App.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Keirohi_App.Models;

[Microsoft.UI.Xaml.Data.Bindable]
public sealed class TripEntryItem : INotifyPropertyChanged
{
    private bool isSelected;
    private Brush rowBackground = EntryRowBackgroundBrushes.Default;

    public string Id { get; set; } = "";
    public string RouteSummary { get; set; } = "";
    public string AmountText { get; set; } = "";
    public string Line2 { get; set; } = "";

    public Brush RowBackground
    {
        get => rowBackground;
        set => SetProperty(ref rowBackground, value);
    }

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected == value)
            {
                return;
            }

            isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectionBorderBrush));
            OnPropertyChanged(nameof(SelectionBorderThickness));
        }
    }

    public SolidColorBrush SelectionBorderBrush =>
        ListRowSelectionChrome.BorderBrush(IsSelected);

    public Thickness SelectionBorderThickness =>
        ListRowSelectionChrome.BorderThickness(IsSelected);

    public event PropertyChangedEventHandler? PropertyChanged;

    public static TripEntryItem From(TripEntry entry)
    {
        var mode = entry.Mode == TransportMode.Bus ? "バス等" : "電車";
        return new TripEntryItem
        {
            Id = entry.Id,
            RouteSummary = $"{entry.Date}  {entry.Departure} → {entry.Arrival}",
            AmountText = $"¥{entry.Amount:N0}",
            Line2 = $"{mode} / 目的地: {entry.Destination}  {entry.Note}",
        };
    }

    private void SetProperty(ref Brush field, Brush value, [CallerMemberName] string? propertyName = null)
    {
        if (ReferenceEquals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

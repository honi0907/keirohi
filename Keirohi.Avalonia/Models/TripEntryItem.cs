using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Keirohi_App.Models;
using Keirohi.Avalonia.Helpers;
using static Keirohi.Avalonia.Helpers.EntryRowBackgroundBrushes;

namespace Keirohi.Avalonia.Models;

public sealed class TripEntryItem : INotifyPropertyChanged
{
    private bool isSelected;
    private IBrush rowBackground = Default;

    public string Id { get; set; } = "";
    public string RouteSummary { get; set; } = "";
    public string AmountText { get; set; } = "";
    public string Line2 { get; set; } = "";

    public bool FromMobileSuicaPdf { get; init; }

    /// <summary>一覧の並びで日付が前の行より古い（要修正）。</summary>
    public bool HasDateOrderWarning { get; set; }

    /// <summary>日付グループの先頭行など、行間用の外側余白。</summary>
    public Thickness ItemMargin { get; set; }

    public IBrush RowBackground
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

    public IBrush SelectionBorderBrush => ListRowSelectionChrome.BorderBrush(IsSelected);

    public Thickness SelectionBorderThickness => ListRowSelectionChrome.BorderThickness(IsSelected);

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
            FromMobileSuicaPdf = entry.FromMobileSuicaPdf,
        };
    }

    private void SetProperty(ref IBrush field, IBrush value, [CallerMemberName] string? propertyName = null)
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

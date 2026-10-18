using System.ComponentModel;
using System.Runtime.CompilerServices;
using Keirohi_App.Helpers;
using Keirohi_App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Keirohi_App.Models;

[Microsoft.UI.Xaml.Data.Bindable]
public sealed class RoutePatternListRow : INotifyPropertyChanged
{
    private static readonly SolidColorBrush TransparentBorderBrush = new(Windows.UI.Color.FromArgb(0, 0, 0, 0));
    private static readonly SolidColorBrush RowOutlineBrush = new(Windows.UI.Color.FromArgb(255, 180, 180, 180));

    private string title = "";
    private string detail = "";
    private string amountText = "";
    private bool isSelected;
    private SolidColorBrush rowBackgroundBrush = RoutePatternListColorHelper.ToRowBackgroundBrush(null);
    private SolidColorBrush accentBarBrush = RoutePatternListColorHelper.ToAccentBarBrush(null);
    private static SolidColorBrush? selectionBorderBrush;

    public RoutePatternListRow(RoutePattern pattern)
    {
        Pattern = pattern;
        Refresh();
    }

    public RoutePattern Pattern { get; }

    public string Title
    {
        get => title;
        private set => SetProperty(ref title, value);
    }

    public string Detail
    {
        get => detail;
        private set => SetProperty(ref detail, value);
    }

    public string AmountText
    {
        get => amountText;
        private set => SetProperty(ref amountText, value);
    }

    public SolidColorBrush RowBackgroundBrush
    {
        get => rowBackgroundBrush;
        private set => SetProperty(ref rowBackgroundBrush, value);
    }

    public SolidColorBrush AccentBarBrush
    {
        get => accentBarBrush;
        private set => SetProperty(ref accentBarBrush, value);
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionBorderBrush)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionBorderThickness)));
        }
    }

    public SolidColorBrush SelectionBorderBrush =>
        IsSelected ? GetSelectionBorderBrush() : RowOutlineBrush;

    public Thickness SelectionBorderThickness => IsSelected ? new Thickness(2) : new Thickness(1);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh(FareIndexService? fareIndex = null, RoutePattern? amountSource = null)
    {
        var legCount = Pattern.Legs.Count(l =>
            !string.IsNullOrWhiteSpace(l.Departure) && !string.IsNullOrWhiteSpace(l.Arrival));
        Title = string.IsNullOrWhiteSpace(Pattern.Name)
            ? "（名称未設定）"
            : Pattern.Name.Trim();
        var legLabel = legCount == 0 ? "区間未入力" : $"{legCount} 区間";
        var saveState = App.RoutePatternDraft.GetPatternSaveState(Pattern);
        Detail = saveState switch
        {
            PatternDraftSaveState.New => $"{legLabel} · 新規（未保存）",
            PatternDraftSaveState.Modified => $"{legLabel} · 変更あり（未保存）",
            PatternDraftSaveState.EmptyDraft => legLabel,
            _ => legLabel,
        };
        AmountText = RoutePatternFareSummaryHelper.FormatListAmount(amountSource ?? Pattern, fareIndex);
        RowBackgroundBrush = RoutePatternListColorHelper.ToRowBackgroundBrush(Pattern.ListColor);
        AccentBarBrush = RoutePatternListColorHelper.ToAccentBarBrush(Pattern.ListColor);
    }

    public static RoutePatternListRow From(RoutePattern pattern) => new(pattern);

    private static SolidColorBrush GetSelectionBorderBrush()
    {
        if (selectionBorderBrush is not null)
        {
            return selectionBorderBrush;
        }

        if (Application.Current.Resources.TryGetValue("SystemAccentColor", out var resource) &&
            resource is Windows.UI.Color accent)
        {
            selectionBorderBrush = new SolidColorBrush(accent);
            return selectionBorderBrush;
        }

        selectionBorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 212));
        return selectionBorderBrush;
    }

    private void SetProperty(ref string field, string value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetProperty(ref SolidColorBrush field, SolidColorBrush value, [CallerMemberName] string? propertyName = null)
    {
        if (field.Color.Equals(value.Color))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

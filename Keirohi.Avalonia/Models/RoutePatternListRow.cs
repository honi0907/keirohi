using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Keirohi_App.Helpers;
using Keirohi_App.Models;
using Keirohi_App.Services;
using Keirohi.Avalonia.Helpers;

namespace Keirohi.Avalonia.Models;

public sealed class RoutePatternListRow : INotifyPropertyChanged
{
    private string title = "";
    private string detail = "";
    private string amountText = "";
    private bool isSelected;
    private IBrush rowBackgroundBrush = RoutePatternListColorHelper.ToRowBackgroundBrush(null);
    private IBrush accentBarBrush = RoutePatternListColorHelper.ToAccentBarBrush(null);

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

    public IBrush RowBackgroundBrush
    {
        get => rowBackgroundBrush;
        private set => SetProperty(ref rowBackgroundBrush, value);
    }

    public IBrush AccentBarBrush
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

    public IBrush SelectionBorderBrush =>
        IsSelected ? ListRowSelectionChrome.BorderBrush(true) : ListRowSelectionChrome.RowOutlineBrush;

    public Thickness SelectionBorderThickness => ListRowSelectionChrome.BorderThickness(IsSelected);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh(FareIndexService? fareIndex = null, RoutePattern? amountSource = null)
    {
        var legCount = Pattern.Legs.Count(l =>
            !string.IsNullOrWhiteSpace(l.Departure) && !string.IsNullOrWhiteSpace(l.Arrival));
        Title = string.IsNullOrWhiteSpace(Pattern.Name)
            ? "（名称未設定）"
            : Pattern.Name.Trim();
        var legLabel = legCount == 0 ? "区間未入力" : $"{legCount} 区間";
        var saveState = AppHost.RoutePatternDraft.GetPatternSaveState(Pattern);
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

    private void SetProperty(ref string field, string value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetProperty(ref IBrush field, IBrush value, [CallerMemberName] string? propertyName = null)
    {
        if (ReferenceEquals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Keirohi_App.Models;

namespace Keirohi.Avalonia.Models;

public sealed class MobileSuicaPdfImportRowItem : INotifyPropertyChanged
{
    private bool isChecked = true;

    public MobileSuicaPdfImportRowItem(TripEntry entry, string line)
    {
        Entry = entry;
        Line = line;
    }

    public TripEntry Entry { get; }

    public string Line { get; }

    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if (isChecked == value)
            {
                return;
            }

            isChecked = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

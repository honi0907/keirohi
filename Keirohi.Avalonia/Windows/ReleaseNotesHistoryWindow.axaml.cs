using Avalonia.Controls;
using Avalonia.Interactivity;
using Keirohi_App.Updates;

namespace Keirohi.Avalonia.Windows;

public partial class ReleaseNotesHistoryWindow : Window
{
    public ReleaseNotesHistoryWindow()
    {
        InitializeComponent();
        VersionList.ItemsSource = ReleaseNotesCatalog.GetAll()
            .OrderByDescending(v => v.Version, Comparer<string>.Create(VersionComparer.Compare))
            .Select(v =>
            {
                var date = string.IsNullOrWhiteSpace(v.Date) ? "" : $" — {v.Date}";
                var title = string.IsNullOrWhiteSpace(v.Title) ? "" : $"　{v.Title}";
                return new ReleaseNotesHistoryItem
                {
                    Heading = $"v{v.Version}{date}{title}",
                    Items = v.Items,
                };
            })
            .ToList();
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    private sealed class ReleaseNotesHistoryItem
    {
        public required string Heading { get; init; }

        public IReadOnlyList<string> Items { get; init; } = [];
    }
}

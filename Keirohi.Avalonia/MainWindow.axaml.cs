using Avalonia.Controls;
using Avalonia.Interactivity;
using Keirohi_App.Helpers;
using Keirohi.Avalonia.Services;
using Keirohi.Avalonia.Views;

namespace Keirohi.Avalonia;

public partial class MainWindow : Window
{
    private const double PreviewPaneWidth = 520;
    private const double NavPaneWidth = 188;
    private const double PatternUnifiedEditorWidth = 672;

    private readonly InputView inputView = new();
    private readonly ExportView exportView = new();
    private readonly PatternsView patternsView = new();
    private readonly SettingsView settingsView = new();
    private bool initialized;

    public MainWindow()
    {
        InitializeComponent();
        Opened += MainWindow_Opened;
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        AppHost.MainWindowInstance = this;
        AppHost.State.DataPersisted += (_, _) => UpdateLastSavedLabel();
        UpdateLastSavedLabel();
        NavList.SelectedIndex = 0;
        UpdatePatternPane("input");
        PageHost.Content = inputView;
        RegistrationPreview.SetRegisterButton(RegisterToEntriesButton);
        ConfigureSharedRegistration("input");
        _ = OnlineUpdateUiHelper.PromptOnStartupAsync();
    }

    private void UpdateLastSavedLabel()
    {
        LastSavedText.Text = DataPersistenceStatusHelper.FormatLastSavedLabel(AppHost.State.DataLastWrittenAt);
        ToolTip.SetTip(LastSavedText, DataPersistenceStatusHelper.DataFolderToolTip);
    }

    private void ConfigureSharedRegistration(string? navTag)
    {
        RegistrationPreview.ConfigureRegistration(
            navTag,
            navTag switch
            {
                "input" => RegisterFromInputAsync,
                "patterns" => RegisterFromPatternAsync,
                _ => null,
            });
    }

    private async Task RegisterFromInputAsync()
    {
        if (PageHost.Content is not InputView inputPage)
        {
            return;
        }

        var message = await inputPage.TryRegisterEntryAsync();
        if (message is not null)
        {
            RegistrationPreview.ShowRegisterMessage(message);
        }
    }

    private async Task RegisterFromPatternAsync()
    {
        var message = await RegistrationPreview.TryApplyPatternToEntriesAsync();
        if (message is not null)
        {
            RegistrationPreview.ShowRegisterMessage(message);
        }
    }

    private void NavList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not ListBoxItem item || item.Tag is not string tag)
        {
            return;
        }

        UpdatePatternPane(tag);
        ConfigureSharedRegistration(tag);
        PageHost.Content = tag switch
        {
            "input" => inputView,
            "export" => exportView,
            "patterns" => patternsView,
            "settings" => settingsView,
            _ => inputView,
        };

        if (tag == "input")
        {
            inputView.OnNavigatedTo();
        }
        else if (tag == "export")
        {
            exportView.OnNavigatedTo();
        }
        else if (tag == "settings")
        {
            settingsView.OnNavigatedTo();
        }
    }

    private void UpdatePatternPane(string? tag)
    {
        var show = tag == "patterns";
        PatternPaneSeparator.IsVisible = show;
        PatternListPane.IsVisible = show;
        PageHost.IsVisible = !show;
        PatternEditorHost.IsVisible = show;
        SavePatternButton.IsVisible = show;

        var showEditorColumnTitle = tag is "input" or "patterns" or "export";
        EditorColumnTitle.IsVisible = showEditorColumnTitle;
        EditorColumnTitle.Text = tag switch
        {
            "input" => "区間を入力",
            "patterns" => "編集",
            "export" => "ファイル出力",
            _ => EditorColumnTitle.Text ?? "",
        };
    }

    private async void SavePatternButton_Click(object? sender, RoutedEventArgs e) =>
        await PatternEditorHost.SavePatternsAsync();

    private async void RegisterToEntriesButton_Click(object? sender, RoutedEventArgs e) =>
        await RegistrationPreview.PerformRegisterAsync();

    private async void PdfSaveButton_Click(object? sender, RoutedEventArgs e) =>
        await RegistrationPreview.OpenPdfPreviewAsync();

    public void NavigateToNavTag(string tag)
    {
        foreach (var item in NavList.Items.OfType<ListBoxItem>())
        {
            if (string.Equals(item.Tag as string, tag, StringComparison.Ordinal))
            {
                NavList.SelectedItem = item;
                return;
            }
        }
    }

    private static double ContentWidth => NavPaneWidth + PatternUnifiedEditorWidth + PreviewPaneWidth;
}

using System.Linq;
using Keirohi_App.Helpers;
using Keirohi_App.Pages;
using Keirohi_App.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Keirohi_App;

public sealed partial class MainWindow : Window
{
    /// <summary>入力・出力・設定のフォーム幅。右の余りは空白。</summary>
    private const double PatternEditorPaneWidth = 332;

    /// <summary>パターン編集（経路＋運賃を1枚にまとめた列）。</summary>
    private const double PatternUnifiedEditorWidth = 672;

    private const double PreviewPaneWidth = 520;

    private const double NavPaneWidth = 188;

    /// <summary>パターン保存ボタンが切れない程度（旧 820 から微増）。</summary>
    private const int DefaultWindowHeight = 840;

    private const int MinimumWindowHeight = 640;

    private bool initialized;

    public MainWindow()
    {
        InitializeComponent();
        NavFrame.Width = PatternEditorPaneWidth;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");
        Activated += MainWindow_Activated;
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = MinWindowWidth;
            presenter.PreferredMinimumHeight = MinimumWindowHeight;
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(MinWindowWidth, DefaultWindowHeight));
        App.State.DataPersisted += (_, _) => UpdateLastSavedLabel();
        UpdateLastSavedLabel();
        UpdatePatternPane("input");
        NavFrame.Navigate(typeof(InputPage));
        RegistrationPreview.SetRegisterButton(RegisterToEntriesButton);
        ConfigureSharedRegistration("input");
        DispatcherQueue.TryEnqueue(() =>
        {
            _ = OnlineUpdateUiHelper.PromptOnStartupAsync(Content.XamlRoot);
        });
    }

    private void UpdateLastSavedLabel()
    {
        LastSavedText.Text = DataPersistenceStatusHelper.FormatLastSavedLabel(App.State.DataLastWrittenAt);
        ToolTipService.SetToolTip(LastSavedText, DataPersistenceStatusHelper.DataFolderToolTip);
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
        if (NavView.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag as string;
            UpdatePatternPane(tag);
            ConfigureSharedRegistration(tag);
        }
    }

    private void ConfigureSharedRegistration(string? navTag)
    {
        RegistrationPreview.ConfigureRegistration(
            navTag,
            navTag switch
            {
                "input" => RegisterFromInputPageAsync,
                "patterns" => RegisterFromPatternAsync,
                _ => null,
            });
    }

    private async Task RegisterFromInputPageAsync()
    {
        if (NavFrame.Content is not InputPage inputPage)
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

    private async void SavePatternButton_Click(object sender, RoutedEventArgs e) =>
        await PatternEditorHost.SavePatternsAsync();

    private async void RegisterToEntriesButton_Click(object sender, RoutedEventArgs e) =>
        await RegistrationPreview.PerformRegisterAsync();

    private async void PdfSaveButton_Click(object sender, RoutedEventArgs e) =>
        await RegistrationPreview.OpenPdfPreviewAsync();

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        NavFrame.GoBack();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag as string;
            UpdatePatternPane(tag);
            ConfigureSharedRegistration(tag);
            switch (item.Tag)
            {
                case "input":
                    NavFrame.Navigate(typeof(InputPage));
                    break;
                case "export":
                    NavFrame.Navigate(typeof(ExportPage));
                    break;
                case "patterns":
                    NavFrame.Navigate(typeof(PatternsPage));
                    break;
                case "settings":
                    NavFrame.Navigate(typeof(SettingsPage));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown navigation tag: {item.Tag}");
            }
        }
    }

    private void UpdatePatternPane(string? tag)
    {
        var show = tag == "patterns" && NavView.IsPaneOpen;
        var visibility = show ? Visibility.Visible : Visibility.Collapsed;
        PatternPaneSeparator.Visibility = visibility;
        PatternListPane.Visibility = visibility;

        NavFrame.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        PatternEditorHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        SavePatternButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        var showEditorColumnTitle = tag is "input" or "patterns" or "export";
        EditorColumnTitle.Visibility = showEditorColumnTitle ? Visibility.Visible : Visibility.Collapsed;
        EditorColumnTitle.Text = tag switch
        {
            "input" => "区間を入力",
            "patterns" => "編集",
            "export" => "ファイル出力",
            _ => EditorColumnTitle.Text,
        };

        EditorColumn.Width = new GridLength(PatternUnifiedEditorWidth);
        FareColumn.Width = new GridLength(0);
        ContentLayout.Width = ContentWidth;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = MinWindowWidth;
        }

        var size = AppWindow.Size;
        if (size.Width < MinWindowWidth)
        {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(MinWindowWidth, Math.Max(size.Height, MinimumWindowHeight)));
        }
    }

    private static int MinWindowWidth => (int)Math.Ceiling(ContentWidth + 24);

    private static double ContentWidth => NavPaneWidth + PatternUnifiedEditorWidth + PreviewPaneWidth;

    public void NavigateToNavTag(string tag)
    {
        foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            if (string.Equals(item.Tag as string, tag, StringComparison.Ordinal))
            {
                NavView.SelectedItem = item;
                return;
            }
        }
    }
}

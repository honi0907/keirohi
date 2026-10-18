using Keirohi_App.Services;
using Microsoft.UI.Xaml;

namespace Keirohi_App;

public partial class App : Application
{
    public static AppStateService State { get; } = new();

    public static RoutePatternDraftService RoutePatternDraft { get; } = new();

    public static MainWindow? MainWindowInstance { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindowInstance = new MainWindow();
        MainWindowInstance.Activate();
    }
}

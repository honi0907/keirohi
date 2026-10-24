using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
#if !WINDOWS
using AvaloniaWebView;
#endif

namespace Keirohi.Avalonia;

public partial class App : global::Avalonia.Application
{
    public override void RegisterServices()
    {
        base.RegisterServices();
#if !WINDOWS
        AvaloniaWebViewBuilder.Initialize(default);
#endif
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var main = new MainWindow();
            AppHost.MainWindowInstance = main;
            desktop.MainWindow = main;
        }

        base.OnFrameworkInitializationCompleted();
    }
}

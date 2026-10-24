using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Keirohi.Avalonia.Helpers;
#if !WINDOWS
using AvaloniaWebView;
#endif

namespace Keirohi.Avalonia;

public partial class App : global::Avalonia.Application
{
    public App()
    {
        Name = "通勤経路費";
    }

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
        AppThemeHelper.Apply(AppHost.State.Settings);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var main = new MainWindow();
            AppHost.MainWindowInstance = main;
            desktop.MainWindow = main;
        }

        base.OnFrameworkInitializationCompleted();
    }
}

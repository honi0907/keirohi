using System.Text;
using Avalonia;
#if !WINDOWS
using Avalonia.WebView.Desktop;
#endif

namespace Keirohi.Avalonia;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
#if !WINDOWS
        builder = builder.UseDesktopWebView();
#endif
        return builder;
    }
}

using System.Diagnostics;

namespace Keirohi.Avalonia.Helpers;

internal static class BrowserLauncher
{
    public static void OpenUri(Uri uri)
    {
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}

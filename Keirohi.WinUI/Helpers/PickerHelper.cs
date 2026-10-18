using WinRT.Interop;

namespace Keirohi_App.Helpers;

public static class PickerHelper
{
    public static void BindToWindow(object picker, Microsoft.UI.Xaml.Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);
    }
}

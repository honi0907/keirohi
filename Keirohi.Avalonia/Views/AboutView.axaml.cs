using System.Reflection;
using Avalonia.Controls;

namespace Keirohi.Avalonia.Views;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null ? "" : $"バージョン {version.Major}.{version.Minor}.{version.Build}";
    }
}

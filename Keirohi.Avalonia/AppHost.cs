using Keirohi_App.Services;

namespace Keirohi.Avalonia;

public static class AppHost
{
    public static AppStateService State { get; } = new();

    public static RoutePatternDraftService RoutePatternDraft { get; } = new(() => State);

    public static MainWindow? MainWindowInstance { get; set; }
}

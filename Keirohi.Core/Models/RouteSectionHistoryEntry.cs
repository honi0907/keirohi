namespace Keirohi_App.Models;

public sealed class RouteSectionHistoryEntry
{
    public TransportMode Mode { get; init; }

    public string Departure { get; init; } = "";

    public string Arrival { get; init; } = "";

    public int Amount { get; init; }

    public string DisplayLine
    {
        get
        {
            var mode = Mode == TransportMode.Bus ? "バス等" : "電車";
            var route = Mode == TransportMode.Bus
                ? Departure
                : $"{Departure} → {Arrival}";
            return $"{mode}  {route}  ¥{Amount:N0}";
        }
    }
}

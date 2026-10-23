using Keirohi_App.Models;

namespace Keirohi_App.Helpers;

public static class YahooTransitSearchHelper
{
    private const string SearchResultBase = "https://transit.yahoo.co.jp/search/result";

    public static Uri BuildSearchUri(string departure, string arrival, TransportMode mode = TransportMode.Train)
    {
        var from = departure.Trim();
        var to = arrival.Trim();
        var query = $"from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}";
        query += mode == TransportMode.Bus
            ? "&lb=1&hb=1&shin=0&ex=0&al=0&sr=0"
            : "&shin=1&ex=1&lb=1&hb=1";
        return new Uri($"{SearchResultBase}?{query}");
    }

    public static bool TryBuildSearchUri(
        string departure,
        string arrival,
        out Uri uri,
        TransportMode mode = TransportMode.Train)
    {
        uri = default!;
        if (string.IsNullOrWhiteSpace(departure) || string.IsNullOrWhiteSpace(arrival))
        {
            return false;
        }

        uri = BuildSearchUri(departure, arrival, mode);
        return true;
    }
}

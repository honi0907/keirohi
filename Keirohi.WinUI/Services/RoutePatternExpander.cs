using Keirohi_App.Models;

namespace Keirohi_App.Services;

public sealed class RoutePatternExpandResult
{
    public List<TripEntry> Entries { get; } = [];
    public List<string> UnresolvedLegs { get; } = [];
}

public static class RoutePatternExpander
{
    public static async Task<RoutePatternExpandResult> ExpandAsync(
        RoutePattern pattern,
        string date,
        string? destinationOverride,
        AppSettings settings,
        FareIndexService fareIndex)
    {
        var result = new RoutePatternExpandResult();
        var destination = string.IsNullOrWhiteSpace(destinationOverride)
            ? pattern.Name.Trim()
            : destinationOverride.Trim();

        foreach (var leg in pattern.Legs)
        {
            var departure = leg.Departure.Trim();
            var arrival = leg.Arrival.Trim();
            if (string.IsNullOrEmpty(departure) || string.IsNullOrEmpty(arrival))
            {
                continue;
            }

            int? amount = leg.Amount > 0 ? leg.Amount : null;
            amount ??= await FareResolver.ResolveAmountAsync(
                departure,
                arrival,
                leg.Mode,
                fareIndex,
                settings.AutoFillFareEnabled,
                settings.EkispertApiKey);

            if (amount is null or <= 0)
            {
                result.UnresolvedLegs.Add($"{departure}→{arrival}");
                continue;
            }

            result.Entries.Add(new TripEntry
            {
                Date = date,
                Destination = destination,
                Departure = departure,
                Arrival = arrival,
                Note = leg.Note.Trim(),
                Amount = amount.Value,
                Mode = leg.Mode,
            });
        }

        return result;
    }
}

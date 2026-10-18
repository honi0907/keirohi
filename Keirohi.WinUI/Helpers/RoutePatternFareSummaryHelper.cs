using Keirohi_App.Models;
using Keirohi_App.Services;

namespace Keirohi_App.Helpers;

public static class RoutePatternFareSummaryHelper
{
    public static string FormatListAmount(RoutePattern pattern, FareIndexService? fareIndex)
    {
        var (total, filledLegs, resolvedLegs) = Summarize(pattern, fareIndex);
        if (filledLegs == 0)
        {
            return "";
        }

        if (total is null)
        {
            return "—";
        }

        var formatted = $"¥{total.Value:N0}";
        return resolvedLegs < filledLegs ? $"{formatted}+" : formatted;
    }

    public static (int? Total, int FilledLegs, int ResolvedLegs) Summarize(
        RoutePattern pattern,
        FareIndexService? fareIndex)
    {
        var sum = 0;
        var filled = 0;
        var resolved = 0;

        foreach (var leg in pattern.Legs)
        {
            if (string.IsNullOrWhiteSpace(leg.Departure) || string.IsNullOrWhiteSpace(leg.Arrival))
            {
                continue;
            }

            filled++;
            if (leg.Amount > 0)
            {
                sum += leg.Amount;
                resolved++;
                continue;
            }

            if (fareIndex?.Lookup(leg.Departure, leg.Arrival) is int estimated && estimated > 0)
            {
                sum += estimated;
                resolved++;
            }
        }

        if (filled == 0)
        {
            return (null, 0, 0);
        }

        if (resolved == 0)
        {
            return (null, filled, 0);
        }

        return (sum, filled, resolved);
    }
}

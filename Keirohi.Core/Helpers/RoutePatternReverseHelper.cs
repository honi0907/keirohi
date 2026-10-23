using Keirohi_App.Models;

namespace Keirohi_App.Helpers;

public static class RoutePatternReverseHelper
{
    private static readonly string[] NameSeparators = ["→", "->", "⇒", "－>", "‐>"];

    public static string ReverseName(string name)
    {
        var trimmed = name.Trim();
        foreach (var sep in NameSeparators)
        {
            var index = trimmed.IndexOf(sep, StringComparison.Ordinal);
            if (index < 0)
            {
                continue;
            }

            var left = trimmed[..index].Trim();
            var right = trimmed[(index + sep.Length)..].Trim();
            if (left.Length > 0 && right.Length > 0)
            {
                return $"{right}→{left}";
            }
        }

        return trimmed.Length > 0 ? $"{trimmed}（戻り）" : "（戻り）";
    }

    public static RoutePattern CreateReverse(RoutePattern source)
    {
        var legs = source.Legs
            .Where(l => !string.IsNullOrWhiteSpace(l.Departure) || !string.IsNullOrWhiteSpace(l.Arrival))
            .Reverse()
            .Select(l => new RoutePatternLeg
            {
                Departure = l.Arrival,
                Arrival = l.Departure,
                Mode = l.Mode,
                Note = l.Note,
                Amount = l.Amount,
            })
            .ToList();

        if (legs.Count == 0)
        {
            legs.Add(new RoutePatternLeg());
        }

        return new RoutePattern
        {
            Name = ReverseName(source.Name),
            Legs = legs,
        };
    }

    public static void ApplyInPlace(RoutePattern target)
    {
        var reversed = CreateReverse(target);
        target.Name = reversed.Name;
        target.Legs = reversed.Legs;
    }
}

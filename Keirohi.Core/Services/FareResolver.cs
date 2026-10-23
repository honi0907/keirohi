using Keirohi_App.Models;

namespace Keirohi_App.Services;

public static class FareResolver
{
    public static async Task<int?> ResolveAmountAsync(
        string departure,
        string arrival,
        TransportMode mode,
        FareIndexService fareIndex,
        bool autoFillEnabled,
        string? ekispertApiKey)
    {
        if (!autoFillEnabled)
        {
            return null;
        }

        var dep = departure.Trim();
        var arr = arrival.Trim();
        if (string.IsNullOrEmpty(dep) || string.IsNullOrEmpty(arr))
        {
            return null;
        }

        var fromMemory = fareIndex.Lookup(dep, arr);
        if (fromMemory is not null)
        {
            return fromMemory;
        }

        if (mode == TransportMode.Train && !string.IsNullOrWhiteSpace(ekispertApiKey))
        {
            return await EkispertFareService.FetchFareAsync(dep, arr, ekispertApiKey);
        }

        return null;
    }
}

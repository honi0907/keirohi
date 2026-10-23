using System.Net;
using System.Text.Json;

namespace Keirohi_App.Services;

public static class EkispertFareService
{
    private const string ApiBase = "https://api.ekispert.jp/v1/json";

    public static async Task<int?> FetchFareAsync(string departure, string arrival, string apiKey)
    {
        var key = apiKey.Trim();
        if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(departure) || string.IsNullOrWhiteSpace(arrival))
        {
            return null;
        }

        var viaList = $"{departure.Trim()}:{arrival.Trim()}";
        var url =
            $"{ApiBase}/search/course/extreme?key={Uri.EscapeDataString(key)}&viaList={Uri.EscapeDataString(viaList)}&sort=price";
        var response = await GetJsonAsync(url);
        if (response.Document is null)
        {
            return null;
        }

        using (response.Document)
        {
            if (!response.Document.RootElement.TryGetProperty("ResultSet", out var resultSet))
            {
                return null;
            }

            if (TryGetApiErrorMessage(resultSet) is not null)
            {
                return null;
            }

            return TryParseFareFromResultSet(resultSet);
        }
    }

    private static async Task<(JsonDocument? Document, int StatusCode, string? ErrorDetail)> GetJsonAsync(string url)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        using var res = await client.GetAsync(url);
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            return (null, (int)res.StatusCode, FormatHttpFailure((int)res.StatusCode, body));
        }

        try
        {
            return (JsonDocument.Parse(body), (int)res.StatusCode, null);
        }
        catch (JsonException)
        {
            return (null, (int)res.StatusCode, "API の応答を解析できませんでした。");
        }
    }

    private static string FormatHttpFailure(int statusCode, string body)
    {
        if (TryParseErrorMessageFromBody(body) is { } parsed)
        {
            return $"HTTP {statusCode}: {parsed}";
        }

        return statusCode switch
        {
            (int)HttpStatusCode.Forbidden =>
                "HTTP 403: キーが無効、プラン対象外、またはリクエストが拒否されました。",
            _ => $"HTTP {statusCode}: リクエストが拒否されました。",
        };
    }

    private static string? TryParseErrorMessageFromBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("ResultSet", out var resultSet))
            {
                return TryGetApiErrorMessage(resultSet);
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static string? TryGetApiErrorMessage(JsonElement resultSet)
    {
        if (!resultSet.TryGetProperty("Error", out var error))
        {
            return null;
        }

        var message = error.TryGetProperty("Message", out var messageEl) ? messageEl.GetString() : null;
        var code = error.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(message))
        {
            message = "API エラー";
        }

        return string.IsNullOrWhiteSpace(code) ? message : $"{message}（{code}）";
    }

    private static int? TryParseFareFromResultSet(JsonElement resultSet)
    {
        if (!resultSet.TryGetProperty("Course", out var courses))
        {
            return null;
        }

        JsonElement first;
        if (courses.ValueKind == JsonValueKind.Array)
        {
            if (courses.GetArrayLength() == 0)
            {
                return null;
            }

            first = courses[0];
        }
        else
        {
            first = courses;
        }

        if (!first.TryGetProperty("Price", out var prices))
        {
            return null;
        }

        IEnumerable<JsonElement> priceList = prices.ValueKind == JsonValueKind.Array
            ? prices.EnumerateArray()
            : [prices];

        foreach (var price in priceList)
        {
            if (price.TryGetProperty("kind", out var kind) && kind.GetString() == "Fare"
                && price.TryGetProperty("Oneway", out var oneway))
            {
                var amount = oneway.GetInt32();
                if (amount > 0)
                {
                    return amount;
                }
            }
        }

        return null;
    }
}

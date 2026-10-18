using System.Text.Json.Serialization;

namespace Keirohi_App.Models;

public sealed class RoutePattern
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    /// <summary>一覧行の背景色（#AARRGGBB または #RRGGBB）。空で無色。</summary>
    public string? ListColor { get; set; }
    public List<RoutePatternLeg> Legs { get; set; } = [new RoutePatternLeg()];
}

public sealed class RoutePatternLeg
{
    public string Departure { get; set; } = "";
    public string Arrival { get; set; } = "";
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TransportMode Mode { get; set; } = TransportMode.Train;
    public string Note { get; set; } = "";
    /// <summary>0 のときは履歴・API から運賃を推定。1 以上ならその金額を使用。</summary>
    public int Amount { get; set; }
}

using System.Text.Json.Serialization;

namespace Keirohi_App.Models;

public enum TransportMode
{
    Train,
    Bus,
}

public sealed class TripEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Date { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Departure { get; set; } = "";
    public string Arrival { get; set; } = "";
    public string Note { get; set; } = "";
    public int Amount { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TransportMode Mode { get; set; } = TransportMode.Train;

    /// <summary>モバイル Suica PDF 取込行（一覧の色分け用）。</summary>
    public bool FromMobileSuicaPdf { get; set; }

    /// <summary>PDF テキスト上の出現順（取込行のみ。同日の並びを保つ）。</summary>
    public int PdfImportOrder { get; set; }
}

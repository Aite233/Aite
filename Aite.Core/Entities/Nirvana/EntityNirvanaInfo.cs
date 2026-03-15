using System.Text.Json.Serialization;

namespace Aite.Core.Entities.Aite;

public class EntityAiteInfo {
    [JsonPropertyName("days")]
    public required double Days { get; set; }

    [JsonPropertyName("msg")]
    public required string Msg { get; set; }
}
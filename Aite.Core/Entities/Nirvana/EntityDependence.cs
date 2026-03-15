using System.Text.Json.Serialization;

namespace Aite.Core.Entities.Aite;

public class EntityDependence {
    [JsonPropertyName("mode")]
    public required string Mode { get; set; }

    [JsonPropertyName("data")]
    public required EntityDependence2[] Data { get; set; }
}
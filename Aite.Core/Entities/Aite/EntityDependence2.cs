using System.Text.Json.Serialization;

namespace Aite.Core.Entities.Aite;

public class EntityDependence2 {
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("id")]
    public required string Id { get; set; }
}
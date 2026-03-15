using System.Text.Json.Serialization;

namespace Aite.Config.Entities.EntitiesAite;

public class EntityMode {
    [JsonPropertyName("mode")]
    public required string Mode { get; init; }
}
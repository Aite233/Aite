using System.Text.Json.Serialization;

namespace Aite.Config.Entities.EntitiesAite;

public class EntityText {
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
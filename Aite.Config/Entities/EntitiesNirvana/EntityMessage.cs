using System.Text.Json.Serialization;

namespace Aite.Config.Entities.EntitiesAite;

public class EntityMessage {
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
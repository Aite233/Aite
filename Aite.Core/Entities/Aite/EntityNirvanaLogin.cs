using System.Text.Json.Serialization;

namespace Aite.Core.Entities.Aite;

public class EntityAiteLogin {
    [JsonPropertyName("online")]
    public string? Token { get; set; }

    [JsonPropertyName("msg")]
    public required string Msg { get; set; }
}
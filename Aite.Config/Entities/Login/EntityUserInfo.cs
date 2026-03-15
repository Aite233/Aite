using System.Text.Json.Serialization;
using Aite.Config.Utils.CodeTools;

namespace Aite.Config.Entities.Login;

// 登录信息
public class EntityUserInfo {
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    public string GetUserId()
    {
        return UserId ?? throw new ErrorCodeException(ErrorCode.LogInNot);
    }

    public string GetToken()
    {
        return Token ?? throw new ErrorCodeException(ErrorCode.LogInNot);
    }

    public bool IsNotNuLl()
    {
        return Token != null && UserId != null;
    }
}
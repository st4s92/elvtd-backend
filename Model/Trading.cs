using System.Text.Json.Serialization;

namespace Backend.Model;

public class CtraderUser
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = "";
    public string Currency { get; set; } = "";
    public double Balance { get; set; }
    public double Equity { get; set; }
    public string Type { get; set; } = "";
}

public class CtraderAppTokenResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = "";

    [JsonPropertyName("refreshToken")]
    public string refreshToken { get; set; } = "";
}

public class BridgeEnvelope<T>
{
    public string Type { get; set; } = "";
    public T Data { get; set; } = default!;
}

public class ManualCtraderAccountPayload
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("account_number")]
    public long AccountNumber { get; set; }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("expiry_token")]
    public string ExpiryToken { get; set; } = "";

    [JsonPropertyName("role")]
    public string Role { get; set; } = "";
}
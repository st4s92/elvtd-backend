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
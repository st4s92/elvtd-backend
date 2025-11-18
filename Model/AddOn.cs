using System.Text.Json.Serialization;

namespace Backend.Model;
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

public class GetPaginatedResponse<T> 
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = [];
    [JsonPropertyName("total")]
    public long Total { get; set; }
}
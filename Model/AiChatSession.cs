using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("ai_chat_sessions")]
public class AiChatSession
{
    [Key]
    [Column("id")]
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [Column("user_id")]
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    [JsonIgnore]
    public User? User { get; set; }

    [Column("title"), MaxLength(255)]
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Neuer Chat";

    [Column("context_snapshot")]
    [JsonPropertyName("context_snapshot")]
    public string? ContextSnapshot { get; set; }

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("messages")]
    public ICollection<AiChatMessage>? Messages { get; set; }
}

public class AiChatSessionPayload
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("account_id")]
    public long? AccountId { get; set; }
}

public class AiChatStreamPayload
{
    [JsonPropertyName("session_id")]
    public long SessionId { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("account_id")]
    public long? AccountId { get; set; }
}

public class AiAnalysisPayload
{
    [JsonPropertyName("account_id")]
    public long? AccountId { get; set; }
}

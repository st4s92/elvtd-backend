using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("ai_chat_messages")]
public class AiChatMessage
{
    [Key]
    [Column("id")]
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [Column("session_id")]
    [JsonPropertyName("session_id")]
    public long SessionId { get; set; }

    [ForeignKey(nameof(SessionId))]
    [JsonIgnore]
    public AiChatSession? Session { get; set; }

    [Column("role"), MaxLength(20)]
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [Column("content")]
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [Column("tokens_used")]
    [JsonPropertyName("tokens_used")]
    public int TokensUsed { get; set; }

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

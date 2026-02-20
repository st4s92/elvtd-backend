using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("symbol_map")]
public class SymbolMap : IAuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required, MaxLength(100)]
    [Column("broker_name")]
    public string BrokerName { get; set; } = "";

    [Required, MaxLength(100)]
    [Column("server_name")]
    public string ServerName { get; set; } = "";

    [Required, MaxLength(20)]
    [Column("broker_symbol")]
    public string BrokerSymbol { get; set; } = "";

    [Required, MaxLength(20)]
    [Column("canonical_symbol")]
    public string CanonicalSymbol { get; set; } = "";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}

// === Payloads ===

public class SymbolMapPayload
{
    [JsonPropertyName("broker_name")]
    public string BrokerName { get; set; } = "";

    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = "";

    [JsonPropertyName("broker_symbol")]
    public string BrokerSymbol { get; set; } = "";

    [JsonPropertyName("canonical_symbol")]
    public string CanonicalSymbol { get; set; } = "";
}

public class SymbolMapGetPayload : SymbolMapPayload
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
}

public class SymbolMapGetPaginatedPayload : SymbolMapGetPayload
{
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}

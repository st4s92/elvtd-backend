using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("master_slave")]
public class MasterSlave : IAuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("name"), MaxLength(100)]
    public string Name { get; set; } = "";

    [Column("master_id")]
    public long MasterId { get; set; }

    [Column("slave_id")]
    public long SlaveId { get; set; }

    public Account? MasterAccount { get; set; }
    public Account? SlaveAccount { get; set; }

    // ✅ Add these navigation collections
    public ICollection<MasterSlavePair> Pairs { get; set; } = new List<MasterSlavePair>();
    public ICollection<MasterSlaveConfig> Configs { get; set; } = new List<MasterSlaveConfig>();

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}

public class MasterSlavePayload
{
    [JsonPropertyName("name")]
    public string? Name { get; set; } = "";

    [JsonPropertyName("master_id")]
    public long? MasterId { get; set; }

    [JsonPropertyName("slave_id")]
    public long? SlaveId { get; set; }
}

public class MasterSlaveGetPayload : MasterSlavePayload
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
}

public class MasterSlaveGetPaginatedPayload : MasterSlaveGetPayload
{
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}

// ===== edit account master slave config
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CopyTradeRole
{
    MASTER,
    SLAVE,
}

public class MasterSlaveSymbolPairPayload
{
    [JsonPropertyName("masterSymbol")]
    public string MasterSymbol { get; set; } = "";

    [JsonPropertyName("slaveSymbol")]
    public string SlaveSymbol { get; set; } = "";
}

public class MasterSlaveFullConfigPayload
{
    [JsonPropertyName("connection_name")]
    [Required, MaxLength(100)]
    public string ConnectionName { get; set; } = "";

    [JsonPropertyName("role")]
    [Required]
    public CopyTradeRole Role { get; set; }

    [JsonPropertyName("accountId")]
    [Required]
    public long AccountId { get; set; }

    [JsonPropertyName("destination_id")]
    [Required]
    public long DestinationId { get; set; }

    [JsonPropertyName("multiplier")]
    [Range(0.1, 10)]
    public decimal Multiplier { get; set; } = 1.0m;

    [JsonPropertyName("symbol_pairs")]
    public List<MasterSlaveSymbolPairPayload> SymbolPairs { get; set; } = new();
}

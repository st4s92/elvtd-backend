using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("master_slave_pair")]
public class MasterSlavePair : IAuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("master_slave_id")]
    public long MasterSlaveId { get; set; }

    [ForeignKey(nameof(MasterSlaveId))]
    public virtual MasterSlave? MasterSlave { get; set; }

    [Required, MaxLength(10)]
    [Column("master_pair")]
    public string MasterPair { get; set; } = "";

    [Required, MaxLength(10)]
    [Column("slave_pair")]
    public string SlavePair { get; set; } = "";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}


public class MasterSlavePairPayload
{
    [JsonPropertyName("master_slave_id")]
    public long? MasterSlaveId { get; set; }
    [JsonPropertyName("slave_pair")]
    public string? SlavePair { get; set; } = "";
    [JsonPropertyName("master_pair")]
    public string? MasterPair { get; set; } = "";
}


public class MasterSlavePairGetPayload : MasterSlavePairPayload
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
}

public class MasterSlavePairGetPaginatedPayload : MasterSlavePairGetPayload
{
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}
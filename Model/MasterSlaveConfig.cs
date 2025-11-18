using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("master_slave_config")]
public class MasterSlaveConfig : IAuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("master_slave_id")]
    public long MasterSlaveId { get; set; }

    [ForeignKey(nameof(MasterSlaveId))]
    public virtual MasterSlave? MasterSlave { get; set; }

    [Required]
    [Column("multiplier", TypeName = "decimal(10,4)")]
    public decimal Multiplier { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}


public class MasterSlaveConfigPayload
{
    [JsonPropertyName("master_slave_id")]
    public long? MasterSlaveId { get; set; }
    [JsonPropertyName("multiplier")]
    public decimal? Multiplier { get; set; }
}


public class MasterSlaveConfigGetPayload : MasterSlaveConfigPayload
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
}

public class MasterSlaveConfigGetPaginatedPayload : MasterSlaveConfigGetPayload
{
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}
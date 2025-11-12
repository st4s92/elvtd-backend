using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model;

[Table("master_slave_pair")]
public class MasterSlavePair : IAuditableEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("master_slave_id")]
    public int MasterSlaveId { get; set; }

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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model;

[Table("master_slave_config")]
public class MasterSlaveConfig : IAuditableEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("master_slave_id")]
    public int MasterSlaveId { get; set; }

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

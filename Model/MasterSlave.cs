using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model;

[Table("master_slave")]
public class MasterSlave : IAuditableEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("master_id")]
    public int MasterId { get; set; }

    [Column("slave_id")]
    public int SlaveId { get; set; }

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

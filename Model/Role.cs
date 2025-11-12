using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model;

[Table("roles")]
public class Role : IAuditableEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Required, MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model;

public class AppToken : IAuditableEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserID { get; set; } = 0;

    [Required, MaxLength(50)]
    [Column("platform")]
    public string Platform { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    [Column("platform_id")]
    public string PlatformId { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    [Column("token")]
    public string AuthToken { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    [Column("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Column("expired_at")]
    public DateTime ExpiredAt { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(100)]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}
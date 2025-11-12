using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Model;

[Table("users")]
public class User : IAuditableEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name"), MaxLength(100)]
    public string Name { get; set; } = "";

    [Column("email"), MaxLength(100)]
    public string Email { get; set; } = "";

    [Column("password"), MaxLength(300)]
    public string Password { get; set; } = "";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;

    // Navigation
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}


public record LoginRequest(string Email, string Password);

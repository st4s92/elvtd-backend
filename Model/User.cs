using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("users")]
public class User : IAuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("name"), MaxLength(100)]
    public string Name { get; set; } = "";

    [Column("email"), MaxLength(100)]
    public string Email { get; set; } = "";
    [Column("role_id")]
    public long RoleId { get; set; }

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
    [JsonIgnore]
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}


public record LoginRequest(string Email, string Password);


public class UserPayload
{
    [JsonPropertyName("name")]
    public string? Name { get; set; } = "";

    [JsonPropertyName("email")]
    public string? Email { get; set; } = "";
    [JsonPropertyName("password")]
    public string? Password { get; set; } = "";

    [JsonPropertyName("role_id")]
    public long? RoleId { get; set; }
}


public class UserGetPayload : UserPayload
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
}

public class UserGetPaginatedPayload : UserGetPayload
{
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}

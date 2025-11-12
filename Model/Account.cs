using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("accounts")]
public class Account : IAuditableEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("platform_name"), MaxLength(100)]
    public string PlatformName { get; set; } = "";

    [Column("platform_path"), MaxLength(300)]
    public string? PlatformPath { get; set; } = "";

    [Column("account_number")]
    public int AccountNumber { get; set; }

    [Column("broker_name"), MaxLength(100)]
    public string BrokerName { get; set; } = "";

    [Column("server_name"), MaxLength(100)]
    public string ServerName { get; set; } = "";

    [Column("user_id")]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;

    // Reverse navigation
    [JsonIgnore]
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<MasterSlave> MasterRelations { get; set; } = new List<MasterSlave>();
    public ICollection<MasterSlave> SlaveRelations { get; set; } = new List<MasterSlave>();
}

public class AccountAddPayload
{
    [JsonPropertyName("platform_name")]
    [Column("platform_name"), MaxLength(100)]
    public string PlatformName { get; set; } = "";

    [JsonPropertyName("platform_path")]
    [Column("platform_path"), MaxLength(300)]
    public string? PlatformPath { get; set; } = "";

    [JsonPropertyName("account_number")]
    [Column("account_number")]
    public int AccountNumber { get; set; }

    [JsonPropertyName("broker_name")]
    public string BrokerName { get; set; } = "";

    [JsonPropertyName("server_name")]
    [Column("server_name"), MaxLength(100)]
    public string ServerName { get; set; } = "";

    [JsonPropertyName("user_id")]
    [Column("user_id")]
    public int UserId { get; set; }
}
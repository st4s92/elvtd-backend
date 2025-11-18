using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("accounts")]
public class Account : IAuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("platform_name"), MaxLength(100)]
    public string PlatformName { get; set; } = "";

    [Column("platform_path"), MaxLength(300)]
    public string? PlatformPath { get; set; } = "";

    [Column("account_number")]
    public long AccountNumber { get; set; }

    [Column("broker_name"), MaxLength(100)]
    public string BrokerName { get; set; } = "";

    [Column("server_name"), MaxLength(100)]
    public string ServerName { get; set; } = "";

    [Column("user_id")]
    public long UserId { get; set; }

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
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<MasterSlave> MasterRelations { get; set; } = new List<MasterSlave>();
    public ICollection<MasterSlave> SlaveRelations { get; set; } = new List<MasterSlave>();
}

public class AccountPayload
{
    [JsonPropertyName("platform_name")]
    public string? PlatformName { get; set; } = "";

    [JsonPropertyName("platform_path")]
    public string? PlatformPath { get; set; } = "";

    [JsonPropertyName("account_number")]
    public long? AccountNumber { get; set; }

    [JsonPropertyName("broker_name")]
    public string? BrokerName { get; set; } = "";

    [JsonPropertyName("server_name")]
    public string? ServerName { get; set; } = "";

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }
}

public class AccountGetPayload : AccountPayload
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
}

public class AccountGetPaginatedPayload : AccountGetPayload
{
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}

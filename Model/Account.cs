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

    [Column("account_number")]
    public long AccountNumber { get; set; }

    [Column("account_password")]
    public string AccountPassword { get; set; } = "";

    [Column("broker_name"), MaxLength(100)]
    public string BrokerName { get; set; } = "";

    [Column("server_name"), MaxLength(100)]
    public string ServerName { get; set; } = "";

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("equity")]
    public decimal Equity { get; set; }

    [Column("balance")]
    public decimal Balance { get; set; }

    [Column("status")]
    public ConnectionStatus Status { get; set; } = ConnectionStatus.None;

    [Column("role")]
    public string Role { get; set; } = "";

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

    public ServerAccount? ServerAccount { get; set; }
}

public class AccountPayload
{
    [JsonPropertyName("platform_name")]
    public string? PlatformName { get; set; } = "";

    [JsonPropertyName("account_number")]
    public long? AccountNumber { get; set; }

    [JsonPropertyName("account_password")]
    public string? AccountPassword { get; set; }

    [JsonPropertyName("broker_name")]
    public string? BrokerName { get; set; } = "";

    [JsonPropertyName("server_name")]
    public string? ServerName { get; set; } = "";

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; } = "";

    [JsonPropertyName("role")]
    public string? Role { get; set; } = "SLAVE";
}

public class AccountGetPayload : AccountPayload
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
}

public class AccountGetPaginatedPayload : AccountGetPayload
{
    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}

public class AccountGetPaginatedObject
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("platform_name")]
    public string PlatformName { get; set; } = "";

    [JsonPropertyName("account_number")]
    public long AccountNumber { get; set; }

    [JsonPropertyName("broker_name")]
    public string BrokerName { get; set; } = "";

    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = "";

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "NONE";

    [JsonPropertyName("server_status")]
    public string? ServerStatus { get; set; }

    [JsonPropertyName("server_status_message")]
    public string? ServerStatusMessage { get; set; }

    [JsonPropertyName("status")]
    public ConnectionStatus? Status { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class TradePlatformCreateJob
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("platform_name")]
    public string PlatformName { get; set; } = "";

    [JsonPropertyName("account_number")]
    public long AccountNumber { get; set; }

    [JsonPropertyName("account_password")]
    public string AccountPassword { get; set; } = "";

    [JsonPropertyName("broker_name")]
    public string BrokerName { get; set; } = "";

    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = "";

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = "SLAVE";

    [JsonPropertyName("status")]
    public int Status { get; set; } = 100;

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("pid")]
    public int Pid { get; set; }
}

public class TradePlatformCreatedEvent
{
    [JsonPropertyName("id")]
    public int AccountId { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("installation_path")]
    public string InstallationPath { get; set; } = "";

    [JsonPropertyName("server_ip")]
    public string ServerIp { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("pid")]
    public int Pid { get; set; }
}

public class ServerAccountPlatformUpdateRequest
{
    public long AccountId { get; set; }
    public string Message { get; set; } = "";
    public ConnectionStatus Status { get; set; }
    public string InstallationPath { get; set; } = "";
    public int Pid { get; set; }
}

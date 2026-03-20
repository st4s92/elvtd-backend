using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("accounts")]
public class Account : IAuditableEntity
{
    [Key]
    [Column("id")]
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [Column("platform_name"), MaxLength(100)]
    [JsonPropertyName("platform_name")]
    public string PlatformName { get; set; } = "";

    [Column("account_number")]
    [JsonPropertyName("account_number")]
    public long AccountNumber { get; set; }

    [Column("account_password")]
    [JsonPropertyName("account_password")]
    public string AccountPassword { get; set; } = "";

    [Column("broker_name"), MaxLength(100)]
    [JsonPropertyName("broker_name")]
    public string BrokerName { get; set; } = "";

    [Column("server_name"), MaxLength(100)]
    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = "";

    [Column("user_id")]
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [Column("equity")]
    [JsonPropertyName("equity")]
    public decimal Equity { get; set; }

    [Column("balance")]
    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    [Column("status")]
    [JsonPropertyName("status")]
    public ConnectionStatus Status { get; set; } = ConnectionStatus.None;

    [Column("role")]
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [Column("is_flush_order")]
    public int IsFlushOrder { get; set; } = 0;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("copier_version"), MaxLength(50)]
    [JsonPropertyName("copier_version")]
    public string? CopierVersion { get; set; }

    [Column("access_token"), MaxLength(500)]
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [Column("refresh_token"), MaxLength(500)]
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [Column("token_expired_at")]
    [JsonPropertyName("token_expired_at")]
    public DateTime? TokenExpiredAt { get; set; }

    [Column("ctid_trader_account_id")]
    [JsonPropertyName("ctid_trader_account_id")]
    public long? CtidTraderAccountId { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;

    // Reverse navigation
    [JsonIgnore]
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    [JsonIgnore]
    public ICollection<MasterSlave> MasterRelations { get; set; } = new List<MasterSlave>();
    [JsonIgnore]
    public ICollection<MasterSlave> SlaveRelations { get; set; } = new List<MasterSlave>();

    [JsonIgnore]
    public ICollection<ActiveOrder> ActiveOrders { get; set; } = new List<ActiveOrder>();

    [JsonPropertyName("server_account")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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
    public string? BrokerName { get; set; }

    [JsonPropertyName("server_name")]
    public string? ServerName { get; set; }

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("copier_version")]
    public string? CopierVersion { get; set; }

    // cTrader token fields (optional, only used for cTrader accounts)
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expiry_token")]
    public string? ExpiryToken { get; set; }

    [JsonPropertyName("ctid_trader_account_id")]
    public long? CtidTraderAccountId { get; set; }

    [JsonPropertyName("status")]
    public ConnectionStatus? Status { get; set; }
}

public class AccountGetPayload : AccountPayload
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("search")]
    public string? Search { get; set; }
}

public class AccountGetPaginatedPayload : AccountGetPayload
{
    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("sort_by")]
    public string? SortBy { get; set; }

    [JsonPropertyName("sort_order")]
    public string? SortOrder { get; set; }
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

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    [JsonPropertyName("equity")]
    public decimal Equity { get; set; }

    [JsonPropertyName("open_positions_count")]
    public int OpenPositionsCount { get; set; }

    [JsonPropertyName("dedicated_server_name")]
    public string DedicatedServerName { get; set; } = "";

    [JsonPropertyName("copier_version")]
    public string? CopierVersion { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("token_expired_at")]
    public DateTime? TokenExpiredAt { get; set; }

    [JsonPropertyName("ctid_trader_account_id")]
    public long? CtidTraderAccountId { get; set; }
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

    [JsonPropertyName("server_ip")]
    public string ServerIp { get; set; } = "";
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
    public string ServerIp { get; set; } = "";
    public int Pid { get; set; }
}

public class AccountDetailDto
{
    [JsonPropertyName("account")]
    public Account Account { get; set; } = null!;

    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("serverAccount")]
    public ServerAccount? ServerAccount { get; set; }

    [JsonPropertyName("server")]
    public Server? Server { get; set; }

    [JsonPropertyName("orders")]
    public List<ActiveOrderDto> Orders { get; set; } = [];

    [JsonPropertyName("orderLogs")]
    public List<OrderLog> OrderLogs { get; set; } = [];

    [JsonPropertyName("accountLogs")]
    public List<AccountLog> AccountLogs { get; set; } = [];
}

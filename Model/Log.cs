using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("order_logs")]
public class OrderLog : IAuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [Column("order_symbol"), MaxLength(20)]
    public string OrderSymbol { get; set; } = "";

    [Column("order_ticket")]
    public long OrderTicket { get; set; }

    [Column("order_type"), MaxLength(20)]
    public string OrderType { get; set; } = "";

    [Column("order_lot", TypeName = "decimal(13,3)")]
    public decimal OrderLot { get; set; }

    [Column("order_price", TypeName = "decimal(13,6)")]
    public decimal? OrderPrice { get; set; }

    [Column("sl_price", TypeName = "decimal(13,6)")]
    public decimal? SlPrice { get; set; }

    [Column("tp_price", TypeName = "decimal(13,6)")]
    public decimal? TpPrice { get; set; }

    [Column("last_price", TypeName = "decimal(13,6)")]
    public decimal? LastPrice { get; set; }

    [Column("last_time")]
    public DateTime? LastTime { get; set; }

    [Column("order_profit", TypeName = "decimal(13,2)")]
    public decimal? OrderProfit { get; set; }

    [Column("change", TypeName = "decimal(13,2)")]
    public decimal? Change { get; set; }

    [Column("status")]
    public OrderStatus Status { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}

public class OrderLogPayload
{
    [JsonPropertyName("account_number")]
    public long AccountNumber { get; set; }

    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = "";

    [JsonPropertyName("order_symbol")]
    public string OrderSymbol { get; set; } = "";

    [JsonPropertyName("order_ticket")]
    public long OrderTicket { get; set; }

    [JsonPropertyName("order_type")]
    public string OrderType { get; set; } = "";

    [JsonPropertyName("order_lot")]
    public decimal OrderLot { get; set; }

    [JsonPropertyName("order_price")]
    public decimal? OrderPrice { get; set; }

    [JsonPropertyName("sl_price")]
    public decimal? SlPrice { get; set; }

    [JsonPropertyName("tp_price")]
    public decimal? TpPrice { get; set; }

    [JsonPropertyName("last_price")]
    public decimal? LastPrice { get; set; }

    [JsonPropertyName("last_time")]
    public DateTime? LastTime { get; set; }

    [JsonPropertyName("order_profit")]
    public decimal? OrderProfit { get; set; }

    [JsonPropertyName("change")]
    public decimal? Change { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}

[Table("account_logs")]
public class AccountLog : IAuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [Column("equity")]
    public decimal Equity { get; set; }

    [Column("balance")]
    public decimal Balance { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}

public class SyncAccountStatePayload
{
    [JsonPropertyName("account_number")]
    public long AccountNumber { get; set; }

    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = "";

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    [JsonPropertyName("equity")]
    public decimal Equity { get; set; }

    [JsonPropertyName("positions")]
    public List<OrderLogPayload> Positions { get; set; } = new();

    [JsonPropertyName("copier_version")]
    public string? CopierVersion { get; set; }

    [JsonPropertyName("server_status")]
    public string? ServerStatus { get; set; }

    [JsonPropertyName("server_status_message")]
    public string? ServerStatusMessage { get; set; }

    [JsonPropertyName("expert_log")]
    public string? ExpertLog { get; set; }

    [JsonIgnore]
    public string SourceIp { get; set; } = "";
}

[Table("system_logs")]
public class SystemLog : IAuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("category"), MaxLength(50)]
    public string Category { get; set; } = ""; // e.g., "Account", "Order", "System"

    [Column("action"), MaxLength(50)]
    public string Action { get; set; } = ""; // e.g., "Create", "Delete", "Install", "Restart"

    [Column("account_id")]
    public long? AccountId { get; set; } // Nullable, as some logs might be system-wide

    [JsonIgnore]
    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [Column("message")]
    public string Message { get; set; } = "";

    [Column("level"), MaxLength(20)]
    public string Level { get; set; } = "Info"; // "Info", "Warning", "Error"

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}

public class SystemLogGetPayload
{
    public string? Category { get; set; }
    public string? Action { get; set; }
    public long? AccountNumber { get; set; }
    public string? Level { get; set; }
    public string? Search { get; set; }
}

public class SystemLogGetPaginatedPayload : SystemLogGetPayload
{
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 10;
}

public class SystemLogCreatePayload
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("account_id")]
    public long? AccountId { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("level")]
    public string Level { get; set; } = "Info";
}

public class SystemLogDto
{
    public long Id { get; set; }
    public string Category { get; set; } = "";
    public string Action { get; set; } = "";
    public long? AccountId { get; set; }
    public long? AccountNumber { get; set; }
    public string? ServerName { get; set; }
    public string Message { get; set; } = "";
    public string Level { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

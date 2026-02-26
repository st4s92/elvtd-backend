using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

public enum OrderStatus
{
    None = 100,
    Progress = 200,
    Failed = 300,
    Closed = 400,
    Canceled = 500,
    Success = 600,
    Complete = 700,
}

[Table("orders")]
public class Order : IAuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [Column("master_order_id")]
    public long? MasterOrderId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(MasterOrderId))]
    public Order? MasterOrder { get; set; }

    [Column("order_ticket")]
    public long OrderTicket { get; set; }

    [Column("close_ticket")]
    public long? CloseTicket { get; set; }

    [Column("order_symbol"), MaxLength(20)]
    public string OrderSymbol { get; set; } = "";

    [Column("order_type"), MaxLength(20)]
    public string OrderType { get; set; } = "";

    [Column("order_lot", TypeName = "decimal(13,3)")]
    public decimal OrderLot { get; set; }

    [Column("order_price", TypeName = "decimal(13,6)")]
    public decimal? OrderPrice { get; set; }

    [Column("close_price", TypeName = "decimal(13,6)")]
    public decimal? ClosePrice { get; set; }

    [Column("order_magic")]
    public long? OrderMagic { get; set; }

    [Column("status")]
    public OrderStatus Status { get; set; }

    [Column("copy_message"), MaxLength(255)]
    public string? CopyMessage { get; set; }

    [Column("order_open_at")]
    public DateTime? OrderOpenAt { get; set; }

    [Column("order_copied_at")]
    public DateTime? OrderCopiedAt { get; set; }

    [Column("order_close_at")]
    public DateTime? OrderCloseAt { get; set; }

    [Column("order_profit", TypeName = "decimal(13,2)")]
    public decimal? OrderProfit { get; set; }

    [NotMapped]
    public long? AverageExecutionLag { get; set; }

    [NotMapped]
    public long? MaxExecutionLag { get; set; }

    [NotMapped]
    public int SlaveCount { get; set; }

    [NotMapped]
    public int SlaveSuccessCount { get; set; }

    [NotMapped]
    public int SlaveFailureCount { get; set; }

    [NotMapped]
    public bool? IsMasterOnly { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}

public record OrderSyncRequest(List<Order> Orders, Account Account);

public class BridgeListOrderPayload
{
    [JsonPropertyName("broker_name")]
    public string BrokerName { get; set; } = "";

    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = "";

    [JsonPropertyName("account_id")]
    public long AccountId { get; set; }

    [JsonPropertyName("orders")]
    public List<BridgeOrderItem> Orders { get; set; } = new();
}

public class BridgeOrderPayload
{
    [JsonPropertyName("broker_name")]
    public string BrokerName { get; set; } = "";

    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = "";

    [JsonPropertyName("account_id")]
    public long AccountId { get; set; }

    [JsonPropertyName("order")]
    public BridgeOrderItem Order { get; set; } = new();
}

public class BridgeOrderItem
{
    [JsonPropertyName("master_order_id")]
    public long MasterOrderId { get; set; }

    [JsonPropertyName("order_ticket")]
    public long OrderTicket { get; set; }

    [JsonPropertyName("close_ticket")]
    public long CloseTicket { get; set; }

    [JsonPropertyName("order_symbol")]
    public string OrderSymbol { get; set; } = "";

    [JsonPropertyName("order_type")]
    public string OrderType { get; set; } = "";

    [JsonPropertyName("order_lot")]
    public decimal OrderLot { get; set; }

    [JsonPropertyName("order_price")]
    public decimal OrderPrice { get; set; }

    [JsonPropertyName("order_close_price")]
    public decimal OrderClosePrice { get; set; }

    [JsonPropertyName("order_profit")]
    public decimal? OrderProfit { get; set; }

    [JsonPropertyName("order_comment")]
    public string OrderMagic { get; set; } = "";

    [JsonPropertyName("order_status")]
    public string OrderStatus { get; set; } = "";

    [JsonPropertyName("order_open_at")]
    public DateTime? OrderOpenAt { get; set; }

    [JsonPropertyName("order_close_at")]
    public DateTime? OrderCloseAt { get; set; }
}

public class BridgeOrderBroadcastPayload
{
    [JsonPropertyName("slave_pair")]
    public string SlavePair { get; set; } = "";

    [JsonPropertyName("order_type")]
    public string OrderType { get; set; } = "";

    [JsonPropertyName("order_lot")]
    public decimal OrderLot { get; set; }

    [JsonPropertyName("order_ticket")]
    public long OrderTicket { get; set; }

    [JsonPropertyName("master_order_id")]
    public long MasterOrderId { get; set; }

    [JsonPropertyName("copy_type")]
    public string CopyType { get; set; } = "";

    [JsonPropertyName("order_magic")]
    public long OrderMagic { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class OrderQuery
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("account_id")]
    public long? AccountId { get; set; }

    [JsonPropertyName("master_order_id")]
    public long? MasterOrderId { get; set; }

    [JsonPropertyName("order_ticket")]
    public long? OrderTicket { get; set; }

    [JsonPropertyName("order_symbol")]
    public string? OrderSymbol { get; set; }

    [JsonPropertyName("order_type")]
    public string? OrderType { get; set; }

    [JsonPropertyName("order_lot")]
    public decimal? OrderLot { get; set; }

    [JsonPropertyName("status")]
    public OrderStatus? Status { get; set; }

    [JsonPropertyName("search")]
    public string? Search { get; set; }

    [JsonPropertyName("is_master_only")]
    public bool? IsMasterOnly { get; set; }

    [JsonPropertyName("is_closed")]
    public bool? IsClosed { get; set; }
}

public class OrderGetPaginatedPayload : OrderQuery
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

// simplyfiy create new order item
public enum OrderType
{
    [Description("DEAL_TYPE_BUY")]
    Buy,

    [Description("DEAL_TYPE_SELL")]
    Sell,
}

public class BridgeCreateOrderItem
{
    [JsonPropertyName("order_ticket")]
    public long OrderTicket { get; set; }

    [JsonPropertyName("order_symbol")]
    public string OrderSymbol { get; set; } = "";

    [JsonPropertyName("order_type")]
    public string OrderType { get; set; } = "";

    [JsonPropertyName("order_lot")]
    public decimal OrderLot { get; set; }

    [JsonPropertyName("order_price")]
    public decimal? OrderPrice { get; set; }

    [JsonPropertyName("order_open_at")]
    public DateTime? OrderOpenAt { get; set; }
}

public class BridgeListCreateOrderPayload
{
    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = "";

    [JsonPropertyName("account_id")]
    public long AccountId { get; set; }

    [JsonPropertyName("balance")]
    public decimal? Balance { get; set; }

    [JsonPropertyName("equity")]
    public decimal? Equity { get; set; }

    [JsonPropertyName("orders")]
    public List<BridgeCreateOrderItem> Orders { get; set; } = new();
}

public class MasterOrderDeleteOrder
{
    [JsonPropertyName("account_id")]
    public long AccountId { get; set; }

    [JsonPropertyName("is_flush_order")]
    public bool IsFlushOrder { get; set; }
    [JsonPropertyName("order_ids")]
    public List<long>? OrderIds { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

public enum OrderStatus
{
    None = 100,
    Pending = 200,
    Failed = 300,
    Closed = 400,
    Canceled = 500,
    Success = 600,
    Complete = 700
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
    public decimal OrderPrice { get; set; }

    [Column("close_price", TypeName = "decimal(13,6)")]
    public decimal? ClosePrice { get; set; }

    [Column("actual_price", TypeName = "decimal(13,6)")]
    public decimal? ActualPrice { get; set; }

    [Column("order_comment"), MaxLength(255)]
    public string? OrderComment { get; set; }

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
    public BridgeOrderItem Order{ get; set; } = new();
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

    [JsonPropertyName("actual_price")]
    public decimal? ActualPrice { get; set; }

    [JsonPropertyName("order_comment")]
    public string OrderComment { get; set; } = "";

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
}

public class OrderQuery
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
    [JsonPropertyName("account_id")]
    public long? AccountId { get; set; }
    [JsonPropertyName("master_order_id")]
    public long? MasterOrderId { get; set; }
    [JsonPropertyName("order_symbol")]
    public string? OrderSymbol { get; set; }
    [JsonPropertyName("order_type")]
    public string? OrderType { get; set; }
    [JsonPropertyName("status")]
    public OrderStatus? Status { get; set; } = OrderStatus.None;
}

public class OrderGetPaginatedPayload : OrderQuery
{
    [JsonPropertyName("per_page")]

    public int PerPage { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}
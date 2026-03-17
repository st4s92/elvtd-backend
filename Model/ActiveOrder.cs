using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("active_orders")]
public class ActiveOrder : IAuditableEntity
{
    [Key]
    [Column("id")]
    [JsonPropertyName("id")]
    public long Id { get; set; }

    // -------------------------
    // SLAVE IDENTITY
    // -------------------------

    [Column("account_id")]
    [JsonPropertyName("account_id")]
    public long AccountId { get; set; } // SLAVE account id

    [ForeignKey(nameof(AccountId))]
    [JsonPropertyName("account")]
    public Account? Account { get; set; }

    [Column("account_number")]
    [JsonPropertyName("account_number")]
    public long AccountNumber { get; set; } // SLAVE MT5 account number

    [Column("server_name")]
    [JsonPropertyName("server_name")]
    public string? ServerName { get; set; }

    // -------------------------
    // MASTER REFERENCE
    // -------------------------

    [Column("master_order_id")]
    [JsonPropertyName("master_order_id")]
    public long? MasterOrderId { get; set; }

    // -------------------------
    // EXECUTION DATA
    // -------------------------

    [Column("order_ticket")]
    [JsonPropertyName("order_ticket")]
    public long OrderTicket { get; set; } = 0; // updated later by MT5

    [Column("order_symbol")]
    [JsonPropertyName("order_symbol")]
    public string OrderSymbol { get; set; } = "";

    [Column("order_magic")]
    [JsonPropertyName("order_magic")]
    public long OrderMagic { get; set; }

    [Column("order_type"), MaxLength(20)]
    [JsonPropertyName("order_type")]
    public string OrderType { get; set; } = "";

    [Column("order_lot", TypeName = "decimal(13,3)")]
    [JsonPropertyName("order_lot")]
    public decimal OrderLot { get; set; }

    [Column("order_price", TypeName = "decimal(13,6)")]
    [JsonPropertyName("order_price")]
    public decimal OrderPrice { get; set; }

    [Column("order_open_at")]
    [JsonPropertyName("order_open_at")]
    public DateTime? OrderOpenAt { get; set; }

    [Column("order_profit", TypeName = "decimal(13,2)")]
    [JsonPropertyName("order_profit")]
    public decimal? OrderProfit { get; set; }


    [Column("status")]
    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; }

    // -------------------------
    // AUDIT
    // -------------------------

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class PlatformActivePositionSyncPayload
{
    [JsonPropertyName("account_number")]
    public long AccountNumber { get; set; }

    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = null!;

    [JsonPropertyName("is_flush_order")]
    public int IsFlushOrder { get; set; } = 0;

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    [JsonPropertyName("equity")]
    public decimal Equity { get; set; }

    [JsonPropertyName("position_list")]
    public List<PlatformPositionDto> PositionList { get; set; } = [];

    [JsonPropertyName("copier_version")]
    public string? CopierVersion { get; set; }
}

public class PlatformPositionDto
{
    [JsonPropertyName("order_ticket")]
    public long OrderTicket { get; set; }

    [JsonPropertyName("order_magic")]
    public long OrderMagic { get; set; }

    [JsonPropertyName("order_type")]
    public string OrderType { get; set; } = null!;

    [JsonPropertyName("order_lot")]
    public decimal OrderLot { get; set; }

    [JsonPropertyName("order_price")]
    public decimal OrderPrice { get; set; }

    [JsonPropertyName("order_profit")]
    public decimal OrderProfit { get; set; }

    [JsonPropertyName("order_symbol")]
    public string OrderSymbol { get; set; } = "";

    [JsonPropertyName("order_open_at")]
    public DateTime OrderOpenAt { get; set; }

    [JsonPropertyName("status")]
    public OrderStatus Status { get; set; } = OrderStatus.Progress;
}

public class ActiveOrderDto
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public long OrderTicket { get; set; }
    public string OrderSymbol { get; set; } = "";
    public string OrderType { get; set; } = "";
    public decimal OrderLot { get; set; }
    public decimal? OrderPrice { get; set; }
    public decimal? OrderProfit { get; set; }
}
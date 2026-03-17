using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

public enum ConnectionStatus
{
    All = 0,
    None = 100,
    Success = 200,
    UnknownFail = 300
}

[Table("servers")]
public class Server : IAuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("server_name"), MaxLength(100)]
    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = "";

    [Column("server_ip"), MaxLength(100)]
    [JsonPropertyName("server_ip")]
    public string ServerIp { get; set; } = "";

    [Column("status")]
    public ConnectionStatus Status { get; set; } = ConnectionStatus.None;

    [Column("server_os"), MaxLength(300)]
    public string ServerOs { get; set; } = "";

    [Column("active_terminals")]
    public int ActiveTerminals { get; set; }

    [Column("cpu_usage")]
    public double CpuUsage { get; set; }

    [Column("ram_usage")]
    public double RamUsage { get; set; }

    [Column("uptime"), MaxLength(100)]
    public string UptimeString { get; set; } = "";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public ICollection<ServerAccount> ServerAccounts { get; set; }
        = new List<ServerAccount>();
}

public class ServerCreatePayload
{
    [JsonPropertyName("server_name")]
    public string? ServerName { get; set; } = "";

    [JsonPropertyName("server_ip")]
    public string? ServerIp { get; set; } = "";

    [JsonPropertyName("server_os")]
    public string? ServerOs { get; set; } = "";
}

public class ServerGetPayload : ServerCreatePayload
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
}

public class ServerGetPaginatedPayload : ServerGetPayload
{
    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}

[Table("server_account")]
public class ServerAccount : IAuditableEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("server_id")]
    public long ServerId { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("installation_path"), MaxLength(300)]
    public string InstallationPath { get; set; } = "";

    [Column("platform_pid"), MaxLength(300)]
    public int? PlatformPid { get; set; }
    [Column("status")]
    public ConnectionStatus Status { get; set; } = ConnectionStatus.None;
    [Column("message"), MaxLength(100)]
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;

    [JsonIgnore]
    public Account Account { get; set; } = null!;
    
    [JsonPropertyName("server")]
    public Server Server { get; set; } = null!;
}

public class ServerAccountCreatePayload
{
    [JsonPropertyName("server_id")]
    public long ServerId { get; set; }

    [JsonPropertyName("account_id")]
    public long AccountId { get; set; }
}

public class ServerHeartbeatRequest
{
    [JsonPropertyName("ip")]
    public string Ip { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("status")]
    public ConnectionStatus Status { get; set; } = ConnectionStatus.None;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("active_terminals")]
    public int ActiveTerminals { get; set; }

    [JsonPropertyName("cpu_usage")]
    public double CpuUsage { get; set; }

    [JsonPropertyName("ram_usage")]
    public double RamUsage { get; set; }

    [JsonPropertyName("uptime")]
    public string Uptime { get; set; } = "";
}

public class HealthCheckResponse
{
    [JsonPropertyName("account")]
    public List<Account> Accounts { get; set; } = [];

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}
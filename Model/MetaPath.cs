using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

[Table("metatraders")]
public class Metatraders : IAuditableEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Key]
    [Column("account_id")]
    public int AccountId { get; set; }
    [Key]
    [Column("path")]
    public string path { get; set; } = "";
    [Key]
    [Column("vps_ip")]
    public string VpsIp { get; set; } = "";
    public DateTime CreatedAt { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DateTime UpdatedAt { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}

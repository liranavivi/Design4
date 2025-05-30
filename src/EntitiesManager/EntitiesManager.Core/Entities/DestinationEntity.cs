using EntitiesManager.Core.Entities.Base;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EntitiesManager.Core.Entities;

public class DestinationEntity : BaseEntity
{
    [BsonElement("address")]
    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("configuration")]
    public Dictionary<string, object> Configuration { get; set; } = new();

    [BsonElement("version")]
    [Required(ErrorMessage = "Version is required")]
    [StringLength(50, ErrorMessage = "Version cannot exceed 50 characters")]
    public string Version { get; set; } = string.Empty;

    [BsonElement("name")]
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("protocolId")]
    [Required(ErrorMessage = "ProtocolId is required")]
    public Guid ProtocolId { get; set; } = Guid.Empty;

    public override string GetCompositeKey() => $"{Address}_{Version}_{Name}";
}

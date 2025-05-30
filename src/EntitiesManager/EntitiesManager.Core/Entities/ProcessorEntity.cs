using EntitiesManager.Core.Entities.Base;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EntitiesManager.Core.Entities;

public class ProcessorEntity : BaseEntity
{
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

    [BsonElement("inputSchema")]
    [Required(ErrorMessage = "InputSchema is required")]
    [StringLength(5000, ErrorMessage = "InputSchema cannot exceed 5000 characters")]
    public string InputSchema { get; set; } = string.Empty;

    [BsonElement("outputSchema")]
    [Required(ErrorMessage = "OutputSchema is required")]
    [StringLength(5000, ErrorMessage = "OutputSchema cannot exceed 5000 characters")]
    public string OutputSchema { get; set; } = string.Empty;

    public override string GetCompositeKey() => $"{Version}";
}

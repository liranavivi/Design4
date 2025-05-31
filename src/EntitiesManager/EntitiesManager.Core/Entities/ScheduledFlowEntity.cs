using EntitiesManager.Core.Entities.Base;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EntitiesManager.Core.Entities;

public class ScheduledFlowEntity : BaseEntity
{
    [BsonElement("version")]
    [Required(ErrorMessage = "Version is required")]
    [StringLength(50, ErrorMessage = "Version cannot exceed 50 characters")]
    public string Version { get; set; } = string.Empty;

    [BsonElement("name")]
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("sourceId")]
    [Required(ErrorMessage = "SourceId is required")]
    public Guid SourceId { get; set; }

    [BsonElement("destinationIds")]
    public List<Guid> DestinationIds { get; set; } = new List<Guid>();

    [BsonElement("flowId")]
    [Required(ErrorMessage = "FlowId is required")]
    public Guid FlowId { get; set; }

    public override string GetCompositeKey() => string.Empty; // ScheduledFlowEntity no longer uses composite keys
}

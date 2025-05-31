using EntitiesManager.Core.Entities.Base;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EntitiesManager.Core.Entities;

public class OrchestratedFlowEntity : BaseEntity
{
    [BsonElement("version")]
    [Required(ErrorMessage = "Version is required")]
    [StringLength(50, ErrorMessage = "Version cannot exceed 50 characters")]
    public string Version { get; set; } = string.Empty;

    [BsonElement("name")]
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("assignmentIds")]
    public List<Guid> AssignmentIds { get; set; } = new List<Guid>();

    [BsonElement("flowId")]
    [Required(ErrorMessage = "FlowId is required")]
    public Guid FlowId { get; set; }

    public override string GetCompositeKey() => $"{Name}_{Version}"; // Use Name + Version as unique identifier
}

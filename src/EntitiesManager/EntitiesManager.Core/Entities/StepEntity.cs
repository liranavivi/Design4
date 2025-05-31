using EntitiesManager.Core.Entities.Base;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EntitiesManager.Core.Entities;

public class StepEntity : BaseEntity
{
    [BsonElement("entityId")]
    [Required(ErrorMessage = "EntityId is required")]
    public Guid EntityId { get; set; } = Guid.Empty;

    [BsonElement("nextStepIds")]
    public List<Guid> NextStepIds { get; set; } = new List<Guid>();

    public override string GetCompositeKey() => $"{EntityId}"; // Use EntityId as unique identifier
}

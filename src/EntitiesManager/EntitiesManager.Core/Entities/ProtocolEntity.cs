using EntitiesManager.Core.Entities.Base;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EntitiesManager.Core.Entities;

public class ProtocolEntity : BaseEntity
{
    [BsonElement("name")]
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    public override string GetCompositeKey() => $"{Name}";
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EntitiesManager.Core.Entities.Base;

public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } // MongoDB will auto-generate

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [BsonElement("updatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;

    [BsonElement("description")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Abstract method for composite key validation
    /// </summary>
    public abstract string GetCompositeKey();

    /// <summary>
    /// Helper method to check if entity is new (no ID assigned yet)
    /// </summary>
    [BsonIgnore]
    public bool IsNew => Id == Guid.Empty;
}

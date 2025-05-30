namespace EntitiesManager.Infrastructure.MassTransit.Events;

public class StepCreatedEvent
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; } = Guid.Empty;
    public List<Guid> NextStepIds { get; set; } = new List<Guid>();
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class StepUpdatedEvent
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; } = Guid.Empty;
    public List<Guid> NextStepIds { get; set; } = new List<Guid>();
    public string Description { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class StepDeletedEvent
{
    public Guid Id { get; set; }
    public DateTime DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}

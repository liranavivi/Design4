namespace EntitiesManager.Infrastructure.MassTransit.Events;

public class ImporterCreatedEvent
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ProtocolId { get; set; } = Guid.Empty;
    public string OutputSchema { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class ImporterUpdatedEvent
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ProtocolId { get; set; } = Guid.Empty;
    public string OutputSchema { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class ImporterDeletedEvent
{
    public Guid Id { get; set; }
    public DateTime DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}

namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateTaskScheduledCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ScheduledFlowId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateTaskScheduledCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ScheduledFlowId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteTaskScheduledCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetTaskScheduledQuery
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}

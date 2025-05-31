namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateScheduledFlowCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public List<Guid> DestinationIds { get; set; } = new List<Guid>();
    public Guid FlowId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateScheduledFlowCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public List<Guid> DestinationIds { get; set; } = new List<Guid>();
    public Guid FlowId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteScheduledFlowCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetScheduledFlowQuery
{
    public Guid? Id { get; set; }
    // CompositeKey removed since ScheduledFlowEntity no longer uses composite keys
}

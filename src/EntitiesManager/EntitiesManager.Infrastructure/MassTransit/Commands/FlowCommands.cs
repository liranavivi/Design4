namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateFlowCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> StepIds { get; set; } = new List<Guid>();
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateFlowCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> StepIds { get; set; } = new List<Guid>();
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteFlowCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetFlowQuery
{
    public Guid? Id { get; set; }
    // CompositeKey removed since FlowEntity no longer uses composite keys
}

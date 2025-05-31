namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateOrchestratedFlowCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> AssignmentIds { get; set; } = new List<Guid>();
    public Guid FlowId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateOrchestratedFlowCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> AssignmentIds { get; set; } = new List<Guid>();
    public Guid FlowId { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteOrchestratedFlowCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetOrchestratedFlowQuery
{
    public Guid? Id { get; set; }
    // CompositeKey removed since OrchestratedFlowEntity no longer uses composite keys
}

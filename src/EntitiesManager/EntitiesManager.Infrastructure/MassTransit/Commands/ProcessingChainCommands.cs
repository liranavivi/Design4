namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateProcessingChainCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> StepIds { get; set; } = new List<Guid>();
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateProcessingChainCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> StepIds { get; set; } = new List<Guid>();
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteProcessingChainCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetProcessingChainQuery
{
    public Guid? Id { get; set; }
    // CompositeKey removed since ProcessingChainEntity no longer uses composite keys
}

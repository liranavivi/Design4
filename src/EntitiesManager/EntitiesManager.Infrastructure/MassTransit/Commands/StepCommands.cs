namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateStepCommand
{
    public Guid EntityId { get; set; } = Guid.Empty;
    public List<Guid> NextStepIds { get; set; } = new List<Guid>();
    public string Description { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateStepCommand
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; } = Guid.Empty;
    public List<Guid> NextStepIds { get; set; } = new List<Guid>();
    public string Description { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteStepCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetStepQuery
{
    public Guid? Id { get; set; }
    public Guid? EntityId { get; set; }
    // CompositeKey removed since StepEntity no longer uses composite keys
}

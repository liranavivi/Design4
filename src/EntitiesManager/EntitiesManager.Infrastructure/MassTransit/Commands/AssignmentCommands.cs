namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateAssignmentCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid StepId { get; set; }
    public List<Guid> EntityIds { get; set; } = new List<Guid>();
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateAssignmentCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid StepId { get; set; }
    public List<Guid> EntityIds { get; set; } = new List<Guid>();
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteAssignmentCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetAssignmentQuery
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}

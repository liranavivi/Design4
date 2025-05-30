namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateStepCommand
{
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateStepCommand
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
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
    public string? CompositeKey { get; set; }
}

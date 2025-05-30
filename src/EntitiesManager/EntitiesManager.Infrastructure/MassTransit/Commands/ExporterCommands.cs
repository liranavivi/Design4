namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateExporterCommand
{
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateExporterCommand
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteExporterCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetExporterQuery
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}

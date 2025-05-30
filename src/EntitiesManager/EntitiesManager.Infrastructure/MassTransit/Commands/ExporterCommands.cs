namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateExporterCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ProtocolId { get; set; } = Guid.Empty;
    public string InputSchema { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateExporterCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ProtocolId { get; set; } = Guid.Empty;
    public string InputSchema { get; set; } = string.Empty;
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

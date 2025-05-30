namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateImporterCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ProtocolId { get; set; } = Guid.Empty;
    public string OutputSchema { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateImporterCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ProtocolId { get; set; } = Guid.Empty;
    public string OutputSchema { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteImporterCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetImporterQuery
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}

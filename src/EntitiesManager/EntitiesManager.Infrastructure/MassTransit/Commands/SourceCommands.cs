namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateSourceCommand
{
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public Guid ProtocolId { get; set; } = Guid.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateSourceCommand
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public Guid ProtocolId { get; set; } = Guid.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteSourceCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetSourceQuery
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}

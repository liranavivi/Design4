namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateDestinationCommand
{
    public string Address { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Configuration { get; set; } = new();
    public Guid ProtocolId { get; set; } = Guid.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateDestinationCommand
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

public class DeleteDestinationCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetDestinationQuery
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}

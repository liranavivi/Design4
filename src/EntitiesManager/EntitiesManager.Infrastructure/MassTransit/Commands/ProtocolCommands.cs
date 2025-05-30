namespace EntitiesManager.Infrastructure.MassTransit.Commands;

public class CreateProtocolCommand
{
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class UpdateProtocolCommand
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class DeleteProtocolCommand
{
    public Guid Id { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public class GetProtocolQuery
{
    public Guid? Id { get; set; }
    public string? CompositeKey { get; set; }
}

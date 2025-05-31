using EntitiesManager.Core.Interfaces.Repositories;
using EntitiesManager.Infrastructure.MassTransit.Commands;
using EntitiesManager.Infrastructure.MassTransit.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EntitiesManager.Infrastructure.MassTransit.Consumers.Assignment;

public class DeleteAssignmentCommandConsumer : IConsumer<DeleteAssignmentCommand>
{
    private readonly IAssignmentEntityRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteAssignmentCommandConsumer> _logger;

    public DeleteAssignmentCommandConsumer(
        IAssignmentEntityRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteAssignmentCommandConsumer> logger)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteAssignmentCommand> context)
    {
        _logger.LogInformation("Processing DeleteAssignmentCommand for ID {Id}", context.Message.Id);

        try
        {
            var deleted = await _repository.DeleteAsync(context.Message.Id);

            if (deleted)
            {
                await _publishEndpoint.Publish(new AssignmentDeletedEvent
                {
                    Id = context.Message.Id,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = context.Message.RequestedBy
                });

                await context.RespondAsync(new { Success = true, Message = "Entity deleted successfully" });
                _logger.LogInformation("Successfully processed DeleteAssignmentCommand for ID {Id}", context.Message.Id);
            }
            else
            {
                _logger.LogWarning("AssignmentEntity with ID {Id} not found for deletion", context.Message.Id);
                await context.RespondAsync(new { Success = false, Error = "Entity not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DeleteAssignmentCommand for ID {Id}", context.Message.Id);
            throw;
        }
    }
}

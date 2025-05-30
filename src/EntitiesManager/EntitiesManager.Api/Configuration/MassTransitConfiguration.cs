using EntitiesManager.Infrastructure.MassTransit.Consumers.Source;
using EntitiesManager.Infrastructure.MassTransit.Consumers.Step;
using EntitiesManager.Infrastructure.MassTransit.Consumers.Destination;
using EntitiesManager.Infrastructure.MassTransit.Consumers.Protocol;
using EntitiesManager.Infrastructure.MassTransit.Consumers.Importer;
using EntitiesManager.Infrastructure.MassTransit.Consumers.Exporter;
using EntitiesManager.Infrastructure.MassTransit.Consumers.Processor;
using EntitiesManager.Infrastructure.MassTransit.Consumers.ProcessingChain;
using EntitiesManager.Infrastructure.MassTransit.Consumers.Flow;
using EntitiesManager.Infrastructure.MassTransit.Consumers.TaskScheduled;
using EntitiesManager.Infrastructure.MassTransit.Consumers.ScheduledFlow;
using MassTransit;

namespace EntitiesManager.Api.Configuration;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // Add source consumers
            x.AddConsumer<CreateSourceCommandConsumer>();
            x.AddConsumer<UpdateSourceCommandConsumer>();
            x.AddConsumer<DeleteSourceCommandConsumer>();
            x.AddConsumer<GetSourceQueryConsumer>();

            // Add step consumers
            x.AddConsumer<CreateStepCommandConsumer>();
            x.AddConsumer<UpdateStepCommandConsumer>();
            x.AddConsumer<DeleteStepCommandConsumer>();
            x.AddConsumer<GetStepQueryConsumer>();

            // Add destination consumers
            x.AddConsumer<CreateDestinationCommandConsumer>();
            x.AddConsumer<UpdateDestinationCommandConsumer>();
            x.AddConsumer<DeleteDestinationCommandConsumer>();
            x.AddConsumer<GetDestinationQueryConsumer>();

            // Add protocol consumers
            x.AddConsumer<CreateProtocolCommandConsumer>();
            x.AddConsumer<UpdateProtocolCommandConsumer>();
            x.AddConsumer<DeleteProtocolCommandConsumer>();
            x.AddConsumer<GetProtocolQueryConsumer>();

            // Add importer consumers
            x.AddConsumer<CreateImporterCommandConsumer>();
            x.AddConsumer<UpdateImporterCommandConsumer>();
            x.AddConsumer<DeleteImporterCommandConsumer>();
            x.AddConsumer<GetImporterQueryConsumer>();

            // Add exporter consumers
            x.AddConsumer<CreateExporterCommandConsumer>();
            x.AddConsumer<UpdateExporterCommandConsumer>();
            x.AddConsumer<DeleteExporterCommandConsumer>();
            x.AddConsumer<GetExporterQueryConsumer>();

            // Add processor consumers
            x.AddConsumer<CreateProcessorCommandConsumer>();
            x.AddConsumer<UpdateProcessorCommandConsumer>();
            x.AddConsumer<DeleteProcessorCommandConsumer>();
            x.AddConsumer<GetProcessorQueryConsumer>();

            // Add processing chain consumers
            x.AddConsumer<CreateProcessingChainCommandConsumer>();
            x.AddConsumer<UpdateProcessingChainCommandConsumer>();
            x.AddConsumer<DeleteProcessingChainCommandConsumer>();
            x.AddConsumer<GetProcessingChainQueryConsumer>();

            // Add flow consumers
            x.AddConsumer<CreateFlowCommandConsumer>();
            x.AddConsumer<UpdateFlowCommandConsumer>();
            x.AddConsumer<DeleteFlowCommandConsumer>();
            x.AddConsumer<GetFlowQueryConsumer>();

            // Add task scheduled consumers
            x.AddConsumer<CreateTaskScheduledCommandConsumer>();
            x.AddConsumer<UpdateTaskScheduledCommandConsumer>();
            x.AddConsumer<DeleteTaskScheduledCommandConsumer>();
            x.AddConsumer<GetTaskScheduledQueryConsumer>();

            // Add scheduled flow consumers
            x.AddConsumer<CreateScheduledFlowCommandConsumer>();
            x.AddConsumer<UpdateScheduledFlowCommandConsumer>();
            x.AddConsumer<DeleteScheduledFlowCommandConsumer>();
            x.AddConsumer<GetScheduledFlowQueryConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqSettings = configuration.GetSection("RabbitMQ");

                cfg.Host(rabbitMqSettings["Host"] ?? "localhost", rabbitMqSettings["VirtualHost"] ?? "/", h =>
                {
                    h.Username(rabbitMqSettings["Username"] ?? "guest");
                    h.Password(rabbitMqSettings["Password"] ?? "guest");
                });

                // Configure retry policy
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15),
                    TimeSpan.FromSeconds(30)
                ));

                // Configure error handling
                // cfg.UseInMemoryOutbox(); // Commented out due to obsolete warning

                // Configure endpoints to use message type routing
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}

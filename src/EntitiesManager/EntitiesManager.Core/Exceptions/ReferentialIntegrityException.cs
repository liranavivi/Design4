using System;
using System.Collections.Generic;
using System.Linq;
using EntitiesManager.Core.Interfaces.Services;

namespace EntitiesManager.Core.Exceptions;

public class ReferentialIntegrityException : Exception
{
    public ProtocolReferenceInfo? ProtocolReferences { get; }
    public SourceEntityReferenceInfo? SourceEntityReferences { get; }
    public DestinationEntityReferenceInfo? DestinationEntityReferences { get; }
    public ImporterEntityReferenceInfo? ImporterEntityReferences { get; }
    public ExporterEntityReferenceInfo? ExporterEntityReferences { get; }
    public ProcessorEntityReferenceInfo? ProcessorEntityReferences { get; }
    public StepEntityReferenceInfo? StepEntityReferences { get; }
    public FlowEntityReferenceInfo? FlowEntityReferences { get; }
    public OrchestratedFlowEntityReferenceInfo? OrchestratedFlowEntityReferences { get; }

    // Constructor for ProtocolEntity validation
    public ReferentialIntegrityException(string message, ProtocolReferenceInfo references)
        : base(message)
    {
        ProtocolReferences = references;
    }

    public ReferentialIntegrityException(string message, ProtocolReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        ProtocolReferences = references;
    }

    // Constructor for SourceEntity validation
    public ReferentialIntegrityException(string message, SourceEntityReferenceInfo references)
        : base(message)
    {
        SourceEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, SourceEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        SourceEntityReferences = references;
    }

    // Constructor for DestinationEntity validation
    public ReferentialIntegrityException(string message, DestinationEntityReferenceInfo references)
        : base(message)
    {
        DestinationEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, DestinationEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        DestinationEntityReferences = references;
    }

    // Constructor for ImporterEntity validation
    public ReferentialIntegrityException(string message, ImporterEntityReferenceInfo references)
        : base(message)
    {
        ImporterEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, ImporterEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        ImporterEntityReferences = references;
    }

    // Constructor for ExporterEntity validation
    public ReferentialIntegrityException(string message, ExporterEntityReferenceInfo references)
        : base(message)
    {
        ExporterEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, ExporterEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        ExporterEntityReferences = references;
    }

    // Constructor for ProcessorEntity validation
    public ReferentialIntegrityException(string message, ProcessorEntityReferenceInfo references)
        : base(message)
    {
        ProcessorEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, ProcessorEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        ProcessorEntityReferences = references;
    }

    // Constructor for StepEntity validation
    public ReferentialIntegrityException(string message, StepEntityReferenceInfo references)
        : base(message)
    {
        StepEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, StepEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        StepEntityReferences = references;
    }

    // Constructor for FlowEntity validation
    public ReferentialIntegrityException(string message, FlowEntityReferenceInfo references)
        : base(message)
    {
        FlowEntityReferences = references;
    }

    // Constructor for OrchestratedFlowEntity validation
    public ReferentialIntegrityException(string message, OrchestratedFlowEntityReferenceInfo references)
        : base(message)
    {
        OrchestratedFlowEntityReferences = references;
    }

    public ReferentialIntegrityException(string message, FlowEntityReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        FlowEntityReferences = references;
    }

    public string GetDetailedMessage()
    {
        // Handle OrchestratedFlowEntity references
        if (OrchestratedFlowEntityReferences?.HasReferences == true)
        {
            // TaskScheduledEntity removed - OrchestratedFlowEntity no longer has references to validate
            return $"Cannot modify OrchestratedFlowEntity. Found references.";
        }

        // Handle FlowEntity references
        if (FlowEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify FlowEntity. Found {FlowEntityReferences.OrchestratedFlowEntityCount} OrchestratedFlowEntity reference{(FlowEntityReferences.OrchestratedFlowEntityCount > 1 ? "s" : "")}.";
        }

        // Handle StepEntity references
        if (StepEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify StepEntity. Found {StepEntityReferences.FlowEntityCount} FlowEntity reference{(StepEntityReferences.FlowEntityCount > 1 ? "s" : "")}.";
        }

        // Handle ProcessorEntity references
        if (ProcessorEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify ProcessorEntity. Found {ProcessorEntityReferences.StepEntityCount} StepEntity reference{(ProcessorEntityReferences.StepEntityCount > 1 ? "s" : "")}.";
        }

        // Handle ExporterEntity references
        if (ExporterEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify ExporterEntity. Found {ExporterEntityReferences.StepEntityCount} StepEntity reference{(ExporterEntityReferences.StepEntityCount > 1 ? "s" : "")}.";
        }

        // Handle ImporterEntity references
        if (ImporterEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify ImporterEntity. Found {ImporterEntityReferences.StepEntityCount} StepEntity reference{(ImporterEntityReferences.StepEntityCount > 1 ? "s" : "")}.";
        }

        // Handle DestinationEntity references
        if (DestinationEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify DestinationEntity. Found references.";
        }

        // Handle SourceEntity references
        if (SourceEntityReferences?.HasReferences == true)
        {
            return $"Cannot modify SourceEntity. Found references.";
        }

        // Handle ProtocolEntity references
        if (ProtocolReferences?.HasReferences == true)
        {
            var referenceDetails = new List<string>();

            if (ProtocolReferences.SourceEntityCount > 0)
            {
                referenceDetails.Add($"{ProtocolReferences.SourceEntityCount} SourceEntity reference{(ProtocolReferences.SourceEntityCount > 1 ? "s" : "")}");
            }

            if (ProtocolReferences.DestinationEntityCount > 0)
            {
                referenceDetails.Add($"{ProtocolReferences.DestinationEntityCount} DestinationEntity reference{(ProtocolReferences.DestinationEntityCount > 1 ? "s" : "")}");
            }

            var details = string.Join(" and ", referenceDetails);
            return $"Cannot delete ProtocolEntity. Found {details}.";
        }

        return Message;
    }
}

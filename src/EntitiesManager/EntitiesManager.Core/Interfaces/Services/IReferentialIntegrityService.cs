using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntitiesManager.Core.Interfaces.Services;

public interface IReferentialIntegrityService
{
    // ProtocolEntity validation methods
    Task<ReferentialIntegrityResult> ValidateProtocolDeletionAsync(Guid protocolId);
    Task<ReferentialIntegrityResult> ValidateProtocolUpdateAsync(Guid currentId, Guid newId);
    Task<ProtocolReferenceInfo> GetProtocolReferencesAsync(Guid protocolId);

    // SourceEntity validation methods
    Task<ReferentialIntegrityResult> ValidateSourceEntityDeletionAsync(Guid sourceId);
    Task<ReferentialIntegrityResult> ValidateSourceEntityUpdateAsync(Guid sourceId);
    Task<SourceEntityReferenceInfo> GetSourceEntityReferencesAsync(Guid sourceId);

    Task<ReferentialIntegrityResult> ValidateDestinationEntityDeletionAsync(Guid destinationId);
    Task<ReferentialIntegrityResult> ValidateDestinationEntityUpdateAsync(Guid destinationId);
    Task<DestinationEntityReferenceInfo> GetDestinationEntityReferencesAsync(Guid destinationId);

    Task<ReferentialIntegrityResult> ValidateImporterEntityDeletionAsync(Guid importerId);
    Task<ReferentialIntegrityResult> ValidateImporterEntityUpdateAsync(Guid importerId);
    Task<ImporterEntityReferenceInfo> GetImporterEntityReferencesAsync(Guid importerId);

    Task<ReferentialIntegrityResult> ValidateExporterEntityDeletionAsync(Guid exporterId);
    Task<ReferentialIntegrityResult> ValidateExporterEntityUpdateAsync(Guid exporterId);
    Task<ExporterEntityReferenceInfo> GetExporterEntityReferencesAsync(Guid exporterId);

    Task<ReferentialIntegrityResult> ValidateProcessorEntityDeletionAsync(Guid processorId);
    Task<ReferentialIntegrityResult> ValidateProcessorEntityUpdateAsync(Guid processorId);
    Task<ProcessorEntityReferenceInfo> GetProcessorEntityReferencesAsync(Guid processorId);

    Task<ReferentialIntegrityResult> ValidateStepEntityDeletionAsync(Guid stepId);
    Task<ReferentialIntegrityResult> ValidateStepEntityUpdateAsync(Guid stepId);
    Task<StepEntityReferenceInfo> GetStepEntityReferencesAsync(Guid stepId);

    Task<ReferentialIntegrityResult> ValidateFlowEntityDeletionAsync(Guid flowId);
    Task<ReferentialIntegrityResult> ValidateFlowEntityUpdateAsync(Guid flowId);
    Task<FlowEntityReferenceInfo> GetFlowEntityReferencesAsync(Guid flowId);

    // OrchestratedFlowEntity validation methods
    Task<ReferentialIntegrityResult> ValidateOrchestratedFlowEntityDeletionAsync(Guid orchestratedFlowId);
    Task<ReferentialIntegrityResult> ValidateOrchestratedFlowEntityUpdateAsync(Guid orchestratedFlowId);
    Task<OrchestratedFlowEntityReferenceInfo> GetOrchestratedFlowEntityReferencesAsync(Guid orchestratedFlowId);
}

public class ReferentialIntegrityResult
{
    public bool IsValid { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public ProtocolReferenceInfo? ProtocolReferences { get; private set; }
    public SourceEntityReferenceInfo? SourceEntityReferences { get; set; }
    public DestinationEntityReferenceInfo? DestinationEntityReferences { get; set; }
    public ImporterEntityReferenceInfo? ImporterEntityReferences { get; private set; }
    public ExporterEntityReferenceInfo? ExporterEntityReferences { get; private set; }
    public ProcessorEntityReferenceInfo? ProcessorEntityReferences { get; private set; }
    public StepEntityReferenceInfo? StepEntityReferences { get; private set; }
    public FlowEntityReferenceInfo? FlowEntityReferences { get; private set; }
    public OrchestratedFlowEntityReferenceInfo? OrchestratedFlowEntityReferences { get; set; }
    public TimeSpan ValidationDuration { get; set; }

    public static ReferentialIntegrityResult Valid() => new() { IsValid = true };

    public static ReferentialIntegrityResult Invalid(string message, ProtocolReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        ProtocolReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, SourceEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        SourceEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, DestinationEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        DestinationEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, ImporterEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        ImporterEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, ExporterEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        ExporterEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, ProcessorEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        ProcessorEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, StepEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        StepEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, FlowEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        FlowEntityReferences = references
    };

    public static ReferentialIntegrityResult Invalid(string message, OrchestratedFlowEntityReferenceInfo references) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        OrchestratedFlowEntityReferences = references
    };
}

public class ProtocolReferenceInfo
{
    public long SourceEntityCount { get; set; }
    public long DestinationEntityCount { get; set; }
    public long TotalReferences => SourceEntityCount + DestinationEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (SourceEntityCount > 0) types.Add($"SourceEntity ({SourceEntityCount} records)");
        if (DestinationEntityCount > 0) types.Add($"DestinationEntity ({DestinationEntityCount} records)");
        return types;
    }
}

public class SourceEntityReferenceInfo
{
    // SourceEntity no longer referenced by OrchestratedFlowEntity (Assignment-focused architecture)
    public long TotalReferences => 0;
    public bool HasReferences => false;

    public List<string> GetReferencingEntityTypes()
    {
        return new List<string>();
    }
}

public class DestinationEntityReferenceInfo
{
    // DestinationEntity no longer referenced by OrchestratedFlowEntity (Assignment-focused architecture)
    public long TotalReferences => 0;
    public bool HasReferences => false;

    public List<string> GetReferencingEntityTypes()
    {
        return new List<string>();
    }
}

public class ImporterEntityReferenceInfo
{
    public long StepEntityCount { get; set; }
    public long TotalReferences => StepEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (StepEntityCount > 0) types.Add($"StepEntity ({StepEntityCount} records)");
        return types;
    }
}

public class ExporterEntityReferenceInfo
{
    public long StepEntityCount { get; set; }
    public long TotalReferences => StepEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (StepEntityCount > 0) types.Add($"StepEntity ({StepEntityCount} records)");
        return types;
    }
}

public class ProcessorEntityReferenceInfo
{
    public long StepEntityCount { get; set; }
    public long TotalReferences => StepEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (StepEntityCount > 0) types.Add($"StepEntity ({StepEntityCount} records)");
        return types;
    }
}

public class StepEntityReferenceInfo
{
    public long FlowEntityCount { get; set; }
    public long TotalReferences => FlowEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (FlowEntityCount > 0) types.Add($"FlowEntity ({FlowEntityCount} records)");
        return types;
    }
}

public class FlowEntityReferenceInfo
{
    public long OrchestratedFlowEntityCount { get; set; }
    public long TotalReferences => OrchestratedFlowEntityCount;
    public bool HasReferences => TotalReferences > 0;

    public List<string> GetReferencingEntityTypes()
    {
        var types = new List<string>();
        if (OrchestratedFlowEntityCount > 0) types.Add($"OrchestratedFlowEntity ({OrchestratedFlowEntityCount} records)");
        return types;
    }
}

public class OrchestratedFlowEntityReferenceInfo
{
    // TaskScheduledEntity removed - OrchestratedFlowEntity now only manages AssignmentIds relationships
    public long TotalReferences => 0; // No references to validate anymore
    public bool HasReferences => false; // Always false since TaskScheduled is removed

    public List<string> GetReferencingEntityTypes()
    {
        // Return empty list since TaskScheduledEntity is removed
        return new List<string>();
    }
}

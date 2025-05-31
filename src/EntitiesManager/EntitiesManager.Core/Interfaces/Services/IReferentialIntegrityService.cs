using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntitiesManager.Core.Interfaces.Services;

public interface IReferentialIntegrityService
{
    Task<ReferentialIntegrityResult> ValidateProtocolDeletionAsync(Guid protocolId);
    Task<ReferentialIntegrityResult> ValidateProtocolUpdateAsync(Guid currentId, Guid newId);
    Task<ProtocolReferenceInfo> GetProtocolReferencesAsync(Guid protocolId);
}

public class ReferentialIntegrityResult
{
    public bool IsValid { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public ProtocolReferenceInfo References { get; private set; } = new();
    public TimeSpan ValidationDuration { get; set; }

    public static ReferentialIntegrityResult Valid() => new() { IsValid = true };
    
    public static ReferentialIntegrityResult Invalid(string message, ProtocolReferenceInfo references) => new() 
    { 
        IsValid = false, 
        ErrorMessage = message, 
        References = references 
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

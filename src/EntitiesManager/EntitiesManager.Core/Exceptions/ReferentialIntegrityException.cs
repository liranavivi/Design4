using System;
using System.Collections.Generic;
using System.Linq;
using EntitiesManager.Core.Interfaces.Services;

namespace EntitiesManager.Core.Exceptions;

public class ReferentialIntegrityException : Exception
{
    public ProtocolReferenceInfo References { get; }

    public ReferentialIntegrityException(string message, ProtocolReferenceInfo references)
        : base(message)
    {
        References = references;
    }

    public ReferentialIntegrityException(string message, ProtocolReferenceInfo references, Exception innerException)
        : base(message, innerException)
    {
        References = references;
    }

    public string GetDetailedMessage()
    {
        if (References == null || References.TotalReferences == 0)
            return Message;

        var referenceDetails = new List<string>();

        if (References.SourceEntityCount > 0)
        {
            referenceDetails.Add($"{References.SourceEntityCount} SourceEntity reference{(References.SourceEntityCount > 1 ? "s" : "")}");
        }

        if (References.DestinationEntityCount > 0)
        {
            referenceDetails.Add($"{References.DestinationEntityCount} DestinationEntity reference{(References.DestinationEntityCount > 1 ? "s" : "")}");
        }

        var details = string.Join(" and ", referenceDetails);

        return $"Cannot delete ProtocolEntity. Found {details}.";
    }
}

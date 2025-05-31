using System;
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
}

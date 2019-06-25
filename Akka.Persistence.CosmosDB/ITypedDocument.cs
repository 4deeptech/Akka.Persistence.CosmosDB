using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.Persistence.CosmosDB
{
    public interface ITypedDocument
    {
        string DocumentType { get; }
    }
}

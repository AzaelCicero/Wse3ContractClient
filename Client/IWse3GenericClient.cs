using System;
using System.Collections.Generic;

namespace Wse3ContractClient.Client
{
    internal interface IWse3GenericClient
    {
        object Invoke(string action, object[] args, IList<string> paramNames, Type returnType);
    }
}
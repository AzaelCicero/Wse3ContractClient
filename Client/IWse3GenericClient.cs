using System;
using System.Collections.Generic;
using System.Reflection;

namespace Wse3ContractClient.Client
{
    internal interface IWse3GenericClient
    {
        object Invoke(string action, object[] args, List<string> paramNames, IList<Type> paramTypes, Type returnType);
    }
}
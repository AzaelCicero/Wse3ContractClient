using System;
using System.Dynamic;
using System.Linq;

namespace Wse3ContractClient.Client
{
    internal class DynamicWebServiceClient : ImpromptuInterface.Dynamic.ImpromptuObject
    {
        private readonly IWse3GenericClient _client;

        public DynamicWebServiceClient(IWse3GenericClient client)
        {
            _client = client;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            Type realReturnType;
            TryTypeForName(binder.Name, out realReturnType);
            var methodInfo = KnownInterfaces.First().GetMethod(binder.Name);
            var paramNames = methodInfo.GetParameters().Select(info => info.Name).ToList();

            var response = _client.Invoke(binder.Name, args, paramNames, realReturnType);
            result = response;

            return true;
        }
    }
}
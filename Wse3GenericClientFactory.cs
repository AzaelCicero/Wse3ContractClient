using Wse3ContractClient.Client;
using Wse3ContractClient.Headers;

namespace Wse3ContractClient
{
    public class Wse3GenericClientFactory : IWse3GenericClientFactory
    {
        private readonly IWsseSecurityMessageHeaderFactory _securityMessageHeaderFactory;

        public Wse3GenericClientFactory() : this(null)
        {
        }

        public Wse3GenericClientFactory(IWsseSecurityMessageHeaderFactory securityMessageHeaderFactory)
        {
            _securityMessageHeaderFactory = securityMessageHeaderFactory ?? new WsseSecurityMessageHeaderFactory();
        }

        public TContract Create<TContract>(string uri, string @namespace, string userName, string password, string mashineName) where TContract : class
        {
            var wse3GenericClient = new Wse3GenericClient(uri, @namespace, userName, password, mashineName, _securityMessageHeaderFactory);
            return new DynamicWebServiceClient(wse3GenericClient).ActLike<TContract>();
        }
    }
}
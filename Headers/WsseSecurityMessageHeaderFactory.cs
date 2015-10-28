using System.ServiceModel.Channels;

namespace Wse3ContractClient.Headers
{
    public class WsseSecurityMessageHeaderFactory : IWsseSecurityMessageHeaderFactory
    {
        public virtual MessageHeader CreateSecurityHeader(string userName, string password, string mashineName)
        {
            return new WsseSecurityMessageHeader(userName, password, mashineName);
        }
    }
}
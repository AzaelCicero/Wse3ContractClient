using System.ServiceModel.Channels;

namespace Wse3ContractClient.Headers
{
    public interface IWsseSecurityMessageHeaderFactory
    {
        MessageHeader CreateSecurityHeader(string userName, string password, string mashineName);
    }
}
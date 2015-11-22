using ImpromptuInterface;

namespace Wse3ContractClient
{
    public interface IWse3GenericClientFactory
    {
        TContract Create<TContract>(string uri, string @namespace, string userName, string password, string machineName) where TContract : class;
    }
}
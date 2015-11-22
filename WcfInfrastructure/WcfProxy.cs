using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Wse3ContractClient.WcfInfrastructure
{
    internal class WcfProxy : ClientBase<IFakeContract>
    {
        public WcfProxy(Binding binding, string uri) : base(binding, new EndpointAddress(uri))
        {
        }

        public Message Send(Message request)
        {
            return Channel.Send(request);
        }
    }
}
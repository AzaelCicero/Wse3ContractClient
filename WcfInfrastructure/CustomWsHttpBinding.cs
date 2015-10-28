using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Wse3ContractClient.WcfInfrastructure
{
    public class CustomWsHttpBinding : WSHttpBinding
    {
        public override BindingElementCollection CreateBindingElements()
        {
            var elements = base.CreateBindingElements();

            var encodingElement = elements.Find<MessageEncodingBindingElement>();
            encodingElement.MessageVersion = MessageVersion.Soap12WSAddressingAugust2004;

            return elements;
        }
    }
}
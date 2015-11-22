using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Wse3ContractClient.WcfInfrastructure
{
    [ServiceContract(Namespace = "http://hackOnWcf.ToActAsServicestack.net/")]
    internal interface IFakeContract
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message Send(Message requestMsg);
    }
}
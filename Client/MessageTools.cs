using System.ServiceModel.Channels;

namespace Wse3ContractClient.Client
{
    public static class MessageTools
    {
        public static string ReadMessageAndNotDestroyOriginal(ref Message message)
        {
            var bufferedCopy = message.CreateBufferedCopy(int.MaxValue);
            message = bufferedCopy.CreateMessage();

            var toRead = bufferedCopy.CreateMessage();
            return toRead.GetReaderAtBodyContents().ReadOuterXml();
        }
    }
}
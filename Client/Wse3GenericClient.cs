using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using log4net;
using Wse3ContractClient.Headers;
using Wse3ContractClient.WcfInfrastructure;
using Wse3ContractClient.XmlMinions;

namespace Wse3ContractClient.Client
{
    internal class Wse3GenericClient : IWse3GenericClient
    {
        private ILog _logger = LogManager.GetLogger(typeof(Wse3GenericClient));

        private readonly string _namespace;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _mashineName;
        private readonly string _uri;

        private readonly IWsseSecurityMessageHeaderFactory _securityMessageHeaderFactory;

        private readonly WSHttpBinding _binding;
        private readonly WcfProxy _wcfClient;
        private readonly TypeTagSoap12Converter _tagTypeNameConverter;


        private MessageVersion MessageVersion
        {
            get { return MessageVersion.Soap12WSAddressingAugust2004; }
        }

        public Wse3GenericClient(string uri, string @namespace, string userName, string password, string mashineName, IWsseSecurityMessageHeaderFactory securityMessageHeaderFactory)
        {
            _namespace = @namespace;
            _userName = userName;
            _password = password;
            _mashineName = mashineName;
            _securityMessageHeaderFactory = securityMessageHeaderFactory;

            _uri = uri;

            _binding = new CustomWsHttpBinding
            {
                MaxReceivedMessageSize = int.MaxValue,
                HostNameComparisonMode = HostNameComparisonMode.StrongWildcard,
                Security = { Mode = SecurityMode.None },
                AllowCookies = false
            };

            _wcfClient = new WcfProxy(_binding, _uri);
            _tagTypeNameConverter = new TypeTagSoap12Converter();
        }

        public object Invoke(string action, object[] args, List<string> paramNames, IList<Type> paramTypes, Type returnType)
        {
            using (var stream = BuildRequestBody(action, args, paramNames, paramTypes))
            {
                var request = CreateRequest(action, stream);
                
                _logger.DebugFormat("Sending message to {0}: \n\r {1}", _uri, MessageTools.ReadMessageAndNotDestroyOriginal(ref request));
                var response = _wcfClient.Send(request);
                _logger.DebugFormat("Received message from {0}: \n\r {1}", _uri, MessageTools.ReadMessageAndNotDestroyOriginal(ref response));
                
                var responseBuffer = response.CreateBufferedCopy(int.MaxValue);

                HandleFaults(responseBuffer);
                return HandleResult(responseBuffer, returnType);
            }
        }

        private object HandleResult(MessageBuffer responseBuffer, Type returnType)
        {
            if (returnType == typeof(void))
            {
                return null;
            }

            var xml = responseBuffer.CreateMessage().GetReaderAtBodyContents().ReadOuterXml();

            return Deserialize(xml, returnType);
        }

        private static void HandleFaults(MessageBuffer responseBuffer)
        {
            var response = responseBuffer.CreateMessage();
            if (!response.IsFault)
            {
                return;
            }
            
            var exception = CreateExceptionOnFault(response.GetReaderAtBodyContents());

            throw exception;
        }

        private static Exception CreateExceptionOnFault(XmlDictionaryReader faultBody)
        {
            var document = XDocument.Parse(faultBody.ReadOuterXml());
            var details = document.Descendants("detail").First();

            var type = details.Elements("ExceptionType").FirstOrDefault();
            var message = details.Elements("ExceptionMessage").FirstOrDefault();
            var info = details.Elements("ExceptionInfo").FirstOrDefault();

            return new SoapException(string.Format(
                "{0}\n\r{1}\n\r{2}", 
                message != null ? message.Value : "Not included message.", 
                type != null ? type.Value : "Not included exception type.", 
                info != null ? info.Value : "Not included detailed information."), XmlQualifiedName.Empty);
        }

        private Message CreateRequest(string action, Stream messageBody)
        {
            var wsseSecurityHeader = _securityMessageHeaderFactory.CreateSecurityHeader(_userName, _password, _mashineName);
            var msg = Message.CreateMessage(MessageVersion, string.Format("{0}/{1}", _namespace, action), XmlReader.Create(messageBody));

            msg.Headers.To = new Uri(_uri);
            msg.Headers.Add(wsseSecurityHeader);
            AddSoapActionHttpHeader(msg, msg.Headers.Action);
            return msg;
        }

        private Stream BuildRequestBody(string action, object[] args, List<string> paramNames, IList<Type> paramTypes)
        {
            var stream = new MemoryStream();
            var writer = new XmlTextWriter(stream, Encoding.UTF8);

            writer.WriteStartElement(action, _namespace);
            for (var i = 0; i < paramNames.Count; i++)
            {
                var arg = args[i];
                var paramName = paramNames[i];
                if (arg == null)
                    continue;

                var stringWriter = new StringWriter();

                var serializer = new XmlSerializer(paramTypes[i], GetExtraTypesForArgument(arg));
                serializer.Serialize(stringWriter, arg);

                var serialized = XDocument.Parse(stringWriter.ToString());
                serialized.Root.Name = paramName;

                var validSoapXml = serialized.Root.ToString(SaveOptions.DisableFormatting);
                writer.WriteRaw(validSoapXml);
            }
            writer.WriteEndElement();
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        private static Type[] GetExtraTypesForArgument(object arg)
        {
            IEnumerable<Type> elementTypes = null;

            var enumerable = arg as IEnumerable;
            if (enumerable != null)
            {
                elementTypes = enumerable.Cast<object>().Select(o => o.GetType()).Distinct();
            }

            return elementTypes != null ? elementTypes.ToArray() : new[] { arg.GetType() };
        }

        private void AddSoapActionHttpHeader(Message request, string action)
        {
            HttpRequestMessageProperty httpRequestMessage;
            object httpRequestMessageObject;

            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out httpRequestMessageObject))
            {
                httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;
                httpRequestMessage.Headers["SOAPAction"] = string.Format("\"{0}\"", action);
            }
            else
            {
                httpRequestMessage = new HttpRequestMessageProperty();
                httpRequestMessage.Headers["SOAPAction"] = string.Format("\"{0}\"", action);

                request.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessage);
            }
        }

        private object Deserialize(string buffer, Type returnType)
        {
            // MAGIC: Remove stupid Response head, and Rename Result
            var document = XDocument.Parse(buffer);
            if (document.Root != null && document.Root.Elements().Count() == 1)
            {
                var resultElement = document.Root.Elements().First();
                resultElement.Name = _tagTypeNameConverter.GenerateTag(returnType);
                resultElement = XmlTools.RemoveAllNamespaces(resultElement);

                var validXml = resultElement.ToString(SaveOptions.OmitDuplicateNamespaces);
                var reader = new StringReader(validXml);

                var serializer = new XmlSerializer(returnType);
                return serializer.Deserialize(reader);
            }

            throw new NotSupportedException();
        }
    }
}
using System;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Channels;
using System.Web.Services.Protocols;
using System.Xml;
using Microsoft.Web.Services3;
using Microsoft.Web.Services3.Security.Tokens;

namespace Wse3ContractClient.Headers
{
    public class WsseSecurityMessageHeader : MessageHeader
    {
        private readonly string _userName;
        private readonly string _password;
        private readonly string _mashineName;
        
        internal WsseSecurityMessageHeader(string userName, string password, string mashineName)
        {
            _userName = userName;
            _password = password;
            _mashineName = mashineName;
        }

        protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            var nodes = CreateWsseSecurityHeader();
            foreach (var node in nodes)
            {
                writer.WriteRaw(node.OuterXml);
            }
        }
        
        public override string Name
        {
            get { return "Security"; }
        }

        public override string Namespace
        {
            get { return "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"; }
        }

        protected override void OnWriteStartHeader(XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            writer.WriteStartElement("wsse", Name, Namespace);
            WriteHeaderAttributes(writer, messageVersion);
        }

        private XmlNode[] CreateWsseSecurityHeader()
        {
            var token = CreateUsernameToken();

            var securityHeaderXml = SerializeWsseSecurityToken(token);

            return SplitWsseSecurityHeaderChilds(securityHeaderXml);
        }

        private UsernameToken CreateUsernameToken()
        {
            var currentInstanceGuid = Guid.NewGuid().ToString();
            var token = new UsernameToken(_userName, _password, PasswordOption.SendPlainText) { Id = currentInstanceGuid };

            AddTokenData(token, _mashineName, false);

            return token;
        }

        private static XmlNode[] SplitWsseSecurityHeaderChilds(XmlElement securityHeaderXml)
        {
            var wsseSecurityHeader = securityHeaderXml.ChildNodes[0].ChildNodes;
            return new[] { wsseSecurityHeader[0], wsseSecurityHeader[1] };
        }

        private static XmlElement SerializeWsseSecurityToken(UsernameToken token)
        {
            var s = typeof(Microsoft.Web.Services3.Security.Security).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            var defaultConstructor = s.FirstOrDefault(info => !info.GetParameters().Any());
            var security = (Microsoft.Web.Services3.Security.Security)defaultConstructor.Invoke(new object[0]);

            security.Tokens.Clear();
            security.Tokens.Add(token);
            var fakeSoap = new SoapEnvelope(SoapProtocolVersion.Soap12);
            security.SerializeXml(fakeSoap);

            var securityHeaderXml = fakeSoap.Header;
            return securityHeaderXml;
        }

        private static void AddTokenData(UsernameToken token, string mashineName, bool isTokenAuthenticated)
        {
            var document = new XmlDocument();
            var tokenElement = token.GetXml(document);

            var dataChild = document.CreateElement("wsse", "Data", string.Empty);
            var innerChild = document.CreateElement("wsse", "TerminalId", string.Empty);
            innerChild.InnerText = mashineName;
            dataChild.AppendChild(innerChild);

            innerChild = document.CreateElement("wsse", "Authenticated", string.Empty);
            innerChild.InnerText = isTokenAuthenticated.ToString();
            dataChild.AppendChild(innerChild);

            innerChild = document.CreateElement("wsse", "Source", string.Empty);
            innerChild.InnerText = string.Empty;
            dataChild.AppendChild(innerChild);

            tokenElement.AppendChild(dataChild);
            token.LoadXml(tokenElement);
        }
    }
}
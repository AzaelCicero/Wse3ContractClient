using System.Linq;
using System.Xml.Linq;

namespace Wse3ContractClient.XmlMinions
{
    public class XmlTools
    {
        public static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (xmlDocument.HasElements)
            {
                return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(RemoveAllNamespaces));
            }

            var xElement = new XElement(xmlDocument.Name.LocalName)
                           {
                               Value = xmlDocument.Value
                           };

            foreach (var attribute in xmlDocument.Attributes())
            {
                xElement.Add(attribute);
            }

            return xElement;
        }
    }
}
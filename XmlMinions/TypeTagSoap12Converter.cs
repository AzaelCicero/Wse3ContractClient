using System;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace Wse3ContractClient.XmlMinions
{
    internal class TypeTagSoap12Converter
    {
        private readonly MethodInfo _typeNameMethod;

        public TypeTagSoap12Converter()
        {
            var typeScopeType = typeof(XmlSerializer).Assembly.GetType("System.Xml.Serialization.TypeScope");

            _typeNameMethod = typeScopeType.GetMethod("TypeName", BindingFlags.NonPublic | BindingFlags.Static);
        }

        public string GenerateTag(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                case TypeCode.Int32:
                case TypeCode.Boolean:
                case TypeCode.Int16:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                case TypeCode.Char:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return type.Name.ToLower();
                default:
                    if (type == typeof(XmlQualifiedName))
                    {
                        return "QName";
                    }
                    if (type == typeof(byte[]))
                    {
                        return "base64Binary";
                    }
                    if (type == typeof(Guid))
                    {
                        return "guid";
                    }
                    return TypeTag(type);
            }
        }
        private string TypeTag(Type type)
        {
            return (string)_typeNameMethod.Invoke(null, new object[] { type });
        }

    }
}
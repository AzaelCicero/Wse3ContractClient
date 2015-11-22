using System;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace Wse3ContractClient.XmlMinions
{
    internal class TypeTagSoap12Converter
    {
        private readonly Type _typeScopeType;
        private readonly MethodInfo _typeNameMethod;

        public TypeTagSoap12Converter()
        {
            _typeScopeType = typeof(XmlSerializer).Assembly.GetType("System.Xml.Serialization.TypeScope");

            _typeNameMethod = _typeScopeType.GetMethod("TypeName", BindingFlags.NonPublic | BindingFlags.Static);
        }

        public string GenerateTag(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean: return "boolean";
                case TypeCode.Byte: return "byte";
                case TypeCode.Char: return "char";
                case TypeCode.DateTime: return "DateTime";
                case TypeCode.Decimal: return "decimal";
                case TypeCode.Double: return "double";
                case TypeCode.Empty: return "void";
                case TypeCode.Int16: return "short";
                case TypeCode.Int32: return "int";
                case TypeCode.Int64: return "long";
                case TypeCode.SByte: return "sbyte";
                case TypeCode.Single: return "float";
                case TypeCode.String: return "string";
                case TypeCode.UInt16: return "ushort";
                case TypeCode.UInt32: return "uint";
                case TypeCode.UInt64: return "ulong";
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
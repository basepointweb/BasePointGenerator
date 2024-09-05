using BasePointGenerator.Extensions;
using System.Collections.Generic;

namespace BasePointGenerator.Dtos
{
    public class PropertyInfo
    {

        private static readonly HashSet<string> PrimitiveTypeNames = new HashSet<string>
        {
            "Boolean", "Byte", "SByte", "Char",
            "Decimal", "Double", "Single",
            "Int32", "UInt32", "Int64", "UInt64",
            "Int16", "UInt16", "String", "string", "DateTime", "DateTimeOffset"
        };

        public string Type { get; }
        public string Name { get; }
        public bool GenerateGetMethodOnRepository { get; set; }
        public bool PreventDuplication { get; set; }
        public PropertyInfo(string type, string name)
        {
            Type = type;
            Name = name;
        }

        public bool IsPrimitive()
        {
            return PrimitiveTypeNames.Contains(Type);
        }

        public bool IsListProperty()
        {
            return Type.Contains("EntityList") || Type.Contains("List") || Type.Contains("Enumerable") || Type.Contains("Collection");
        }

        public string GetTypeConvertingToDtoWhenIsComplex(string prefixClassName = "", string sufixClassName = "")
        {
            var type = Type;

            type = type.Replace("IEntityList", "IList");
            type = type.Replace("EntityList", "List");

            var className = type;

            if (type.Contains("<") && type.Contains(">") && (!string.IsNullOrWhiteSpace(prefixClassName) || !string.IsNullOrWhiteSpace(sufixClassName)))
            {
                className = type.GetSubstringBetween("<", ">");
            }

            if (!this.IsPrimitive())
                type = type.Replace(className, prefixClassName + className + sufixClassName);

            return type;
        }
    }
}
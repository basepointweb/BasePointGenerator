using BasePointGenerator.Extensions;
using System.Collections.Generic;

namespace BasePointGenerator.Dtos
{
    public class PropertyInfo
    {

        private static readonly HashSet<string> PrimitiveTypeNames = new HashSet<string>
        {
            "BOOLEAN", "BYTE", "BYTE[]", "SBYTE", "CHAR",
            "DECIMAL","FLOAT", "DOUBLE", "SINGLE", "INT",
            "INT32", "UINT32", "INT64", "UINT64", "GUID",
            "INT16", "UINT16", "STRING", "DATETIME", "DATETIMEOFFSET", "TIMESPAN",
            "NULLABLE<BOOLEAN>", "NULLABLE<BYTE>", "NULLABLE<SBYTE>", "NULLABLE<CHAR>",
            "NULLABLE<DECIMAL>","NULLABLE<FLOAT>", "NULLABLE<DOUBLE>", "NULLABLE<SINGLE>", "NULLABLE<INT>",
            "NULLABLE<INT32>", "NULLABLE<UINT32>", "NULLABLE<INT64>", "NULLABLE<UINT64>","NULLABLE<GUID>",
            "NULLABLE<INT16>", "NULLABLE<UINT16>", "NULLABLE<STRING>", "NULLABLE<DATETIME>", "NULLABLE<DATETIMEOFFSET>","NULLABLE<TIMESPAN>",
        };

        public string Type { get; }
        public string UnderlyingType { get; }
        public string Name { get; }
        public bool GenerateGetMethodOnRepository { get; set; }
        public bool PreventDuplication { get; set; }
        public int PropertySize { get; set; }
        public int DecimalPlaces { get; set; }
        public int FieldWidth { get; set; }
        public bool IsSubClassOfBaseEntity { get; set; }
        public bool DeclaredInBaseEntity { get; set; }
        public bool IsReadOnly
        {
            get
            {
                var formattedType = Type.ToUpper().Replace("?", "");

                if (formattedType.ToUpper().Contains("NULLABLE"))
                    formattedType = formattedType.ToUpper().SubstringsBetween("NULLABLE<", ">")[0];

                return
                    !formattedType.Equals("Decimal", StringComparison.OrdinalIgnoreCase) &&
                    !formattedType.Equals("Float", StringComparison.OrdinalIgnoreCase) &&
                    !formattedType.Equals("String", StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool IsDecimalPlacesVisible { get; set; }

        public PropertyInfo(string type, string underlyingType, string name, bool isSubClassOfBaseEntity, bool declaredInBaseEntity)
        {
            Type = type;
            Name = name;
            UnderlyingType = underlyingType;
            IsSubClassOfBaseEntity = isSubClassOfBaseEntity;
            DeclaredInBaseEntity = declaredInBaseEntity;

            var formattedType = Type.ToUpper().Replace("?", "");

            if (formattedType.ToUpper().Contains("NULLABLE"))
                formattedType = formattedType.ToUpper().SubstringsBetween("NULLABLE<", ">")[0];

            IsDecimalPlacesVisible =
                formattedType.Equals("Decimal", StringComparison.OrdinalIgnoreCase) ||
                formattedType.Equals("Float", StringComparison.OrdinalIgnoreCase);

            FieldWidth = IsDecimalPlacesVisible ? 48 : 88;
        }

        public bool IsPrimitive()
        {
            var type = Type.ToUpper().Replace("?", "");

            return PrimitiveTypeNames.Contains(type);
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
                className = type.SubstringsBetween("<", ">")[0];
            }

            if (!this.IsPrimitive())
                type = type.Replace(className, prefixClassName + className + sufixClassName);

            return type;
        }
    }
}
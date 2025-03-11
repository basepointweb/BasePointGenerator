using BasePointGenerator.Extensions;

namespace BasePointGenerator.Services
{
    public class FakeDataFactory
    {
        public static string GetFakeValue(string type)
        {
            type = type.Replace("?", "");

            if (type.Contains("NULLABLE"))
                type = type.SubstringsBetween("NULLABLE<", ">")[0];

            switch (type.ToLower())
            {
                case "string":
                    return "\"Sample text\"";
                case "bool":
                case "boolean":
                    return "true";
                case "char":
                    return "'a'";
                case "byte":
                    return "123";
                case "sbyte":
                    return "123";
                case "short":
                    return "12345";
                case "ushort":
                    return "12345";
                case "int":
                    return "123";
                case "uint":
                    return "123u";
                case "long":
                    return "123456789L";
                case "ulong":
                    return "123456789UL";
                case "float":
                    return "123.45f";
                case "double":
                    return "123.45";
                case "decimal":
                    return "123.45m";
                case "datetime":
                    return "DateTime.Now";
                case "datetimeoffset":
                    return "DateTimeOffset.Now";
                default:
                    return "default";
            }
        }
    }
}

using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BasePointGenerator.Generators.InfrastructureLayer.Migrations
{
    public static class MigrationCreateTableFactory
    {
        public static string Create(
            string fileContent,
            string filePath,
            IList<PropertyInfo> classProperties,
            IList<MethodInfo> methods,
            FileContentGenerationOptions options)
        {
            Validate(fileContent);

            if (!classProperties.Any())
                throw new ValidationException("It wasn't identified public properties to generate builder class");

            var originalClassName = GetOriginalClassName(fileContent);

            return CreateTable(fileContent, originalClassName, classProperties, ref filePath);
        }

        private static string CreateTable(string fileContent, string originalClassName, IList<PropertyInfo> properties, ref string filePath)
        {
            var content = new StringBuilder();

            fileContent = fileContent.Substring(content.Length);

            content.AppendLine($"CREATE TABLE IF NOT EXISTS `{originalClassName.ToLower()}` (");

            if (!properties.Any(p => p.Name.Equals("Id")))
            {
                content.AppendLine($"  `Id` VARCHAR(36) NOT NULL,");
            }

            var propertiesToGenerateTableFields = properties.Where(x => !x.IsListProperty())
                .ToList();

            foreach (var property in propertiesToGenerateTableFields)
            {
                if (property.IsPrimitive())
                {
                    content.AppendLine($"  `{property.Name}` {GetMySqlPropertyType(property)},");
                }
                else
                    content.AppendLine($"  `{property.Type}Id` VARCHAR(36) NOT NULL,");
            }

            var preventDuplicationProperties = propertiesToGenerateTableFields.Where(x => x.PreventDuplication)
                .ToList();

            foreach (var property in preventDuplicationProperties)
            {
                content.AppendLine($"  UNIQUE KEY `{originalClassName}{property.Name}_UNIQUE` (`{property.Name}`),");
            }

            var nestedProperties = propertiesToGenerateTableFields.Where(x => !x.IsPrimitive())
               .ToList();

            int addedProperties = 0;

            foreach (var property in nestedProperties)
            {
                var separator = (addedProperties > 0) ? "," : "";

                content.AppendLine($"  CONSTRAINT `FK_{originalClassName}{property.Type}Id_{property.Type}` FOREIGN KEY (`{property.Type}Id`) REFERENCES `{property.Type}` (`Id`) ON UPDATE CASCADE{separator}");

                addedProperties++;
            }

            content.AppendLine($"  PRIMARY KEY (`Id`));");

            return content.ToString();
        }

        private static object GetMySqlPropertyType(PropertyInfo property)
        {
            var type = property.Type.ToUpper();

            var isNullable = (type.EndsWith("?") || type.Contains("NULLABLE"));

            if (type.Contains("NULLABLE"))
                type = type.SubstringsBetween("NULLABLE<", ">")[0];

            type = type.Replace("?", "");

            var mySqlType = type;

            switch (type)
            {
                case "INT":
                    mySqlType = $"TINYINT";
                    break;
                case "STRING":
                    mySqlType = $"VARCHAR({property.PropertySize})";
                    break;
                case "CHAR":
                    mySqlType = $"VARCHAR({1})";
                    break;
                case "DECIMAL":
                    mySqlType = $"DECIMAL({property.PropertySize},2)";
                    break;
                case "FLOAT":
                    mySqlType = $"FLOAT({property.PropertySize},2)";
                    break;
                case "BOOL":
                    mySqlType = $"BOOLEAN DEFAULT TRUE";
                    break;
                case "DATETIME":
                    mySqlType = $"DATETIME";
                    break;
                case "DATETIMEOFFSET":
                    mySqlType = $"DATETIME";
                    break;
                case "TIMESPAN":
                    mySqlType = $"TIMESTAMP";
                    break;
                case "BYTE[]":
                    mySqlType = $"BLOB";
                    break;
            }

            return mySqlType + (isNullable ? " NULL" : " NOT NULL");
        }

        private static void Validate(string fileContent)
        {
            if (fileContent.IndexOf("namespace ") < 0)
                throw new ValidationException("The file selected is not valid.");
        }

        private static string GetOriginalClassName(string fileContent)
        {
            var regex = Regex.Match(fileContent, @"\s+(class)\s+(?<Name>[^\s]+)");

            return regex.Groups["Name"].Value.Replace(":", "");
        }
    }
}
using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BasePointGenerator
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
                    content.AppendLine($"  `{property.Name}`{property.Type} NOT NULL,");
                else
                    content.AppendLine($"  `{property.Type}Id` VARCHAR(36) NOT NULL,");
            }

            var preventDuplicationProperties = propertiesToGenerateTableFields.Where(x => x.PreventDuplication)
                .ToList();

            foreach (var property in preventDuplicationProperties)
            {
                content.AppendLine($"  UNIQUE KEY `{originalClassName}{property.Name}_UNIQUE` (`{property.Type}`),");
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
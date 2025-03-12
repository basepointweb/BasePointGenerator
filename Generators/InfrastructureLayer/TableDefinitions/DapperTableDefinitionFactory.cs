using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BasePointGenerator.Generators.InfrastructureLayer.TableDefinitions
{
    public static class DapperTableDefinitionFactory
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

            return CreateRepositoryClass(fileContent, originalClassName, classProperties, filePath);
        }

        private static string CreateRepositoryClass(string fileContent, string originalClassName, IList<PropertyInfo> properties, string filePath)
        {
            var content = new StringBuilder();

            fileContent = fileContent.Substring(content.Length);

            content.AppendLine("using BasePoint.Core.Cqrs.Dapper.TableDefinitions;");
            content.AppendLine("using System.Data;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Domain.Entities;");
            content.AppendLine("");
            content.AppendLine(GetNameSpace(filePath));

            content.AppendLine("{");

            var newClassName = string.Concat(originalClassName, "TableDefinition");

            content.AppendLine(string.Concat("\tpublic class ", newClassName));

            content.AppendLine("\t{");

            GenerateDapperDefinitionStaticProperty(content, originalClassName, properties);

            content.AppendLine("\t}");

            content.AppendLine("}");

            return content.ToString();
        }

        private static void Validate(string fileContent)
        {
            if (fileContent.IndexOf("namespace ") < 0)
                throw new ValidationException("The file selected is not valid.");
        }

        private static void GenerateDapperDefinitionStaticProperty(StringBuilder content, string originalClassName, IList<PropertyInfo> properties)
        {
            content.AppendLine();
            content.AppendLine($"\t\tpublic static readonly DapperTableDefinition TableDefinition = new DapperTableDefinition");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\tTableName = \"{originalClassName}\",");
            content.AppendLine($"\t\t\tColumnDefinitions = new List<DapperTableColumnDefinition>()");
            content.AppendLine("\t\t\t{");

            content.AppendLine("\t\t\t\tnew DapperTableColumnDefinition");
            content.AppendLine("\t\t\t\t{");
            content.AppendLine($"\t\t\t\t\tDbFieldName = \"Id\",");
            content.AppendLine($"\t\t\t\t\tEntityFieldName = nameof({originalClassName}.Id),");
            content.AppendLine($"\t\t\t\t\tType = DbType.Guid");
            content.AppendLine("\t\t\t\t},");

            foreach (var item in properties)
            {
                content.AppendLine("\t\t\t\tnew DapperTableColumnDefinition");
                content.AppendLine("\t\t\t\t{");
                content.AppendLine($"\t\t\t\t\tDbFieldName = \"{item.Name}\",");
                content.AppendLine($"\t\t\t\t\tEntityFieldName = nameof({originalClassName}.{item.Name}),");
                content.Append($"\t\t\t\t\tType = DbType.{GetDbTypeName(item.Type)}");

                if (item.Type.ToUpper().Contains("STRING") || item.Type.ToUpper().Contains("CHAR"))
                {
                    content.Append($",\n");
                    content.AppendLine($"\t\t\t\t\tSize = {item.PropertySize}");
                }
                else
                {
                    content.Append($",\n");
                }

                content.AppendLine("\t\t\t\t},");
            }
            content.AppendLine("\t\t\t}");
            content.AppendLine("\t\t};");
        }

        private static object GetDbTypeName(string type)
        {
            type = type.Replace("?", "");

            if (type.Contains("NULLABLE"))
                type = type.SubstringsBetween("NULLABLE<", ">")[0];

            return type switch
            {
                "string" => "AnsiString",
                "char" => "AnsiString",
                "int" => "Int32",
                "int?" => "Int32",
                "DateTime" => "DateTime",
                "DateTime?" => "DateTime",
                "DateTimeOffset" => "DateTimeOffset",
                "DateTimeOffset?" => "DateTimeOffset",
                "TimeSpan" => "Time",
                "TimeSpan?" => "Time",
                "decimal" => "Decimal",
                "decimal?" => "Decimal",
                "bool" => "Boolean",
                "bool?" => "Boolean",
                "Guid" => "Guid",
                "Guid?" => "Guid",
                _ => "AnsiString",
            };
        }

        private static string GetNameSpace(string filePath)
        {
            var solution = VS.Solutions.GetCurrentSolutionAsync().Result;

            var solutionPath = Path.GetDirectoryName(solution.FullPath);

            var namespacePath = filePath.Replace(solutionPath, "").Replace("\\", ".");
            var solutionName = solution.Name.Replace(".sln", "");

            int count = Regex.Matches(namespacePath, Regex.Escape(solutionName)).Count;

            if (count > 1)
                namespacePath = namespacePath.ReplaceFirstOccurrence("." + solutionName, "");

            var caracteresDiference = 1;

            if (namespacePath.EndsWith("."))
                caracteresDiference += 1;

            namespacePath = namespacePath.Substring(1, namespacePath.Length - caracteresDiference);

            return "namespace " + namespacePath;
        }

        private static string GetNameRootProjectName()
        {
            var solution = VS.Solutions.GetCurrentSolutionAsync().Result;

            return solution.Name.Replace(".sln", "");
        }

        private static string GetUsings(string fileContent)
        {
            return fileContent.Substring(0, fileContent.IndexOf("namespace"));
        }

        private static string GetOriginalClassName(string fileContent)
        {
            var regex = Regex.Match(fileContent, @"\s+(class)\s+(?<Name>[^\s]+)");

            return regex.Groups["Name"].Value.Replace(":", "");
        }
    }
}
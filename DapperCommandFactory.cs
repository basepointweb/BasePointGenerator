using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BasePointGenerator
{
    public static class DapperCommandFactory
    {
        public static string Create(
            string fileContent,
            string filePath,
            IList<PropertyInfo> classProperties,
            IList<MethodInfo> methods,
            FileContentGenerationOptions options)
        {
            Validate(fileContent);

            var originalClassName = GetOriginalClassName(fileContent);

            return CreateRepositoryClass(fileContent, originalClassName, filePath);
        }

        private static string CreateRepositoryClass(string fileContent, string originalClassName, string filePath)
        {
            var content = new StringBuilder();

            fileContent = fileContent.Substring(content.Length);

            content.AppendLine("using Dapper;");
            content.AppendLine("using MySql.Data.MySqlClient;");
            content.AppendLine("using System.Data;");
            content.AppendLine("using BasePoint.Core.Cqrs.Dapper.EntityCommands;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Domain.Entities;");
            content.AppendLine($"using {GetNameRootProjectName()}.Cqrs.Dapper.TableDefinitions;");
            content.AppendLine("");

            content.AppendLine(GetNameSpace(filePath));

            content.AppendLine("{");

            var newClassName = string.Concat(originalClassName, "Command");

            content.AppendLine(string.Concat("\tpublic class ", newClassName, $" : DapperCommand<{originalClassName}>"));

            content.AppendLine("\t{");

            GenerateRepositoryConstructor(content, originalClassName, newClassName);

            GenerateCreateCommandDefinitionsMethod(content, originalClassName);

            content.AppendLine("\t}");

            content.AppendLine("}");

            return content.ToString();
        }

        private static void Validate(string fileContent)
        {
            if (fileContent.IndexOf("namespace ") < 0)
                throw new ValidationException("The file selected is not valid.");
        }

        private static void GenerateRepositoryConstructor(StringBuilder content, string originalClassName, string newClassName)
        {
            content.AppendLine($"\t\tpublic {newClassName}(IDbConnection connection, {originalClassName} affectedEntity) : base(connection, affectedEntity)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\tAddTypeMapping(nameof({originalClassName}), {originalClassName}TableDefinition.TableDefinition);");
            content.AppendLine("\t\t}");
            content.AppendLine();
        }

        private static void GenerateCreateCommandDefinitionsMethod(StringBuilder content, string className)
        {
            content.AppendLine($"\t\tpublic override IList<CommandDefinition> CreateCommandDefinitions({className} entity)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\t CreateCommandDefinitionByState(entity);");
            content.AppendLine($"");
            content.AppendLine($"\t\t\t return CommandDefinitions;");
            content.AppendLine("\t\t}");
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

            namespacePath = namespacePath.Substring(1, namespacePath.Length - 2);

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
using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BasePointGenerator
{
    public static class DapperQueryProviderFactory
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

            content.AppendLine("using BasePoint.Core.Cqrs.Dapper.QueryProviders;");
            content.AppendLine("using BasePoint.Core.Cqrs.Dapper.Extensions;");
            content.AppendLine("using BasePoint.Core.Shared;");
            content.AppendLine("using BasePoint.Core.Application.Cqrs;");
            content.AppendLine($"using BasePoint.Core.Application.Dtos.Input;");
            content.AppendLine("using Dapper;");
            content.AppendLine("using System.Data;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Application.Dtos.{originalClassName.ToPlural()};");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Application.Cqrs.QueryProviders.{originalClassName.ToPlural()};");

            content.AppendLine("");
            content.AppendLine(GetNameSpace(filePath));

            content.AppendLine("{");

            var newClassName = string.Concat(originalClassName, "CqrsQueryProvider");

            content.AppendLine(string.Concat("\tpublic class ", newClassName, $" : DapperCqrsQueryProvider<{originalClassName}Output>, I{originalClassName}CqrsQueryProvider"));

            content.AppendLine("\t{");

            GeneratePrivateVariables(content, originalClassName, properties);

            GenerateRepositoryConstructor(content, originalClassName, newClassName);

            GenerateMethodsToGetEntity(content, originalClassName, newClassName, properties);

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
            content.AppendLine($"\t\tpublic {newClassName}(IDbConnection connection) : base(connection)");
            content.AppendLine("\t\t{");
            content.AppendLine("\t\t}");
            content.AppendLine();
        }

        private static void GenerateMethodsToGetEntity(StringBuilder content, string originalClassName, string className, IList<PropertyInfo> properties)
        {
            content.AppendLine($"\t\tpublic override async Task<{originalClassName}Output> GetById(Guid id)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\tvar sqlCommand = SqlSelectCommand");
            content.AppendLine($"\t\t\t\t + Constants.StringEnter");
            content.AppendLine($"\t\t\t\t + \"Where\" + Constants.StringEnter");
            content.AppendLine($"\t\t\t\t + \"t.Id = @Id;\";");
            content.AppendLine();
            content.AppendLine($"\t\t\tvar parameters = new DynamicParameters();");
            content.AppendLine();
            content.AppendLine($"\t\t\tparameters.Add(\"@Id\", id);");
            content.AppendLine();
            content.AppendLine($"\t\t\treturn await _connection.QueryFirstOrDefaultAsync<{originalClassName}Output>(sqlCommand, parameters);");
            content.AppendLine("\t\t}");
        }

        private static void GeneratePrivateVariables(StringBuilder content, string originalClassName, IList<PropertyInfo> properties)
        {
            content.AppendLine($"\t\tprivate readonly string SqlSelectCommand = @\"SELECT");

            int propertyIndex = 0;

            var propertiesToGenerateSelectedFields = new List<PropertyInfo>();

            if (!properties.Any(x => x.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)))
                propertiesToGenerateSelectedFields.Add(new PropertyInfo("Guid", "Id"));

            propertiesToGenerateSelectedFields.AddRange(properties);

            foreach (var property in propertiesToGenerateSelectedFields)
            {
                var separator = (propertyIndex != propertiesToGenerateSelectedFields.Count - 1) && (propertiesToGenerateSelectedFields.Count > 1) ? "," : string.Empty;

                content.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t t.{property.Name}" + separator);

                propertyIndex++;
            }

            content.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t FROM {originalClassName} t\";");
            content.AppendLine();
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
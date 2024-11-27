using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;

namespace BasePointGenerator
{
    public static class DapperListItemOutputQueryProviderFactory
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
            content.AppendLine("using MySql.Data.MySqlClient;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Application.Dtos;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Application.Cqrs.QueryProviders;");

            content.AppendLine("");
            content.AppendLine(GetNameSpace(filePath));

            content.AppendLine("{");

            var newClassName = string.Concat(originalClassName, "ListItemOutputCqrsQueryProvider");

            content.AppendLine(string.Concat("\tpublic class ", newClassName, $" : DapperCqrsListItemOutputQueryProvider<{originalClassName}ListItemOutput>, I{originalClassName}ListItemOutputCqrsQueryProvider"));

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
            content.AppendLine();
            content.AppendLine($"\t\tpublic {newClassName}(IDbConnection connection) : base(connection)");
            content.AppendLine("\t\t{");
            content.AppendLine("\t\t}");
            content.AppendLine();
        }

        private static void GenerateMethodsToGetEntity(StringBuilder content, string originalClassName, string className, IList<PropertyInfo> properties)
        {
            var firstProperty = properties.FirstOrDefault(x => x.Name.ToUpper() != "ID") ??
                properties.First();

            content.AppendLine($"\t\tpublic override async Task<int> Count(IList<SearchFilterInput> filters)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\tvar sqlCommand = SqlCountSelectCommand");
            content.AppendLine($"\t\t\t\t + Constants.StringEnter;");
            content.AppendLine();
            content.AppendLine($"\t\t\tDictionary<string, object> parameters;");
            content.AppendLine($"\t\t\tstring sqlFilters;");
            content.AppendLine();
            content.AppendLine($"\t\t\tCreateParameters(filters, out parameters, out sqlFilters);");
            content.AppendLine();
            content.AppendLine($"\t\t\tsqlCommand += \" WHERE \" + sqlFilters;");
            content.AppendLine();
            content.AppendLine($"\t\t\tvar dapperParameters = new DynamicParameters(parameters);");
            content.AppendLine();
            content.AppendLine($"\t\t\treturn await _connection.ExecuteScalarAsync<int>(sqlCommand, dapperParameters);");
            content.AppendLine("\t\t}");
            content.AppendLine();
            content.AppendLine($"\t\tpublic override async Task<IList<{originalClassName}ListItemOutput>> GetPaginatedResults(IList<SearchFilterInput> filters, int pageNumber, int itemsPerPage)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\tvar sqlCommand = SqlSelectCommand");
            content.AppendLine($"\t\t\t\t + Constants.StringEnter;");
            content.AppendLine();
            content.AppendLine($"\t\t\tDictionary<string, object> parameters;");
            content.AppendLine($"\t\t\tstring sqlFilters;");
            content.AppendLine();
            content.AppendLine($"\t\t\tCreateParameters(filters, out parameters, out sqlFilters);");
            content.AppendLine();
            content.AppendLine($"\t\t\tsqlCommand += \" WHERE \" + sqlFilters +");
            content.AppendLine($"\t\t\t\t\t\" ORDER BY {firstProperty.Name.ToUpper()} LIMIT @PAGE_NUMBER, @ITENS_PER_PAGE \";");
            content.AppendLine();
            content.AppendLine($"\t\t\tparameters.Add(\"PAGE_NUMBER\", (pageNumber - 1) * itemsPerPage);");
            content.AppendLine($"\t\t\tparameters.Add(\"ITENS_PER_PAGE\", itemsPerPage);");
            content.AppendLine();
            content.AppendLine($"\t\t\tvar dapperParameters = new DynamicParameters(parameters);");
            content.AppendLine();
            content.AppendLine($"\t\t\treturn (await _connection.QueryAsync<{originalClassName}ListItemOutput>(sqlCommand, parameters)).ToList();");
            content.AppendLine("\t\t}");
            content.AppendLine();
            content.AppendLine($"\t\tprivate static void CreateParameters(IList<SearchFilterInput> filters, out Dictionary<string, object> parameters, out string sqlFilters)");
            content.AppendLine("\t\t{");
            content.AppendLine("\t\t\tparameters = new Dictionary<string, object>();");
            content.AppendLine();
            content.AppendLine("\t\t\tsqlFilters = string.Empty;");
            content.AppendLine("\t\t\tforeach (var filter in filters)");
            content.AppendLine("\t\t\t{");
            content.AppendLine("\t\t\t\tif (filter.FilterValue is not null)");
            content.AppendLine("\t\t\t\t{");
            content.AppendLine("\t\t\t\t\tif (!string.IsNullOrWhiteSpace(sqlFilters))");
            content.AppendLine("\t\t\t\t\t\tsqlFilters += \" AND \";");
            content.AppendLine();
            content.AppendLine("\t\t\t\t\tsqlFilters += filter.GetSqlFilter();");
            content.AppendLine();
            content.AppendLine("\t\t\t\t\tvar filterParameters = filter.GetParameters();");
            content.AppendLine();

            content.AppendLine("\t\t\t\t\tforeach (var param in filterParameters)");
            content.AppendLine("\t\t\t\t\t{");
            content.AppendLine("\t\t\t\t\t    parameters.Add(param.Key, param.Value);");
            content.AppendLine("\t\t\t\t\t}");
            content.AppendLine();

            content.AppendLine("\t\t\t\t}");
            content.AppendLine("\t\t\t}");
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
            content.AppendLine($"\t\tprivate readonly string SqlCountSelectCommand = @\"SELECT");
            content.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t Count(t.Id) as Count");
            content.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t FROM {originalClassName} t\";");
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

        private static string GetOriginalClassName(string fileContent)
        {
            var regex = Regex.Match(fileContent, @"\s+(class)\s+(?<Name>[^\s]+)");

            return regex.Groups["Name"].Value.Replace(":", "");
        }
    }
}
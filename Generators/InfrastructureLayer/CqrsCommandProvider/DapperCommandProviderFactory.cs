using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BasePointGenerator.Generators.InfrastructureLayer.CqrsCommandProvider
{
    public static class DapperCommandProviderFactory
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

            content.AppendLine("using BasePoint.Core.Cqrs.Dapper.CommandProviders;");
            content.AppendLine("using BasePoint.Core.Cqrs.Dapper.Extensions;");
            content.AppendLine("using BasePoint.Core.Shared;");
            content.AppendLine("using BasePoint.Core.Domain.Cqrs;");
            content.AppendLine("using Dapper;");
            content.AppendLine("using System.Data;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Domain.Entities;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Domain.Cqrs.CommandProviders.{originalClassName.ToPlural()};");
            content.AppendLine($"using {GetNameRootProjectName()}.Cqrs.Dapper.EntityCommands.{originalClassName.ToPlural()};");

            content.AppendLine("");
            content.AppendLine(GetNameSpace(filePath));

            content.AppendLine("{");

            var newClassName = string.Concat(originalClassName, "CqrsCommandProvider");

            content.AppendLine(string.Concat("\tpublic class ", newClassName, $" : DapperCqrsCommandProvider<{originalClassName}>, I{originalClassName}CqrsCommandProvider"));

            content.AppendLine("\t{");

            GeneratePrivateVariables(content, originalClassName, properties);

            GenerateRepositoryConstructor(content, originalClassName, newClassName);

            GenerateDapperCqrsCommandProviderMethodsImplementation(content, originalClassName);

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
            var propertiesToPreventDuplication = properties.Where(p => p.PreventDuplication && !p.IsListProperty()).ToList();

            if (propertiesToPreventDuplication.Count > 0)
                content.AppendLine();

            int generatedMthods = 0;

            foreach (var property in propertiesToPreventDuplication)
            {
                if (generatedMthods > 0)
                    content.AppendLine();

                content.AppendLine($"\t\tpublic async Task<{originalClassName}> Get{originalClassName}By{property.Name}({property.Type} {property.Name.GetWordWithFirstLetterDown()})");
                content.AppendLine("\t\t{");
                content.AppendLine($"\t\t\tvar sql = SqlSelectCommand");
                content.AppendLine($"\t\t\t\t + Constants.StringEnter");
                content.AppendLine($"\t\t\t\t + \"Where\" + Constants.StringEnter");
                content.AppendLine($"\t\t\t\t + \"t.{property.Name} = @{property.Name};\";");
                content.AppendLine();
                content.AppendLine($"\t\t\tvar parameters = new DynamicParameters();");
                content.AppendLine();
                content.AppendLine($"\t\t\tparameters.Add(\"@{property.Name}\", {property.Name.GetWordWithFirstLetterDown()});");
                content.AppendLine();
                content.AppendLine($"\t\t\treturn await _connection.QueryFirstOrDefaultAsync<{originalClassName}>(sql, parameters);");
                content.AppendLine("\t\t}");
                content.AppendLine();
                content.AppendLine($"\t\tpublic async Task<{originalClassName}> GetAnother{originalClassName}By{property.Name}({originalClassName} {originalClassName.GetWordWithFirstLetterDown()}, {property.Type} {property.Name.GetWordWithFirstLetterDown()})");
                content.AppendLine("\t\t{");
                content.AppendLine($"\t\t\tvar sql = SqlSelectCommand");
                content.AppendLine($"\t\t\t\t + Constants.StringEnter");
                content.AppendLine($"\t\t\t\t + \"Where\" + Constants.StringEnter");
                content.AppendLine($"\t\t\t\t + \"t.{property.Name} = @{property.Name} and t.Id <> @Id\";");
                content.AppendLine();
                content.AppendLine($"\t\t\tvar parameters = new DynamicParameters();");
                content.AppendLine();
                content.AppendLine($"\t\t\tparameters.Add(\"@{property.Name}\", {property.Name.GetWordWithFirstLetterDown()});");
                content.AppendLine($"\t\t\tparameters.Add(\"@Id\", {originalClassName.GetWordWithFirstLetterDown()}.Id);");
                content.AppendLine();
                content.AppendLine($"\t\t\treturn await _connection.QueryFirstOrDefaultAsync<{originalClassName}>(sql, parameters);");
                content.AppendLine("\t\t}");

                generatedMthods++;
            }

            var propertiesToCreateGetMethod = properties.Where(p => p.GenerateGetMethodOnRepository && !p.IsListProperty())
                .Except(propertiesToPreventDuplication)
                .ToList();

            if (propertiesToPreventDuplication.Count > 0)
                content.AppendLine();

            generatedMthods = 0;

            foreach (var property in propertiesToCreateGetMethod)
            {
                if (generatedMthods > 0)
                    content.AppendLine();

                content.AppendLine($"\t\tpublic async Task<{originalClassName}> Get{originalClassName}By{property.Name}({property.Type} {property.Name.GetWordWithFirstLetterDown()})");
                content.AppendLine("\t\t{");
                content.AppendLine($"\t\t\tvar sql = SqlSelectCommand");
                content.AppendLine($"\t\t\t\t + Constants.StringEnter");
                content.AppendLine($"\t\t\t\t + \"Where\" + Constants.StringEnter");
                content.AppendLine($"\t\t\t\t + \"t.{property.Name} = @{property.Name};\";");
                content.AppendLine();
                content.AppendLine($"\t\t\tvar parameters = new DynamicParameters();");
                content.AppendLine();
                content.AppendLine($"\t\t\tparameters.Add(\"@{property.Name}\", {property.Name.GetWordWithFirstLetterDown()});");
                content.AppendLine();
                content.AppendLine($"\t\t\treturn await _connection.QueryFirstOrDefaultAsync<{originalClassName}>(sql, parameters);");
                content.AppendLine("\t\t}");

                generatedMthods++;
            }
        }

        private static void GenerateDapperCqrsCommandProviderMethodsImplementation(StringBuilder content, string originalClassName)
        {
            content.AppendLine($"\t\tpublic override IEntityCommand GetAddCommand({originalClassName} entity)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\t return new {originalClassName}Command(_connection, entity);");
            content.AppendLine("\t\t}");
            content.AppendLine();

            content.AppendLine($"\t\tpublic override IEntityCommand GetDeleteCommand({originalClassName} entity)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\t return new {originalClassName}Command(_connection, entity);");
            content.AppendLine("\t\t}");
            content.AppendLine();

            content.AppendLine($"\t\tpublic override IEntityCommand GetUpdateCommand({originalClassName} entity)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\t return new {originalClassName}Command(_connection, entity);");
            content.AppendLine("\t\t}");
            content.AppendLine();

            content.AppendLine($"\t\tpublic override async Task<{originalClassName}> GetById(Guid id)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\tvar sql = SqlSelectCommand");
            content.AppendLine($"\t\t\t\t + Constants.StringEnter");
            content.AppendLine($"\t\t\t\t + \"Where\" + Constants.StringEnter");
            content.AppendLine($"\t\t\t\t + \"t.Id = @Id;\";");
            content.AppendLine();
            content.AppendLine($"\t\t\tvar parameters = new DynamicParameters();");
            content.AppendLine();
            content.AppendLine($"\t\t\tparameters.Add(\"@Id\", id);");
            content.AppendLine();
            content.AppendLine($"\t\t\treturn await _connection.QueryFirstOrDefaultAsync<{originalClassName}>(sql, parameters);");
            content.AppendLine("\t\t}");
        }

        private static void GeneratePrivateVariables(StringBuilder content, string originalClassName, IList<PropertyInfo> properties)
        {
            content.AppendLine($"\t\tprivate readonly string SqlSelectCommand = @\"SELECT");

            int propertyIndex = 0;

            var propertiesToGenerateSelectedFields = new List<PropertyInfo>
            {
                new PropertyInfo("Guid", "Id")
            };

            propertiesToGenerateSelectedFields.AddRange(properties);

            foreach (var property in propertiesToGenerateSelectedFields)
            {
                var separator = (propertyIndex != propertiesToGenerateSelectedFields.Count - 1) && (propertiesToGenerateSelectedFields.Count > 1) ? "," : string.Empty;

                content.AppendLine($"\t\t\t\t\t\t\t\t\t\t\t t.{property.Name}" + separator);

                propertyIndex++;
            }

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
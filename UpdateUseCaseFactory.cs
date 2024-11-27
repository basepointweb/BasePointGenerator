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
    public static class UpdateUseCaseFactory
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

            return CreateUseCaseClass(fileContent, originalClassName, classProperties, filePath);
        }

        private static string CreateUseCaseClass(string fileContent, string originalClassName, IList<PropertyInfo> properties, string filePath)
        {
            var content = new StringBuilder();

            fileContent = fileContent.Substring(content.Length);

            content.AppendLine("using FluentValidation;");
            content.AppendLine("using BasePoint.Core.Extensions;");
            content.AppendLine("using BasePoint.Core.UnitOfWork.Interfaces;");
            content.AppendLine("using BasePoint.Core.Application.UseCases;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Domain.Repositories.Interfaces;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Shared;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Application.Dtos;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Domain.Entities;");
            content.AppendLine("");
            content.AppendLine(GetNameSpace(filePath));

            content.AppendLine("{");

            var newClassName = string.Concat("Update", originalClassName, "UseCase");

            content.AppendLine(string.Concat("\tpublic class ", newClassName, $" : CommandUseCase<Update{originalClassName}Input, {originalClassName}Output>"));

            content.AppendLine("\t{");

            GeneratePrivateVariables(content, originalClassName);

            GenerateRepositoryConstructor(content, originalClassName, newClassName);

            GenerateInternalExecuteMethod(content, originalClassName, properties);

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
            content.AppendLine($"\t\tpublic {newClassName}(IValidator<Update{originalClassName}Input> validator, I{originalClassName}Repository {originalClassName.GetWordWithFirstLetterDown()}Repository, IUnitOfWork unitOfWork) : base(unitOfWork)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\t_validator = validator;");
            content.AppendLine($"\t\t\t_{originalClassName.GetWordWithFirstLetterDown()}Repository = {originalClassName.GetWordWithFirstLetterDown()}Repository;");
            content.AppendLine("\t\t}");
            content.AppendLine();
        }

        private static void GenerateInternalExecuteMethod(StringBuilder content, string className, IList<PropertyInfo> properties)
        {
            content.AppendLine($"\t\tpublic override async Task<UseCaseOutput<{className}Output>> InternalExecuteAsync(Update{className}Input input)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\t_validator.ValidateAndThrow(input);");
            content.AppendLine("");
            content.AppendLine($"\t\t\tvar previous{className} = _{className.GetWordWithFirstLetterDown()}Repository.GetById(input.Id.Value).Result");
            content.AppendLine($"\t\t\t\t.ThrowResourceNotFoundIfIsNull(SharedConstants.ErrorMessages.{className}WithIdDoesNotExists.Format(input.Id));");
            content.AppendLine("");

            var propertiesToPreventDuplication = properties.Where(p => p.PreventDuplication && !p.IsListProperty()).ToList();

            foreach (var property in propertiesToPreventDuplication)
            {
                content.AppendLine($"\t\t\t_{className.GetWordWithFirstLetterDown()}Repository.GetAnother{className}By{property.Name}(previous{className}, input.{property.Name}).Result");
                content.AppendLine($"\t\t\t\t.ThrowInvalidInputIfIsNotNull(SharedConstants.ErrorMessages.Another{className}With{property.Name}AlreadyExists.Format(input.{property.Name}));");
                content.AppendLine("");
            }

            foreach (var item in properties)
            {
                content.AppendLine(string.Concat($"\t\t\tprevious{className}.{item.Name} = ", $"input.{item.Name};"));
            }

            content.AppendLine("");

            content.AppendLine($"\t\t\t_{className.GetWordWithFirstLetterDown()}Repository.Persist(previous{className}, UnitOfWork);");
            content.AppendLine("");
            content.AppendLine($"\t\t\t await SaveChangesAsync();");
            content.AppendLine("");
            content.AppendLine($"\t\t\t return CreateSuccessOutput(new {className}Output(previous{className}));");
            content.AppendLine("\t\t}");
        }

        private static void GeneratePrivateVariables(StringBuilder content, string originalClassName)
        {
            content.AppendLine($"\t\tprivate readonly IValidator<Update{originalClassName}Input> _validator;");
            content.AppendLine($"\t\tprivate readonly I{originalClassName}Repository _{originalClassName.GetWordWithFirstLetterDown()}Repository;");
            content.AppendLine($"");
            content.AppendLine($"\t\tprotected override string SaveChangesErrorMessage => \"An error occurred while updating the {originalClassName.GetWordWithFirstLetterDown()}.\";");
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

        private static IList<PropertyInfo> GetPropertiesInfo(string fileContent)
        {
            var propertyes = new List<PropertyInfo>();

            foreach (Match item in Regex.Matches(fileContent, @"(?>public)\s+(?!class)((static|readonly)\s)?(?<Type>(\S+(?:<.+?>)?)(?=\s+\w+\s*\{\s*get))\s+(?<Name>[^\s]+)(?=\s*\{\s*get)"))
            {
                propertyes.Add(new PropertyInfo(item.Groups["Type"].Value, item.Groups["Name"].Value));
            }

            return propertyes;
        }
    }
}
using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BasePointGenerator.Generators.ApplicationLayer.UseCases
{
    public static class DeleteUseCaseFactory
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

            content.AppendLine("using BasePoint.Core.Extensions;");
            content.AppendLine("using BasePoint.Core.Application.UseCases;");
            content.AppendLine("using BasePoint.Core.UnitOfWork.Interfaces;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Domain.Repositories.Interfaces.{originalClassName.ToPlural()};");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Shared;");
            content.AppendLine("");
            content.AppendLine(GetNameSpace(filePath));

            content.AppendLine("{");

            var newClassName = string.Concat("Delete", originalClassName, "UseCase");

            content.AppendLine(string.Concat("\tpublic class ", newClassName, $" : CommandUseCase<Guid, bool>"));

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
            content.AppendLine($"\t\tpublic {newClassName}(I{originalClassName}Repository {originalClassName.GetWordWithFirstLetterDown()}Repository, IUnitOfWork unitOfWork) : base(unitOfWork)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\t_{originalClassName.GetWordWithFirstLetterDown()}Repository = {originalClassName.GetWordWithFirstLetterDown()}Repository;");
            content.AppendLine("\t\t}");
            content.AppendLine();
        }

        private static void GenerateInternalExecuteMethod(StringBuilder content, string className, IList<PropertyInfo> properties)
        {
            content.AppendLine($"\t\tpublic override async Task<UseCaseOutput<bool>> InternalExecuteAsync(Guid {className.GetWordWithFirstLetterDown()}Id)");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\tvar previous{className} = _{className.GetWordWithFirstLetterDown()}Repository.GetById({className.GetWordWithFirstLetterDown()}Id).Result");
            content.AppendLine($"\t\t\t\t.ThrowResourceNotFoundIfIsNull(SharedConstants.ErrorMessages.{className}WithIdDoesNotExists.Format({className.GetWordWithFirstLetterDown()}Id));");
            content.AppendLine("");
            content.AppendLine($"\t\t\tprevious{className}.SetStateAsDeleted();");
            content.AppendLine("");
            content.AppendLine($"\t\t\t_{className.GetWordWithFirstLetterDown()}Repository.Persist(previous{className}, UnitOfWork);");
            content.AppendLine("");
            content.AppendLine($"\t\t\t await SaveChangesAsync();");
            content.AppendLine("");
            content.AppendLine($"\t\t\t return CreateSuccessOutput(true);");
            content.AppendLine("\t\t}");
        }

        private static void GeneratePrivateVariables(StringBuilder content, string originalClassName)
        {
            content.AppendLine($"\t\tprivate readonly I{originalClassName}Repository _{originalClassName.GetWordWithFirstLetterDown()}Repository;");
            content.AppendLine($"");
            content.AppendLine($"\t\tprotected override string SaveChangesErrorMessage => \"An error occurred while deleting the {originalClassName.GetWordWithFirstLetterDown()}.\";");
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
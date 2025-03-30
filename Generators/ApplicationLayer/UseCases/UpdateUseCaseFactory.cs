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
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Domain.Repositories.Interfaces.{originalClassName.ToPlural()};");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Shared;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Application.Dtos.{originalClassName.ToPlural()};");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Domain.Entities;");

            var nestedProperties = properties.Where(p => p.IsSubClassOfBaseEntity);

            foreach (var property in nestedProperties)
            {
                var namespaceUsing = $"using {GetNameRootProjectName()}.Core.Domain.Repositories.Interfaces.{property.Type.ToPlural()};";

                if (!content.ToString().Contains(namespaceUsing))
                    content.AppendLine(namespaceUsing);
            }

            content.AppendLine("");
            content.AppendLine(GetNameSpace(filePath));

            content.AppendLine("{");

            var newClassName = string.Concat("Update", originalClassName, "UseCase");

            content.AppendLine(string.Concat("\tpublic class ", newClassName, $" : CommandUseCase<Update{originalClassName}Input, {originalClassName}Output>"));

            content.AppendLine("\t{");

            GeneratePrivateVariables(content, originalClassName, properties);

            GenerateRepositoryConstructor(content, originalClassName, newClassName, properties);

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

        private static void GenerateRepositoryConstructor(StringBuilder content, string originalClassName, string newClassName, IList<PropertyInfo> properties)
        {
            content.AppendLine();
            content.AppendLine($"\t\tpublic {newClassName}(");
            content.AppendLine($"\t\t\tIValidator<Update{originalClassName}Input> validator,");
            content.AppendLine($"\t\t\tI{originalClassName}Repository {originalClassName.GetWordWithFirstLetterDown()}Repository,");

            var nestedProperties = properties.Where(p => p.IsSubClassOfBaseEntity).ToList();

            foreach (var property in nestedProperties)
            {
                var repositoryVar = $"I{property.Type}Repository {property.Type.GetWordWithFirstLetterDown()}Repository,";

                if (!content.ToString().Contains(repositoryVar))
                    content.AppendLine($"\t\t\t{repositoryVar}");
            }

            content.AppendLine($"\t\t\tIUnitOfWork unitOfWork) : base(unitOfWork)");

            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\t_validator = validator;");
            content.AppendLine($"\t\t\t_{originalClassName.GetWordWithFirstLetterDown()}Repository = {originalClassName.GetWordWithFirstLetterDown()}Repository;");

            foreach (var property in nestedProperties)
            {
                var repositoryVar = $"_{property.Type.GetWordWithFirstLetterDown()}Repository = {property.Type.GetWordWithFirstLetterDown()}Repository";

                if (!content.ToString().Contains(repositoryVar))
                    content.AppendLine($"\t\t\t_{property.Type.GetWordWithFirstLetterDown()}Repository = {property.Type.GetWordWithFirstLetterDown()}Repository;");
            }

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

            var nestedProperties = properties.Where(p => p.IsSubClassOfBaseEntity).ToList();

            foreach (var property in nestedProperties)
            {
                content.AppendLine($"\t\t\tvar selected{property.Name} = _{property.Type.GetWordWithFirstLetterDown()}Repository.GetById(input.{property.Name}Id).Result");
                content.AppendLine($"\t\t\t\t.ThrowResourceNotFoundIfIsNull(SharedConstants.ErrorMessages.{property.Type}WithIdDoesNotExists.Format(input.{property.Name}Id));");
                content.AppendLine("");
            }

            foreach (var item in properties)
            {
                if (item.Name.Equals("Id") || item.Name.Equals("CreationDate"))
                    continue;

                if (item.IsSubClassOfBaseEntity)
                {
                    content.AppendLine(string.Concat($"\t\t\tprevious{className}.{item.Name} = ", $"selected{item.Name};"));
                }
                else
                {
                    if (item.IsListProperty())
                    {
                        content.AppendLine($"\t\t\t// Consider to create a method 'Remove{item.UnderlyingType}' in {className} class. You can use RemoveWhereNotIn from IEntityList or other ways to remove");
                        content.AppendLine($"\t\t\tprevious{className}.{item.Name.ToPlural()}.RemoveWhereNotIn(a => a.Property, input.{item.Name.ToPlural()}.Select(a => a.Property));");

                        content.AppendLine($"\t\t\t// Consider to create a method 'Add{item.UnderlyingType}' in {className} class and instanstiate items properly, creating a builder class if necessary");
                        content.AppendLine($"\t\t\tinput.{item.UnderlyingType.ToPlural()}.ForEach(x => previous{className}.Add{item.UnderlyingType}(new {item.UnderlyingType}()");
                        content.AppendLine("\t\t\t{");
                        content.AppendLine($"\t\t\t\t// Set {item.UnderlyingType} properties");
                        content.AppendLine("\t\t\t}));");
                    }
                    else
                    {
                        content.AppendLine(string.Concat($"\t\t\tprevious{className}.{item.Name} = ", $"input.{item.Name};"));
                    }
                }
            }

            content.AppendLine("");

            content.AppendLine($"\t\t\t_{className.GetWordWithFirstLetterDown()}Repository.Persist(previous{className}, UnitOfWork);");
            content.AppendLine("");
            content.AppendLine($"\t\t\t await SaveChangesAsync();");
            content.AppendLine("");
            content.AppendLine($"\t\t\t return CreateSuccessOutput(new {className}Output(previous{className}));");
            content.AppendLine("\t\t}");
        }

        private static void GeneratePrivateVariables(StringBuilder content, string originalClassName, IList<PropertyInfo> properties)
        {
            content.AppendLine($"\t\tprivate readonly IValidator<Update{originalClassName}Input> _validator;");

            content.AppendLine($"\t\tprivate readonly I{originalClassName}Repository _{originalClassName.GetWordWithFirstLetterDown()}Repository;");

            var nestedProperties = properties.Where(p => p.IsSubClassOfBaseEntity).ToList();

            foreach (var property in nestedProperties)
            {
                var repositoryClassName = $"I{property.Type}Repository";

                if (!content.ToString().Contains(repositoryClassName))
                    content.AppendLine($"\t\tprivate readonly {repositoryClassName} _{property.Type.GetWordWithFirstLetterDown()}Repository;");
            }

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

        private static string GetOriginalClassName(string fileContent)
        {
            var regex = Regex.Match(fileContent, @"\s+(class)\s+(?<Name>[^\s]+)");

            return regex.Groups["Name"].Value.Replace(":", "");
        }
    }
}
using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BasePointGenerator.Generators.InfrastructureLayer.Configurations
{
    public static class DapperServiceCollectionExtentionsFactory
    {
        public static string Create(
            string fileContent,
            string filePath,
            IList<PropertyInfo> classProperties,
            IList<MethodInfo> methods,
            FileContentGenerationOptions options)
        {
            if (!classProperties.Any())
                throw new ValidationException("It wasn't identified public properties to generate builder class");

            var originalClassName = GetOriginalClassName(fileContent);

            return CreateConstants(fileContent, originalClassName, classProperties, methods, filePath, options);
        }

        private static string CreateConstants(string fileContent, string originalClassName, IList<PropertyInfo> properties, IList<MethodInfo> methods, string filePath, FileContentGenerationOptions options)
        {
            var serviceCollectionFile = Path.Combine(filePath, "ServiceCollectionExtentions.cs");

            if (!File.Exists(serviceCollectionFile))
                return string.Empty;

            var serviceCollectionFileContent = File.ReadAllText(serviceCollectionFile);

            var commandProvidersDependencyMappings = new StringBuilder();
            var queryProvidersDependencyMappings = new StringBuilder();
            var repositoriesDependencyMappings = new StringBuilder();
            var usingDeclarations = new StringBuilder();

            var commandProviderMapping = $"service.AddSingleton<I{originalClassName}CqrsCommandProvider, {originalClassName}CqrsCommandProvider>();";

            if (!serviceCollectionFileContent.Contains(commandProviderMapping) && !commandProvidersDependencyMappings.ToString().Contains(commandProviderMapping))
                commandProvidersDependencyMappings.AppendLine($"\t\t\t{commandProviderMapping}");

            var queryProviderMapping = $"service.AddSingleton<I{originalClassName}CqrsQueryProvider, {originalClassName}CqrsQueryProvider>();";

            if (!serviceCollectionFileContent.Contains(queryProviderMapping) && !queryProvidersDependencyMappings.ToString().Contains(commandProviderMapping))
                queryProvidersDependencyMappings.AppendLine($"\t\t\t{queryProviderMapping}");

            queryProviderMapping = $"service.AddSingleton<I{originalClassName}ListItemOutputCqrsQueryProvider, {originalClassName}ListItemOutputCqrsQueryProvider>();";

            if (!serviceCollectionFileContent.Contains(queryProviderMapping) && !queryProvidersDependencyMappings.ToString().Contains(commandProviderMapping))
                queryProvidersDependencyMappings.AppendLine($"\t\t\t{queryProviderMapping}");

            queryProviderMapping = $"service.AddSingleton<IListItemOutputCqrsQueryProvider<{originalClassName}ListItemOutput>, {originalClassName}ListItemOutputCqrsQueryProvider>();";

            if (!serviceCollectionFileContent.Contains(queryProviderMapping) && !queryProvidersDependencyMappings.ToString().Contains(commandProviderMapping))
                queryProvidersDependencyMappings.AppendLine($"\t\t\t{queryProviderMapping}");

            var repositoryMapping = $"service.AddSingleton<I{originalClassName}Repository, {originalClassName}Repository>();";

            if (!serviceCollectionFileContent.Contains(repositoryMapping) && !repositoriesDependencyMappings.ToString().Contains(repositoryMapping))
                repositoriesDependencyMappings.AppendLine($"\t\t\t{repositoryMapping}");

            var mapCommandProvidersMethod = "public static void MapCommandProviders(this IServiceCollection service)";

            int insertIndex = serviceCollectionFileContent.IndexOf(mapCommandProvidersMethod) + mapCommandProvidersMethod.Length;
            insertIndex = serviceCollectionFileContent.IndexOf('{', insertIndex);
            var newFileContent = serviceCollectionFileContent;

            if ((insertIndex != -1) && commandProvidersDependencyMappings.Length > 0)
            {
                newFileContent = serviceCollectionFileContent.Insert(insertIndex + 1, "\n" + commandProvidersDependencyMappings.ToString());
            }

            var mapQueryProvidersMethod = "public static void MapQueryProviders(this IServiceCollection service)";

            insertIndex = newFileContent.IndexOf(mapQueryProvidersMethod) + mapQueryProvidersMethod.Length;
            insertIndex = newFileContent.IndexOf('{', insertIndex);

            if ((insertIndex != -1) && queryProvidersDependencyMappings.Length > 0)
            {
                newFileContent = newFileContent.Insert(insertIndex + 1, "\n" + queryProvidersDependencyMappings.ToString());
            }

            var mapRepositoriesMethod = "public static void MapRepositories(this IServiceCollection service)";

            insertIndex = newFileContent.IndexOf(mapRepositoriesMethod) + mapRepositoriesMethod.Length;
            insertIndex = newFileContent.IndexOf('{', insertIndex);

            if ((insertIndex != -1) && repositoriesDependencyMappings.Length > 0)
            {
                newFileContent = newFileContent.Insert(insertIndex + 1, "\n" + repositoriesDependencyMappings.ToString());
            }

            var commandProviderUsingDeclaration = $"using {GetNameRootProjectName()}.Core.Domain.Cqrs.CommandProviders.{originalClassName.ToPlural()};";

            if (!newFileContent.Contains(commandProviderUsingDeclaration) && !usingDeclarations.ToString().Contains(commandProviderUsingDeclaration))
                usingDeclarations.AppendLine($"{commandProviderUsingDeclaration}");

            var repositoriesUsingDeclaration = $"using {GetNameRootProjectName()}.Core.Domain.Repositories.{originalClassName.ToPlural()};";

            if (!newFileContent.Contains(repositoriesUsingDeclaration) && !usingDeclarations.ToString().Contains(repositoriesUsingDeclaration))
                usingDeclarations.AppendLine($"{repositoriesUsingDeclaration}");

            var repositoriesInterfacesUsingDeclaration = $"using {GetNameRootProjectName()}.Core.Domain.Repositories.Interfaces.{originalClassName.ToPlural()};";

            if (!newFileContent.Contains(repositoriesInterfacesUsingDeclaration) && !usingDeclarations.ToString().Contains(repositoriesInterfacesUsingDeclaration))
                usingDeclarations.AppendLine($"{repositoriesInterfacesUsingDeclaration}");

            var usingDapperCommandProviderDeclaration = $"using {GetNameRootProjectName()}.Cqrs.Dapper.CommandProviders.{originalClassName.ToPlural()};";

            if (!newFileContent.Contains(usingDapperCommandProviderDeclaration) && !usingDeclarations.ToString().Contains(usingDapperCommandProviderDeclaration))
                usingDeclarations.AppendLine($"{usingDapperCommandProviderDeclaration}");

            var usingDapperQueryProviderDeclaration = $"using BasePoint.Core.Application.Cqrs.QueryProviders;";

            if (!newFileContent.Contains(usingDapperQueryProviderDeclaration) && !usingDeclarations.ToString().Contains(usingDapperQueryProviderDeclaration))
                usingDeclarations.AppendLine($"{usingDapperQueryProviderDeclaration}");

            usingDapperQueryProviderDeclaration = $"using {GetNameRootProjectName()}.Core.Application.Cqrs.QueryProviders.{originalClassName.ToPlural()};";

            if (!newFileContent.Contains(usingDapperQueryProviderDeclaration) && !usingDeclarations.ToString().Contains(usingDapperQueryProviderDeclaration))
                usingDeclarations.AppendLine($"{usingDapperQueryProviderDeclaration}");

            usingDapperQueryProviderDeclaration = $"using {GetNameRootProjectName()}.Cqrs.Dapper.QueryProviders.{originalClassName.ToPlural()};";

            if (!newFileContent.Contains(usingDapperQueryProviderDeclaration) && !usingDeclarations.ToString().Contains(usingDapperQueryProviderDeclaration))
                usingDeclarations.AppendLine($"{usingDapperQueryProviderDeclaration}");

            var usingApplicationDtosDeclaration = $"using {GetNameRootProjectName()}.Core.Application.Dtos.{originalClassName.ToPlural()};";

            if (!newFileContent.Contains(usingApplicationDtosDeclaration) && !usingDeclarations.ToString().Contains(usingApplicationDtosDeclaration))
                usingDeclarations.AppendLine($"{usingApplicationDtosDeclaration}");

            var classNamespace = $"namespace {GetNameRootProjectName()}.Cqrs.Dapper.Configurations";

            insertIndex = newFileContent.IndexOf(classNamespace) - 1;

            if ((insertIndex != -1) && usingDeclarations.Length > 0)
            {
                newFileContent = newFileContent.Insert(insertIndex, "\n" + usingDeclarations.ToString());
            }

            return newFileContent;
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
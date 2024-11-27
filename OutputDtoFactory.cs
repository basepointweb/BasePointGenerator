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
    public static class OutputDtoFactory
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

            content.AppendLine($"using {GetNameRootProjectName()}.Core.Domain.Entities;");
            content.AppendLine("");

            fileContent = fileContent.Substring(content.Length);

            content.AppendLine(GetNameSpace(filePath));

            content.AppendLine("{");

            var newClassName = string.Concat(originalClassName, "Output");

            content.AppendLine(string.Concat("\tpublic record ", newClassName));

            content.AppendLine("\t{");

            GeneratePublicVariables(content, properties);

            GenerateConstructor(content, newClassName, originalClassName, properties);

            content.AppendLine("\t}");

            content.AppendLine("}");

            return content.ToString();
        }

        private static void GenerateConstructor(StringBuilder content, string newClassName, string originalClassName, IList<PropertyInfo> properties)
        {
            content.AppendLine($"\t\tprotected {newClassName}()");
            content.AppendLine("\t\t{");
            content.AppendLine("\t\t}");
            content.AppendLine("");

            content.AppendLine($"\t\tpublic {newClassName}({originalClassName} {originalClassName.GetWordWithFirstLetterDown()}) : this()");
            content.AppendLine("\t\t{");

            if (!properties.Any(p => p.Name.Equals("Id")))
            {
                content.AppendLine(string.Concat($"\t\t\tId = ", $"{originalClassName.GetWordWithFirstLetterDown()}.Id;"));
            }

            foreach (var item in properties)
            {
                content.AppendLine(string.Concat($"\t\t\t{item.Name} = ", $"{originalClassName.GetWordWithFirstLetterDown()}.{item.Name};"));
            }
            content.AppendLine("\t\t}");
        }

        private static void Validate(string fileContent)
        {
            if (fileContent.IndexOf("namespace ") < 0)
                throw new ValidationException("The file selected is not valid.");
        }

        private static void GeneratePublicVariables(StringBuilder content, IList<PropertyInfo> properties)
        {
            if (!properties.Any(p => p.Name.Equals("Id")))
            {
                content.AppendLine(string.Concat($"\t\tpublic Guid Id", " { get; init; }"));
            }

            foreach (var item in properties)
            {
                content.AppendLine(string.Concat($"\t\tpublic {item.GetTypeConvertingToDtoWhenIsComplex("", "Output")} {item.Name}", " { get; init; }"));
            }
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
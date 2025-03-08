using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BasePointGenerator.Generators.UnitTests.ApplicationLayer.Dtos
{
    public static class ListItemOutputBuilderFactory
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

            return CreateBuilderClass(fileContent, originalClassName, classProperties, filePath);
        }

        private static string CreateBuilderClass(string fileContent, string originalClassName, IList<PropertyInfo> properties, string filePath)
        {
            var content = new StringBuilder();

            fileContent = fileContent.Substring(content.Length);

            content.AppendLine($"using {GetNameRootProjectName()}.Core.Application.Dtos.{originalClassName.ToPlural()};");
            content.AppendLine("");
            content.AppendLine(GetNameSpace(filePath));

            content.AppendLine("{");

            var newClassName = string.Concat(originalClassName, "ListItemOutputBuilder");

            content.AppendLine(string.Concat("\tpublic class ", newClassName));

            content.AppendLine("\t{");

            GeneratePrivateVariables(content, properties);

            GenerateBuilderConstructor(content, newClassName);

            GenerateMethodsToSetValues(content, newClassName, properties);

            GenerateMethodBuild(content, originalClassName, properties);

            content.AppendLine("\t}");

            content.AppendLine("}");

            return content.ToString();
        }

        private static void Validate(string fileContent)
        {
            if (fileContent.IndexOf("namespace ") < 0)
                throw new ValidationException("The file selected is not valid.");
        }

        private static void GenerateMethodBuild(StringBuilder content, string originalClassName, IList<PropertyInfo> properties)
        {
            content.AppendLine($"\t\tpublic {originalClassName}ListItemOutput Build()");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\treturn new {originalClassName}ListItemOutput");
            content.AppendLine("\t\t\t{");

            var propertiesToAdd = new List<PropertyInfo>();
            propertiesToAdd.AddRange(properties);

            if (!propertiesToAdd.Any(p => p.Name.Equals("Id")))
            {
                propertiesToAdd.Add(new PropertyInfo("Guid", "Id"));
            }

            for (int i = 0; i < propertiesToAdd.Count; i++)
            {
                var setValue = $"\t\t\t\t{propertiesToAdd[i].Name} = _{propertiesToAdd[i].Name.GetWordWithFirstLetterDown()}";
                if (i + 1 != propertiesToAdd.Count)
                    setValue = string.Concat(setValue, ",");

                content.AppendLine(setValue);
            }

            content.AppendLine("\t\t\t};");
            content.AppendLine("\t\t}");
        }

        private static void GenerateBuilderConstructor(StringBuilder content, string newClassName)
        {
            content.AppendLine();
            content.AppendLine($"\t\tpublic {newClassName}()" + " { }");
            content.AppendLine();
        }

        private static void GenerateMethodsToSetValues(StringBuilder content, string className, IList<PropertyInfo> properties)
        {
            if (!properties.Any(p => p.Name.Equals("Id")))
            {
                content.AppendLine($"\t\tpublic {className} WithId(Guid id)");
                content.AppendLine("\t\t{");
                content.AppendLine($"\t\t\t_id = id;");
                content.AppendLine("\t\t\treturn this;");
                content.AppendLine("\t\t}");
                content.AppendLine();
            }

            foreach (var item in properties)
            {
                content.AppendLine($"\t\tpublic {className} With{item.Name}({item.GetTypeConvertingToDtoWhenIsComplex("", "ListItemOutput")} {item.Name.GetWordWithFirstLetterDown()})");
                content.AppendLine("\t\t{");
                content.AppendLine($"\t\t\t_{item.Name.GetWordWithFirstLetterDown()} = {item.Name.GetWordWithFirstLetterDown()};");
                content.AppendLine("\t\t\treturn this;");
                content.AppendLine("\t\t}");
                content.AppendLine();
            }
        }

        private static void GeneratePrivateVariables(StringBuilder content, IList<PropertyInfo> properties)
        {

            if (!properties.Any(p => p.Name.Equals("Id")))
            {
                content.AppendLine($"\t\tprivate Guid _id;");
            }

            foreach (var item in properties)
            {
                content.AppendLine($"\t\tprivate {item.GetTypeConvertingToDtoWhenIsComplex("", "ListItemOutput")} _{item.Name.GetWordWithFirstLetterDown()};");
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
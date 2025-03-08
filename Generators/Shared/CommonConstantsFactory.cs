using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BasePointGenerator.Generators.Shared
{
    public static class SharedConstantsFactory
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
            var constantsFile = Path.Combine(filePath, "SharedConstants.cs");

            if (!File.Exists(constantsFile))
                return string.Empty;

            var constantsFileContent = File.ReadAllText(constantsFile);

            var newErrorMessages = new StringBuilder();

            var constantName = $"{originalClassName}WithIdDoesNotExists";

            if (!constantsFileContent.Contains(constantName) && !newErrorMessages.ToString().Contains(constantName))
                newErrorMessages.AppendLine($"\t\t\tpublic static readonly string {constantName} = \"000;{originalClassName.GetWordWithFirstLetterDown()} with Id does not exists.\";");

            foreach (var property in properties)
            {
                if (property.PreventDuplication && !property.IsListProperty())
                {
                    constantName = $"A{originalClassName}With{property.Name}AlreadyExists";

                    if (!constantsFileContent.Contains(constantName) && !newErrorMessages.ToString().Contains(constantName))
                        newErrorMessages.AppendLine($"\t\t\tpublic static readonly string {constantName} = \"000;A {originalClassName.GetWordWithFirstLetterDown()} with {property.Name.GetWordWithFirstLetterDown()} already exists.\";");

                    constantName = $"Another{originalClassName}With{property.Name}AlreadyExists";

                    if (!constantsFileContent.Contains(constantName) && !newErrorMessages.ToString().Contains(constantName))
                        newErrorMessages.AppendLine($"\t\t\tpublic static readonly string {constantName} = \"000;Another {originalClassName.GetWordWithFirstLetterDown()} with {property.Name.GetWordWithFirstLetterDown()} already exists.\";");
                }

                if (options.GenerateCreateUseCase || options.GenerateUpdateUseCase)
                {
                    constantName = $"{originalClassName}{property.Name}IsInvalid";

                    if (!constantsFileContent.Contains(constantName) && !newErrorMessages.ToString().Contains(constantName))
                        newErrorMessages.AppendLine($"\t\t\tpublic static readonly string {constantName} = \"000;{originalClassName}{property.Name}IsInvalid.\";");
                }

                if (options.GenerateUpdateUseCase)
                {
                    constantName = $"{originalClassName}IdIsInvalid";

                    if (!constantsFileContent.Contains(constantName) && !newErrorMessages.ToString().Contains(constantName))
                        newErrorMessages.AppendLine($"\t\t\tpublic static readonly string {constantName} = \"000;{originalClassName}IdIsInvalid.\";");
                }
            }

            int insertIndex = constantsFileContent.IndexOf("ErrorMessages") + "ErrorMessages".Length;
            insertIndex = constantsFileContent.IndexOf('{', insertIndex);
            var newFileContent = constantsFileContent;

            if ((insertIndex != -1) && newErrorMessages.Length > 0)
            {
                newFileContent = constantsFileContent.Insert(insertIndex + 1, "\n" + newErrorMessages.ToString());
            }

            return newFileContent;
        }

        private static string GetOriginalClassName(string fileContent)
        {
            var regex = Regex.Match(fileContent, @"\s+(class)\s+(?<Name>[^\s]+)");

            return regex.Groups["Name"].Value.Replace(":", "");
        }
    }
}
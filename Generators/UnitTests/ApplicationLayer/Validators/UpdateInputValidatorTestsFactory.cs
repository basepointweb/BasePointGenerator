﻿using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;
using BasePointGenerator.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BasePointGenerator.Generators.UnitTests.ApplicationLayer.Validators
{
    public static class UpdateInputValidatorTestsFactory
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

            return CreateUseCaseTestsClass(fileContent, originalClassName, classProperties, filePath);
        }

        private static string CreateUseCaseTestsClass(string fileContent, string originalClassName, IList<PropertyInfo> properties, string filePath)
        {
            var content = new StringBuilder();

            fileContent = fileContent.Substring(content.Length);

            content.AppendLine("using BasePoint.Core.Shared;");
            content.AppendLine($"using BasePoint.Core.Extensions;");
            content.AppendLine("using FluentAssertions;");
            content.AppendLine("using Xunit;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Tests.Application.Dtos.Builders.{originalClassName.ToPlural()};");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Application.Dtos.Validators.{originalClassName.ToPlural()};");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Shared;");

            content.AppendLine("");
            content.AppendLine(GetNameSpace(filePath));

            content.AppendLine("{");

            var newClassName = string.Concat("Update", originalClassName, "InputValidatorTests");

            content.AppendLine(string.Concat("\tpublic class ", newClassName));

            content.AppendLine("\t{");

            GeneratePrivateVariables(content, originalClassName);

            GenerateTestConstructor(content, originalClassName, newClassName);

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

        private static void GenerateTestConstructor(StringBuilder content, string originalClassName, string newClassName)
        {
            content.AppendLine();
            content.AppendLine($"\t\tpublic {newClassName}()");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\t_validator = new Update{originalClassName}InputValidator();");
            content.AppendLine("\t\t}");
            content.AppendLine();
        }

        private static void GenerateInternalExecuteMethod(StringBuilder content, string className, IList<PropertyInfo> properties)
        {
            var firstProperty = properties.First();

            content.AppendLine("\t\t[Fact]");
            content.AppendLine($"\t\tpublic void Validate_InputIsValid_ReturnsIsValid()");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\tvar input = new Update{className}InputBuilder()");
            content.AppendLine($"\t\t\t\t.With{firstProperty.Name}({FakeDataFactory.GetFakeValue(firstProperty.Type)})");
            content.AppendLine($"\t\t\t\t.Build();");
            content.AppendLine("");
            content.AppendLine($"\t\t\tvar validationResult = _validator.Validate(input);");
            content.AppendLine($"\t\t\tvalidationResult.Errors.Should().ContainEquivalentOf(ValidationFailureBuilder.Build(SharedConstants.ErrorMessages.{className}{firstProperty.Name}IsInvalid));");
            content.AppendLine("");
            content.AppendLine("\t\t\tvalidationResult.IsValid.Should().BeTrue();");
            content.AppendLine("\t\t}");
            content.AppendLine();

            var propertiesToAdd = new List<PropertyInfo>();
            propertiesToAdd.AddRange(properties);

            if (!propertiesToAdd.Any(p => p.Name.Equals("Id")))
            {
                propertiesToAdd.Add(new PropertyInfo("Guid", "Id"));
            }

            int methodsAdded = 0;

            foreach (PropertyInfo property in propertiesToAdd)
            {
                var propertyType = property.Type.ToUpper().Replace("?", "");

                if (propertyType.Contains("NULLABLE"))
                    propertyType = propertyType.SubstringsBetween("NULLABLE<", ">")[0];

                if (methodsAdded > 0)
                    content.AppendLine();

                content.AppendLine("\t\t[Fact]");
                content.AppendLine($"\t\tpublic void Validate_Input{property.Name}IsInvalid_ReturnsIsInvalid()");
                content.AppendLine("\t\t{");
                content.AppendLine($"\t\t\tvar input = new Update{className}InputBuilder()");

                if (property.Type == "Guid")
                {
                    content.AppendLine($"\t\t\t\t.With{property.Name}(Guid.Empty)");
                }
                else
                {
                    content.AppendLine($"\t\t\t\t.With{property.Name}({FakeDataFactory.GetFakeValue(property.Type)})");
                }

                content.AppendLine($"\t\t\t\t.Build();");
                content.AppendLine("");
                content.AppendLine($"\t\t\tvar validationResult = _validator.Validate(input);");
                content.AppendLine($"\t\t\tvalidationResult.Errors.Should().ContainEquivalentOf(ValidationFailureBuilder.Build(SharedConstants.ErrorMessages.{className}{property.Name}IsInvalid));");
                content.AppendLine("");
                content.AppendLine("\t\t\tvalidationResult.IsValid.Should().BeFalse();");
                content.AppendLine("\t\t}");

                if (propertyType == "STRING" && property.PropertySize > 0)
                {
                    content.AppendLine();

                    content.AppendLine("\t\t[Fact]");
                    content.AppendLine($"\t\tpublic void Validate_Input{property.Name}SizeIsGreaterThanMaximumLength_ReturnsIsInvalid()");
                    content.AppendLine("\t\t{");
                    content.AppendLine($"\t\t\tvar input = new Update{className}InputBuilder()");
                    content.AppendLine($"\t\t\t\t.With{property.Name}(\"Give a text with a lot of characters that exceeds the size {property.PropertySize} in order to test the maximum length\")");
                    content.AppendLine($"\t\t\t\t.Build();");
                    content.AppendLine("");
                    content.AppendLine($"\t\t\tvar validationResult = _validator.Validate(input);");
                    content.AppendLine("");
                    content.AppendLine("\t\t\tvalidationResult.IsValid.Should().BeFalse();");
                    content.AppendLine($"\t\t\tvalidationResult.Errors.Should().ContainEquivalentOf(ValidationFailureBuilder.Build(SharedConstants.ErrorMessages.{className}{property.Name}MaximumLengthIs.Format(SharedConstants.Restrictions.{className}{property.Name}MaximumLength)));");
                    content.AppendLine("\t\t}");

                    content.AppendLine();

                    methodsAdded++;
                }

                methodsAdded++;
            }
        }

        private static void GeneratePrivateVariables(StringBuilder content, string originalClassName)
        {
            content.AppendLine($"\t\tprivate readonly Update{originalClassName}InputValidator _validator;");
            content.AppendLine($"");
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
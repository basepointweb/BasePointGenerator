﻿using BasePointGenerator.Dtos;
using BasePointGenerator.Exceptions;
using BasePointGenerator.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BasePointGenerator.Generators.UnitTests.ApplicationLayer.UseCases
{
    public static class DeleteUseCaseTestsFactory
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
            content.AppendLine("using BasePoint.Core.Extensions;");
            content.AppendLine("using BasePoint.Core.UnitOfWork.Interfaces;");
            content.AppendLine("using FluentAssertions;");
            content.AppendLine("using Moq;");
            content.AppendLine("using Xunit;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Application.UseCases.{originalClassName.ToPlural()};");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Domain.Repositories.Interfaces.{originalClassName.ToPlural()};");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Tests.Application.Dtos.Builders.{originalClassName.ToPlural()};");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Tests.Domain.Entities.Builders.{originalClassName.ToPlural()};");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Shared;");
            content.AppendLine($"using {GetNameRootProjectName()}.Core.Domain.Entities;");
            content.AppendLine("");
            content.AppendLine(GetNameSpace(filePath));

            content.AppendLine("{");

            var newClassName = string.Concat("Delete", originalClassName, "UseCaseTests");

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
            content.AppendLine($"\t\t\t_unitOfWork = new Mock<IUnitOfWork>();");
            content.AppendLine($"\t\t\t_{originalClassName.GetWordWithFirstLetterDown()}Repository = new Mock<I{originalClassName}Repository>();");
            content.AppendLine($"\t\t\t_useCase = new Delete{originalClassName}UseCase(_{originalClassName.GetWordWithFirstLetterDown()}Repository.Object, _unitOfWork.Object);");
            content.AppendLine("\t\t}");
            content.AppendLine();
        }

        private static void GenerateInternalExecuteMethod(StringBuilder content, string className, IList<PropertyInfo> properties)
        {
            content.AppendLine("\t\t[Fact]");
            content.AppendLine($"\t\tpublic async Task Execute_EverythingIsOk_ReturnsSuccess()");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\tvar id = Guid.NewGuid();");
            content.AppendLine("");
            content.AppendLine($"\t\t\tvar previous{className} = new {className}Builder()");
            content.AppendLine($"\t\t\t\t.Build();");
            content.AppendLine("");

            content.AppendLine($"\t\t\t_unitOfWork.Setup(x => x.SaveChangesAsync())");
            content.AppendLine($"\t\t\t\t.ReturnsAsync(true);");
            content.AppendLine("");

            content.AppendLine($"\t\t\t_{className.GetWordWithFirstLetterDown()}Repository.Setup(x => x.GetById(id))");
            content.AppendLine($"\t\t\t\t.ReturnsAsync(previous{className});");
            content.AppendLine("");

            content.AppendLine($"\t\t\tvar output = await _useCase.ExecuteAsync(id);");
            content.AppendLine("");
            content.AppendLine("\t\t\toutput.HasErros.Should().BeFalse();");
            content.AppendLine($"\t\t\t_{className.GetWordWithFirstLetterDown()}Repository.Verify(x => x.GetById(id), Times.Once);");
            content.AppendLine("\t\t}");
            content.AppendLine();
            content.AppendLine("\t\t[Fact]");
            content.AppendLine($"\t\tpublic async Task Execute_When{className}DoesNotExists_ReturnsError()");
            content.AppendLine("\t\t{");
            content.AppendLine($"\t\t\tvar id = Guid.NewGuid();");
            content.AppendLine("");
            content.AppendLine($"\t\t\t_{className.GetWordWithFirstLetterDown()}Repository.Setup(x => x.GetById(id))");
            content.AppendLine($"\t\t\t\t.ReturnsAsync(null as {className});");
            content.AppendLine("");
            content.AppendLine($"\t\t\tvar output = await _useCase.ExecuteAsync(id);");
            content.AppendLine("");
            content.AppendLine("\t\t\toutput.HasErros.Should().BeTrue();");
            content.AppendLine($"\t\t\toutput.Errors.Should().ContainEquivalentOf(new ErrorMessage(SharedConstants.ErrorMessages.{className}WithIdDoesNotExists.Format(id)));");
            content.AppendLine($"\t\t\t_{className.GetWordWithFirstLetterDown()}Repository.Verify(x => x.GetById(id), Times.Once);");
            content.AppendLine("\t\t}");
        }

        private static void GeneratePrivateVariables(StringBuilder content, string originalClassName)
        {
            content.AppendLine($"\t\tprivate readonly Delete{originalClassName}UseCase _useCase;");
            content.AppendLine($"\t\tprivate readonly Mock<IUnitOfWork> _unitOfWork;");
            content.AppendLine($"\t\tprivate readonly Mock<I{originalClassName}Repository> _{originalClassName.GetWordWithFirstLetterDown()}Repository;");
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
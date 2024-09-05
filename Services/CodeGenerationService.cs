using BasePointGenerator.Dtos;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BasePointGenerator.Services
{
    public delegate string FileContentGenerationFunction(
        string fileContent,
        string filePath,
        IList<PropertyInfo> properties,
        IList<MethodInfo> methods,
        FileContentGenerationOptions options);
    public class CodeGenerationService
    {
        private readonly SolutionItensDto SolutionItens;

        private readonly FilesPathGeneratorService _filesPathGeneratorService;
        public string OriginalFilePath { get; protected set; }
        public string OriginalFileContent { get; protected set; }
        public string FileName { get; protected set; }
        public IList<MethodInfo> Methods { get; protected set; }
        public IList<PropertyInfo> Properties { get; protected set; }
        public IList<string> GeneratedFiles { get; protected set; }

        public CodeGenerationService(Community.VisualStudio.Toolkit.Solution solution, string originalFileFullPath)
        {
            SolutionItens = new SolutionItensDto();

            SolutionItens.Solution = solution;
            OriginalFilePath = originalFileFullPath;

            InstantiateSolutionProjects();

            _filesPathGeneratorService = new FilesPathGeneratorService(SolutionItens, originalFileFullPath);

            OriginalFileContent = System.IO.File.ReadAllText(originalFileFullPath);

            OriginalFilePath = Path.GetDirectoryName(originalFileFullPath);
            FileName = Path.GetFileName(originalFileFullPath);

            Methods = GetMethodsInfo();
            Properties = GetPropertiesInfo();
            GeneratedFiles = [];
        }

        public IList<PropertyInfo> GetPropertiesInfo()
        {
            var propertyes = new List<PropertyInfo>();

            var matches = Regex.Matches(OriginalFileContent, @"(?>public|protected|internal|private)\s+(static\s+|readonly\s+|virtual\s+|override\s+)*?(?<Type>\S+(?:<.+?>)?)\s+(?<Name>[^\s]+)(?=\s*\{\s*get)");

            foreach (Match item in matches)
            {
                var type = item.Groups["Type"].Value;

                propertyes.Add(new PropertyInfo(item.Groups["Type"].Value, item.Groups["Name"].Value));
            }

            return propertyes;
        }

        private IList<MethodInfo> GetMethodsInfo()
        {
            var propertyes = new List<MethodInfo>();

            var matches = Regex.Matches(OriginalFileContent, @"\b(public|internal|static|sealed|virtual|override|async)*\s*(\w+<.*?>|\w+)\s+(\w+)\s*\(.*?\)");

            foreach (Match item in matches)
            {
                propertyes.Add(new MethodInfo(item.Groups[2].Value, item.Groups[3].Value));
            }

            return propertyes;
        }

        public void ReloadFileInformations()
        {
            OriginalFileContent = System.IO.File.ReadAllText(Path.Combine(OriginalFilePath, FileName));
            FileName = Path.GetFileName(FileName);

            Methods = GetMethodsInfo();
            Properties = GetPropertiesInfo();

            Methods = GetMethodsInfo();
            Properties = GetPropertiesInfo();
        }

        public void GenerateFiles(
            bool generateCreateUseCase,
            bool generateUpdateUseCase,
            bool generateDeleteUseCase,
            bool generateGetUseCase,
            bool onlyProcessFilePaths)
        {
            var options = new FileContentGenerationOptions()
            {
                GenerateCreateUseCase = generateCreateUseCase,
                GenerateUpdateUseCase = generateUpdateUseCase,
                GenerateDeleteUseCase = generateDeleteUseCase,
                GenerateGetUseCase = generateGetUseCase,
                OnlyProcessFilePaths = onlyProcessFilePaths
            };

            GeneratedFiles.Clear();

            var newFileName = string.Concat(FileName.Substring(0, FileName.Length - 3), "Builder.cs");
            Generatefile(newFileName, _filesPathGeneratorService.EntityBuilderPath, EntityBuilderFactory.Create, options);

            newFileName = string.Concat("I", FileName.Substring(0, FileName.Length - 3), "Repository.cs");
            Generatefile(newFileName, _filesPathGeneratorService.InterfaceRepositoryPath, InterfaceRepositoryFactory.Create, options);

            newFileName = string.Concat(FileName.Substring(0, FileName.Length - 3), "Repository.cs");
            Generatefile(newFileName, _filesPathGeneratorService.RepositoryPath, RepositoryFactory.Create, options);

            newFileName = string.Concat(FileName.Substring(0, FileName.Length - 3), "Command.cs");
            Generatefile(newFileName, _filesPathGeneratorService.DapperCommandPath, DapperCommandFactory.Create, options);

            newFileName = string.Concat("I", FileName.Substring(0, FileName.Length - 3), "CommandProvider.cs");
            Generatefile(newFileName, _filesPathGeneratorService.InterfaceCommandProviderPath, InterfaceCommandProviderFactory.Create, options);

            newFileName = string.Concat(FileName.Substring(0, FileName.Length - 3), "CqrsCommandProvider.cs");
            Generatefile(newFileName, _filesPathGeneratorService.DapperCommandProviderPath, DapperCommandProviderFactory.Create, options);

            newFileName = string.Concat("Dapper", FileName.Substring(0, FileName.Length - 3), "TableDefinition.cs");
            Generatefile(newFileName, _filesPathGeneratorService.DapperTableDefinitionPath, DapperTableDefinitionFactory.Create, options);

            newFileName = string.Concat(FileName.Substring(0, FileName.Length - 3), "Tests.cs");
            Generatefile(newFileName, _filesPathGeneratorService.EntityTestsPath, EntityTestsFactory.Create, options);

            newFileName = string.Concat(GetTimeStamp(), "CreateTable", FileName.Substring(0, FileName.Length - 3), ".sql");

            var partialScriptName = $"*{FileName.Substring(0, FileName.Length - 3)}*";

            string[] files = Directory.GetFiles(_filesPathGeneratorService.MigratonsPath, partialScriptName);

            if (files.Any())
                newFileName = files[0];

            Generatefile(newFileName, _filesPathGeneratorService.MigratonsPath, MigrationCreateTableFactory.Create, options);

            Generatefile("ServiceCollectionExtentions.cs", _filesPathGeneratorService.DapperServiceCollectionExtentionsPath, DapperServiceCollectionExtentionsFactory.Create, options);

            if (generateCreateUseCase)
                GenerateCreateUseCase(options);

            if (generateUpdateUseCase)
                GenerateUpdateUseCase(options);

            if (generateDeleteUseCase)
                GenerateDeleteUseCase(options);

            if (generateGetUseCase)
                GenerateGetUseCase(options);

            if (generateCreateUseCase || generateUpdateUseCase || generateDeleteUseCase || generateGetUseCase)
            {
                newFileName = string.Concat(FileName.Substring(0, FileName.Length - 3), "Output.cs");
                Generatefile(newFileName, _filesPathGeneratorService.OutputDtoPath, OutputDtoFactory.Create, options);

                newFileName = string.Concat(FileName.Substring(0, FileName.Length - 3), "OutputBuilder.cs");
                Generatefile(newFileName, _filesPathGeneratorService.OutputBuilderPath, OutputBuilderFactory.Create, options);

                newFileName = string.Concat(FileName.Substring(0, FileName.Length - 3), "Controller.cs");
                Generatefile(newFileName, _filesPathGeneratorService.ControllerPath, ControllerFactory.Create, options);
            }

            if (generateCreateUseCase || generateUpdateUseCase || generateGetUseCase)
            {
                Generatefile("Constants.cs", _filesPathGeneratorService.CommonConstantsPath, CommonConstantsFactory.Create, options);

                Generatefile("ServiceCollectionExtentions.cs", _filesPathGeneratorService.ApplicationServiceCollectionExtentionsPath, ApplicationServiceCollectionExtentionFactory.Create, options);
                Generatefile(PostmanCollectionFactory.GetCollectionFileName(), _filesPathGeneratorService.PostmanCollectionPath, PostmanCollectionFactory.Create, options);
            }
        }

        private void GenerateGetUseCase(FileContentGenerationOptions options)
        {
            var newFileName = string.Concat("Get", FileName.Substring(0, FileName.Length - 3), "ByIdUseCase.cs");
            Generatefile(newFileName, _filesPathGeneratorService.UseCasesPath, GetByIdUseCaseFactory.Create, options);

            newFileName = string.Concat("Get", FileName.Substring(0, FileName.Length - 3), "ByIdUseCaseTests.cs");
            Generatefile(newFileName, _filesPathGeneratorService.UseCasesTestsPath, GetByIdUseCaseFactoryTestsFactory.Create, options);

            newFileName = string.Concat("I", FileName.Substring(0, FileName.Length - 3), "CqrsQueryProvider.cs");
            Generatefile(newFileName, _filesPathGeneratorService.InterfaceQueryProviderPath, InterfaceQueryProviderFactory.Create, options);

            newFileName = string.Concat("I", FileName.Substring(0, FileName.Length - 3), "ListItemOutputCqrsQueryProvider.cs");
            Generatefile(newFileName, _filesPathGeneratorService.InterfaceListItemOutputQueryProviderPath, InterfaceListItemOutputQueryProviderFactory.Create, options);

            newFileName = string.Concat(FileName.Substring(0, FileName.Length - 3), "CqrsQueryProvider.cs");
            Generatefile(newFileName, _filesPathGeneratorService.DapperQueryProviderPath, DapperQueryProviderFactory.Create, options);

            newFileName = string.Concat(FileName.Substring(0, FileName.Length - 3), "ListItemOutputCqrsQueryProvider.cs");
            Generatefile(newFileName, _filesPathGeneratorService.DapperListItemOutputQueryProviderPath, DapperListItemOutputQueryProviderFactory.Create, options);

            newFileName = string.Concat(FileName.Substring(0, FileName.Length - 3), "ListItemOutputBuilder.cs");
            Generatefile(newFileName, _filesPathGeneratorService.OutputBuilderPath, ListItemOutputBuilderFactory.Create, options);

            newFileName = string.Concat(FileName.Substring(0, FileName.Length - 3), "ListItemOutput.cs");
            Generatefile(newFileName, _filesPathGeneratorService.OutputDtoPath, ListItemOutputFactory.Create, options);
        }

        private void GenerateDeleteUseCase(FileContentGenerationOptions options)
        {
            var newFileName = string.Concat("Delete", FileName.Substring(0, FileName.Length - 3), "UseCase.cs");
            Generatefile(newFileName, _filesPathGeneratorService.UseCasesPath, DeleteUseCaseFactory.Create, options);

            newFileName = string.Concat("Delete", FileName.Substring(0, FileName.Length - 3), "UseCaseTests.cs");
            Generatefile(newFileName, _filesPathGeneratorService.UseCasesTestsPath, DeleteUseCaseTestsFactory.Create, options);
        }

        private void GenerateUpdateUseCase(FileContentGenerationOptions options)
        {
            var newFileName = string.Concat("Update", FileName.Substring(0, FileName.Length - 3), "Input.cs");
            Generatefile(newFileName, _filesPathGeneratorService.InputDtoPath, UpdateInputDtoFactory.Create, options);

            newFileName = string.Concat("Update", FileName.Substring(0, FileName.Length - 3), "InputValidator.cs");
            Generatefile(newFileName, _filesPathGeneratorService.InputValidatorPath, UpdateInputValidatorFactory.Create, options);

            newFileName = string.Concat("Update", FileName.Substring(0, FileName.Length - 3), "InputValidatorTests.cs");
            Generatefile(newFileName, _filesPathGeneratorService.InputValidatorTestsPath, UpdateInputValidatorTestsFactory.Create, options);

            newFileName = string.Concat("Update", FileName.Substring(0, FileName.Length - 3), "InputBuilder.cs");
            Generatefile(newFileName, _filesPathGeneratorService.InputBuilderPath, UpdateInputBuilderFactory.Create, options);

            newFileName = string.Concat("Update", FileName.Substring(0, FileName.Length - 3), "UseCase.cs");
            Generatefile(newFileName, _filesPathGeneratorService.UseCasesPath, UpdateUseCaseFactory.Create, options);

            newFileName = string.Concat("Update", FileName.Substring(0, FileName.Length - 3), "UseCaseTests.cs");
            Generatefile(newFileName, _filesPathGeneratorService.UseCasesTestsPath, UpdateUseCaseTestsFactory.Create, options);
        }

        private void GenerateCreateUseCase(FileContentGenerationOptions options)
        {
            var newFileName = string.Concat("Create", FileName.Substring(0, FileName.Length - 3), "Input.cs");
            Generatefile(newFileName, _filesPathGeneratorService.InputDtoPath, CreateInputDtoFactory.Create, options);

            newFileName = string.Concat("Create", FileName.Substring(0, FileName.Length - 3), "InputValidator.cs");
            Generatefile(newFileName, _filesPathGeneratorService.InputValidatorPath, CreateInputValidatorFactory.Create, options);

            newFileName = string.Concat("Create", FileName.Substring(0, FileName.Length - 3), "InputBuilder.cs");
            Generatefile(newFileName, _filesPathGeneratorService.InputBuilderPath, CreateInputBuilderFactory.Create, options);

            newFileName = string.Concat("Create", FileName.Substring(0, FileName.Length - 3), "InputValidatorTests.cs");
            Generatefile(newFileName, _filesPathGeneratorService.InputValidatorTestsPath, CreateInputValidatorTestsFactory.Create, options);

            newFileName = string.Concat("Create", FileName.Substring(0, FileName.Length - 3), "UseCase.cs");
            Generatefile(newFileName, _filesPathGeneratorService.UseCasesPath, CreateUseCaseFactory.Create, options);

            newFileName = string.Concat("Create", FileName.Substring(0, FileName.Length - 3), "UseCaseTests.cs");
            Generatefile(newFileName, _filesPathGeneratorService.UseCasesTestsPath, CreateUseCaseTestsFactory.Create, options);
        }

        private string GetTimeStamp()
        {
            return DateTime.Now.ToString("yyyyMMddhhmmss");
        }

        protected void Generatefile(string newFileName, string filePath, FileContentGenerationFunction generationFunction, FileContentGenerationOptions options)
        {
            var generatedFile = Path.Combine(filePath, newFileName);
            var contentFile = "";

            if (!options.OnlyProcessFilePaths)
            {
                contentFile = generationFunction(OriginalFileContent, filePath, Properties, Methods, options);

                if (!string.IsNullOrWhiteSpace(contentFile))
                    System.IO.File.WriteAllText(generatedFile, contentFile);
            }

            if (!newFileName.Equals("Constants.cs") && !newFileName.Contains("postman_collection") && !newFileName.Contains("ServiceCollectionExtentions.cs"))
                GeneratedFiles.Add(generatedFile);
        }

        private void InstantiateSolutionProjects()
        {
            SolutionItens.CoreProject = SolutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core"))
                .FirstOrDefault();

            SolutionItens.CoreTestsProject = SolutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core.Tests"))
                .FirstOrDefault();

            SolutionItens.DapperProject = SolutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Cqrs.Dapper"))
                .FirstOrDefault();

            SolutionItens.AspNetPresentationProject = SolutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Presentation.AspNetCoreApi"))
                .FirstOrDefault();
        }
    }
}

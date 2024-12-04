using System.IO;
using System.Linq;
using BasePointGenerator.Dtos;

namespace BasePointGenerator.Services
{
    public class FilesPathGeneratorService
    {
        private readonly SolutionItensDto _solutionItens;
        private readonly string _originalFilePath;

        public string MigratonsPath { get; private set; }
        public string ControllerPath { get; private set; }
        public string EntityBuilderPath { get; private set; }
        public string DapperCommandProviderPath { get; private set; }
        public string OutputBuilderPath { get; private set; }
        public string InputBuilderPath { get; private set; }
        public string InputValidatorTestsPath { get; private set; }
        public string DapperTableDefinitionPath { get; private set; }
        public string InputValidatorPath { get; private set; }
        public string EntityTestsPath { get; private set; }
        public string UseCasesTestsPath { get; private set; }
        public string DapperCommandPath { get; private set; }
        public string OutputDtoPath { get; private set; }
        public string InputDtoPath { get; private set; }
        public string UseCasesPath { get; private set; }
        public string RepositoryPath { get; private set; }
        public string InterfaceCommandProviderPath { get; private set; }
        public string InterfaceRepositoryPath { get; private set; }
        public string DapperQueryProviderPath { get; private set; }
        public string DapperListItemOutputQueryProviderPath { get; private set; }
        public string InterfaceQueryProviderPath { get; private set; }
        public string InterfaceListItemOutputQueryProviderPath { get; private set; }
        public string SharedConstantsPath { get; private set; }
        public string PostmanCollectionPath { get; private set; }
        public string DapperServiceCollectionExtentionsPath { get; private set; }
        public string ApplicationServiceCollectionExtentionsPath { get; private set; }
        public string AppSettingsPath { get; private set; }

        public FilesPathGeneratorService(SolutionItensDto solutionItens, string originalFilePath)
        {
            _solutionItens = solutionItens;
            _originalFilePath = originalFilePath;

            InitializateDirectories();
        }


        private void InitializateDirectories()
        {
            MigratonsPath = GetMigratonsPath();
            ControllerPath = GetControllerPath();
            EntityBuilderPath = GetEntityBuilderPath();
            DapperCommandProviderPath = GetDapperCommandProviderPath();
            OutputBuilderPath = GetOutputBuilderPath();
            InputBuilderPath = GetInputBuilderPath();
            InputValidatorTestsPath = GetInputValidatorTestsPath();
            DapperTableDefinitionPath = GetDapperTableDefinitionPath();
            InputValidatorPath = GetInputValidatorPath();
            EntityTestsPath = GetEntityTestsPath();
            UseCasesTestsPath = GetUseCasesTestsPath();
            DapperCommandPath = GetDapperCommandPath();
            OutputDtoPath = GetOutputDtoPath();
            InputDtoPath = GetInputDtoPath();
            UseCasesPath = GetUseCasesPath();
            RepositoryPath = GetRepositoryPath();
            InterfaceCommandProviderPath = GetInterfaceCommandProviderPath();
            InterfaceRepositoryPath = GetInterfaceRepositoryPath();
            DapperQueryProviderPath = GetDapperQueryProviderPath();
            DapperListItemOutputQueryProviderPath = GetDapperListItemOutputQueryProviderPath();
            InterfaceQueryProviderPath = GetInterfaceQueryProviderPath();
            InterfaceListItemOutputQueryProviderPath = GetInterfaceListItemOutputQueryProviderPath();
            SharedConstantsPath = GetSharedConstantsPath();
            PostmanCollectionPath = GetPostmanCollectionPath();
            DapperServiceCollectionExtentionsPath = GetDapperServiceCollectionExtentionsPath();
            ApplicationServiceCollectionExtentionsPath = GetApplicationServiceCollectionExtentionsPath();
            AppSettingsPath = GetAppSettingsPath();
        }

        private string GetInterfaceListItemOutputQueryProviderPath()
        {
            return GetInterfaceQueryProviderPath();
        }

        private string GetDapperListItemOutputQueryProviderPath()
        {
            return GetDapperQueryProviderPath();
        }

        private string GetApplicationServiceCollectionExtentionsPath()
        {
            var coreProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core"))
                  .FirstOrDefault();

            var configurationsFolder = coreProject?.Children.Where(n => n.Text.Equals("Configurations"))
                .FirstOrDefault();

            return (configurationsFolder is not null) ? configurationsFolder.FullPath : _originalFilePath;
        }

        private string GetDapperServiceCollectionExtentionsPath()
        {
            var commandProviderProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Cqrs.Dapper"))
               .FirstOrDefault();

            var configurationsFolder = commandProviderProject?.Children.Where(n => n.Text.Equals("Configurations"))
                .FirstOrDefault();

            return (configurationsFolder is not null) ? configurationsFolder.FullPath : string.Empty;
        }

        private string GetPostmanCollectionPath()
        {
            var solutionPath = Path.GetDirectoryName(_solutionItens.Solution.FullPath);

            if (!solutionPath.EndsWith("\\"))
                solutionPath += "\\";

            return string.Concat(solutionPath, "Postman");
        }

        private string GetAppSettingsPath()
        {
            var presentationProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Presentation.AspNetCoreApi"))
               .FirstOrDefault();

            var appSettingsPath = Path.Combine(Path.GetDirectoryName(presentationProject?.FullPath), "appsettings.json");

            return (presentationProject is not null) ? appSettingsPath : string.Empty;
        }

        private string GetSharedConstantsPath()
        {
            var presentationProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core"))
                .FirstOrDefault();

            var sharedFolder = presentationProject?.Children.Where(n => n.Text.Equals("Shared"))
                .FirstOrDefault();

            return (sharedFolder is not null) ? sharedFolder.FullPath : string.Empty;
        }

        private string GetInterfaceQueryProviderPath()
        {
            var presentationProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core"))
                .FirstOrDefault();

            var applicationFolder = presentationProject?.Children.Where(n => n.Text.Equals("Application"))
                .FirstOrDefault();

            var cqrsFolder = applicationFolder?.Children.Where(n => n.Text.Equals("Cqrs"))
                .FirstOrDefault();

            var queryProvidersFolder = cqrsFolder?.Children.Where(n => n.Text.Equals("QueryProviders"))
                .FirstOrDefault();

            return (queryProvidersFolder is not null) ? queryProvidersFolder.FullPath : _originalFilePath;
        }

        private string GetDapperQueryProviderPath()
        {
            var presentationProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Cqrs.Dapper"))
             .FirstOrDefault();

            var queryProvidersFolder = presentationProject?.Children.Where(n => n.Text.Equals("QueryProviders"))
                .FirstOrDefault();

            return (queryProvidersFolder is not null) ? queryProvidersFolder.FullPath : _originalFilePath;
        }

        private string GetMigratonsPath()
        {
            var presentationProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Cqrs.Dapper"))
              .FirstOrDefault();

            var migrationsFolder = presentationProject?.Children.Where(n => n.Text.Equals("Migrations"))
                .FirstOrDefault();

            var sctructuralScriptsFolder = migrationsFolder?.Children.Where(n => n.Text.Equals("SctructuralScripts"))
                .FirstOrDefault();

            return (sctructuralScriptsFolder is not null) ? sctructuralScriptsFolder.FullPath : _originalFilePath;
        }

        private string GetControllerPath()
        {
            var presentationProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Presentation.AspNetCoreApi"))
               .FirstOrDefault();

            var controllersFolder = presentationProject?.Children.Where(n => n.Text.Equals("Controllers"))
                .FirstOrDefault();

            return (controllersFolder is not null) ? controllersFolder.FullPath : _originalFilePath;
        }

        private string GetEntityBuilderPath()
        {
            var coreProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core.Tests"))
                .FirstOrDefault();

            var domainFolder = coreProject?.Children.Where(n => n.Text.Equals("Domain"))
                .FirstOrDefault();

            var modelsFolder = domainFolder?.Children.Where(n => n.Text.Equals("Entities"))
                .FirstOrDefault();

            var buildersFolder = modelsFolder?.Children.Where(n => n.Text.Equals("Builders"))
               .FirstOrDefault();

            return (buildersFolder is not null) ? buildersFolder.FullPath : _originalFilePath;
        }

        private string GetDapperCommandProviderPath()
        {
            var commandProviderProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Cqrs.Dapper"))
               .FirstOrDefault();

            var commandProvidersFolder = commandProviderProject?.Children.Where(n => n.Text.Equals("CommandProviders"))
                .FirstOrDefault();

            return (commandProvidersFolder is not null) ? commandProvidersFolder.FullPath : _originalFilePath;
        }

        private string GetOutputBuilderPath()
        {
            var coreProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core.Tests"))
                .FirstOrDefault();

            var applicationFolder = coreProject?.Children.Where(n => n.Text.Equals("Application"))
                .FirstOrDefault();

            var dtosFolder = applicationFolder?.Children.Where(n => n.Text.Equals("Dtos"))
                .FirstOrDefault();

            var buildersFolder = dtosFolder?.Children.Where(n => n.Text.Equals("Builders"))
               .FirstOrDefault();

            return (buildersFolder is not null) ? buildersFolder.FullPath : _originalFilePath;
        }

        private string GetInputBuilderPath()
        {
            var coreProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core.Tests"))
                 .FirstOrDefault();

            var applicationFolder = coreProject?.Children.Where(n => n.Text.Equals("Application"))
                .FirstOrDefault();

            var dtosFolder = applicationFolder?.Children.Where(n => n.Text.Equals("Dtos"))
                .FirstOrDefault();

            var buildersFolder = dtosFolder?.Children.Where(n => n.Text.Equals("Builders"))
               .FirstOrDefault();

            return (buildersFolder is not null) ? buildersFolder.FullPath : _originalFilePath;
        }

        private string GetInputValidatorTestsPath()
        {
            var coreTestsProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core.Tests"))
              .FirstOrDefault();

            var applicationFolder = coreTestsProject?.Children.Where(n => n.Text.Equals("Application"))
                .FirstOrDefault();

            var dtosFolder = applicationFolder?.Children.Where(n => n.Text.Equals("Dtos"))
                .FirstOrDefault();

            var validatorsFolder = dtosFolder?.Children.Where(n => n.Text.Equals("Validators"))
               .FirstOrDefault();

            return (validatorsFolder is not null) ? validatorsFolder.FullPath : _originalFilePath;
        }

        private string GetDapperTableDefinitionPath()
        {
            var commandProviderProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Cqrs.Dapper"))
               .FirstOrDefault();

            var tableDefinitionsFolder = commandProviderProject?.Children.Where(n => n.Text.Equals("TableDefinitions"))
                .FirstOrDefault();

            return (tableDefinitionsFolder is not null) ? tableDefinitionsFolder.FullPath : _originalFilePath;
        }

        private string GetInputValidatorPath()
        {
            var coreProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core"))
                 .FirstOrDefault();

            var applicationFolder = coreProject?.Children.Where(n => n.Text.Equals("Application"))
                .FirstOrDefault();

            var dtosFolder = applicationFolder?.Children.Where(n => n.Text.Equals("Dtos"))
                .FirstOrDefault();

            var validatorsFolder = dtosFolder?.Children.Where(n => n.Text.Equals("Validators"))
               .FirstOrDefault();

            return (validatorsFolder is not null) ? validatorsFolder.FullPath : _originalFilePath;
        }

        private string GetEntityTestsPath()
        {
            var coreTestsProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core.Tests"))
               .FirstOrDefault();

            var domainFolder = coreTestsProject?.Children.Where(n => n.Text.Equals("Domain"))
                .FirstOrDefault();

            var modelsFolder = domainFolder?.Children.Where(n => n.Text.Equals("Entities"))
                .FirstOrDefault();

            return (modelsFolder is not null) ? modelsFolder.FullPath : _originalFilePath;
        }

        private string GetUseCasesTestsPath()
        {
            var coreTestsProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core.Tests"))
                .FirstOrDefault();

            var applicationFolder = coreTestsProject?.Children.Where(n => n.Text.Equals("Application"))
                .FirstOrDefault();

            var useCasesFolder = applicationFolder?.Children.Where(n => n.Text.Equals("UseCases"))
                .FirstOrDefault();

            return (useCasesFolder is not null) ? useCasesFolder.FullPath : _originalFilePath;
        }

        private string GetDapperCommandPath()
        {
            var commandProviderProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Cqrs.Dapper"))
               .FirstOrDefault();

            var entityCommandsFolder = commandProviderProject?.Children.Where(n => n.Text.Equals("EntityCommands"))
                .FirstOrDefault();

            return (entityCommandsFolder is not null) ? entityCommandsFolder.FullPath : _originalFilePath;
        }

        private string GetOutputDtoPath()
        {
            return GetInputDtoPath();
        }

        private string GetInputDtoPath()
        {
            var coreProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core"))
                  .FirstOrDefault();

            var applicationFolder = coreProject?.Children.Where(n => n.Text.Equals("Application"))
                .FirstOrDefault();

            var dtosFolder = applicationFolder?.Children.Where(n => n.Text.Equals("Dtos"))
                .FirstOrDefault();

            return (dtosFolder is not null) ? dtosFolder.FullPath : _originalFilePath;
        }

        private string GetUseCasesPath()
        {
            var coreProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core"))
                  .FirstOrDefault();

            var applicationFolder = coreProject?.Children.Where(n => n.Text.Equals("Application"))
                .FirstOrDefault();

            var useCasesFolder = applicationFolder?.Children.Where(n => n.Text.Equals("UseCases"))
                .FirstOrDefault();

            return (useCasesFolder is not null) ? useCasesFolder.FullPath : _originalFilePath;
        }

        private string GetRepositoryPath()
        {
            var coreProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core"))
                 .FirstOrDefault();

            var domainFolder = coreProject?.Children.Where(n => n.Text.Equals("Domain"))
                .FirstOrDefault();

            var repositoryFolder = domainFolder?.Children.Where(n => n.Text.Equals("Repositories"))
                .FirstOrDefault();

            return (repositoryFolder is not null) ? repositoryFolder.FullPath : _originalFilePath;
        }

        private string GetInterfaceCommandProviderPath()
        {
            var coreProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core"))
               .FirstOrDefault();

            var domainFolder = coreProject?.Children.Where(n => n.Text.Equals("Domain"))
                .FirstOrDefault();

            var cqrsFolder = domainFolder?.Children.Where(n => n.Text.Equals("Cqrs"))
                .FirstOrDefault();

            var commandProvidersFolder = cqrsFolder?.Children.Where(n => n.Text.Equals("CommandProviders"))
                .FirstOrDefault();

            return (commandProvidersFolder is not null) ? commandProvidersFolder.FullPath : _originalFilePath;
        }

        private string GetInterfaceRepositoryPath()
        {
            var coreProject = _solutionItens.Solution.Children.ToList().Where(c => c.Name.EndsWith(".Core"))
                .FirstOrDefault();

            var domainFolder = coreProject?.Children.Where(n => n.Text.Equals("Domain"))
                .FirstOrDefault();

            var repositoryFolder = domainFolder?.Children.Where(n => n.Text.Equals("Repositories"))
                .FirstOrDefault();

            var repositoryInterfaceFolder = repositoryFolder?.Children.Where(n => n.Text.Equals("Interfaces"))
                .FirstOrDefault();

            return (repositoryInterfaceFolder is not null) ? repositoryInterfaceFolder.FullPath : _originalFilePath;
        }
    }
}

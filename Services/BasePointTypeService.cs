using EnvDTE80;
using SharedProject;
using System.IO;
using System.Linq;

namespace BasePointGenerator.Services
{
    public class BasePointTypeService
    {
        private readonly DTE2 _dte;
        private readonly string _rootNamespaceName;
        private string _selectedFileName;

        public BasePointTypeService(DTE2 dte)
        {
            _dte = dte;
            _rootNamespaceName = GetNameRootProjectName();
        }

        private static string GetNameRootProjectName()
        {
            var solution = VS.Solutions.GetCurrentSolutionAsync().Result;

            return solution.Name.Replace(".sln", "");
        }

        public BasePointType GetBasePointType(string selectedFileName = null)
        {
            if (!string.IsNullOrWhiteSpace(selectedFileName))
                _selectedFileName = selectedFileName;

            if (string.IsNullOrWhiteSpace(_selectedFileName))
                return null;

            var aspNetPresentationProject = _dte.Solution.Projects
       .Cast<EnvDTE.Project>()
       .FirstOrDefault(p => p.Name.EndsWith(".Presentation.AspNetCoreApi"));

            string projectDir = aspNetPresentationProject.Properties.Item("FullPath").Value.ToString();

            string assemblyName = aspNetPresentationProject.Properties.Item("OutputFileName").Value.ToString();

            string outputPath = aspNetPresentationProject.ConfigurationManager
                .ActiveConfiguration
                .Properties
                .Item("OutputPath")
                .Value.ToString();

            string fullAssemblyPath = System.IO.Path.Combine(projectDir, outputPath, $"{_rootNamespaceName}.Core.dll");

            var fullyQualifiedTypeName = $"{_rootNamespaceName}.Core.Domain.Entities.{Path.GetFileName(_selectedFileName).Replace(".cs", "")}";

            return BasePointTypeHelper.GetBasePointTypeFromAssembly(fullAssemblyPath, fullyQualifiedTypeName);
        }
    }
}
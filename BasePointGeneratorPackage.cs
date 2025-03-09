global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;


namespace BasePointGenerator
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.BasePointGeneratorString)]
    [ProvideToolWindow(typeof(frmCodeGenerationOptions))]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class BasePointGeneratorPackage : ToolkitPackage
    {
        protected uint solutionEventsCookie;
        protected IVsSolution solutionService;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();
            await frmCodeGenerationOptionsCommand.InitializeAsync(this);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            solutionService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            if (solutionService != null)
            {
                var solution = VS.Solutions.GetCurrentSolutionAsync().Result;

                if (solution is not null && IsNewSolution(solution.FullPath))
                    await SetStartupProjectAsync(solution);
            }
        }

        private bool IsNewSolution(string solutionFile)
        {
            if (!string.IsNullOrEmpty(solutionFile) && System.IO.File.Exists(solutionFile))
            {
                var creationTime = System.IO.File.GetCreationTime(solutionFile);
                return (DateTime.Now - creationTime).TotalMinutes < 1;
            }
            return false;
        }

        public async Task SetStartupProjectAsync(Community.VisualStudio.Toolkit.Solution solution)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var aspNetPresentationProject = solution.Children.ToList().Where(c => c.Name.EndsWith(".Presentation.AspNetCoreApi"))
               .FirstOrDefault();

            DTE2 dte = await ServiceProvider.GetGlobalServiceAsync(typeof(DTE)) as DTE2;
            if (dte == null)
            {
                throw new InvalidOperationException("Não foi possível obter o serviço DTE.");
            }

            dte.Solution.Properties.Item("StartupProject").Value = aspNetPresentationProject.Name;
        }
    }
}
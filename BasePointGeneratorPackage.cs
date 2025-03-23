global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
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

            ExtractTypeAnalizerTool();

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

        private static void ExtractTypeAnalizerTool()
        {
            string userAppDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string extractPath = Path.Combine(userAppDataLocal, "BasePointGenerator");
            string zipFilePath = Path.Combine(extractPath, "BasePointGeneratorAssemblyAnalizer.zip");

            try
            {

                Directory.CreateDirectory(extractPath);

                string embeddedZipPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ZippedTools\\BasePointGeneratorAssemblyAnalizer.zip");

                if (File.Exists(embeddedZipPath))
                {
                    File.Copy(embeddedZipPath, zipFilePath, true);

                    using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string fullPath = Path.Combine(extractPath, entry.FullName);

                            if (entry.FullName.EndsWith("/"))
                            {
                                Directory.CreateDirectory(fullPath);
                            }
                            else
                            {

                                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                                if (File.Exists(fullPath))
                                {
                                    File.Delete(fullPath);
                                }

                                entry.ExtractToFile(fullPath);
                            }
                        }
                    }

                    Console.WriteLine("Arquivo ZIP extraído e descompactado com sucesso!");
                }
                else
                {
                    Console.WriteLine("Arquivo ZIP não encontrado no VSIX.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao descompactar o arquivo: {ex.Message}");
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
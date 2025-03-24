using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System.Linq;

namespace BasePointGenerator
{
    [Command(PackageIds.FileEditorContextMenuCommand)]
    internal sealed class FileEditorContextMenuCommand : BaseCommand<FileEditorContextMenuCommand>
    {
        private DTE2 _dte;

        public FileEditorContextMenuCommand()
        {
            _dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;
        }

        public string GetSelectedFileName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Document activeDocument = _dte.ActiveDocument;

            if (activeDocument != null)
            {
                var fullPath = activeDocument.FullName;

                return fullPath;
            }

            return null;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var solution = VS.Solutions.GetCurrentSolutionAsync().Result;

            var window = this.Package.FindToolWindow(typeof(frmCodeGenerationOptions), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Não foi possível criar a janela de ferramentas.");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;

            var frm = ((frmCodeGenerationOptionsControl)window.Content);

            frm.BasePointTypeService = new Services.BasePointTypeService(_dte);

            var type = frm.BasePointTypeService.GetBasePointType(GetSelectedFileName());

            if (type is null)
            {
                await VS.MessageBox.ShowWarningAsync("BasePoint code generator", $"Must successfull build the project with type and generate dll for the assembly");
                return;
            }

            frm.CodeGenerationService = new Services.CodeGenerationService(solution, type, GetSelectedFileName());

            frm.ClassProperties = frm.CodeGenerationService.Properties;

            if (frm.ClassProperties.Any(p => p.IsSubClassOfBaseEntity))
            {
                await VS.MessageBox.ShowWarningAsync("BasePoint code generator", $"If the code for BaseEntity-derived property types hasn't been generated yet," +
                    $" it's recommended to generate it first, as {frm.CodeGenerationService.ClassName} generated code will depend on these classes, including repositories.");
            }

            frm.GRD_Properties.ItemsSource = frm.ClassProperties;
            frm.BTN_Generate.IsEnabled = true;
            frm.BTN_Reload.IsEnabled = true;
            frm.PNL_InstructionsToLoadClass.Visibility = System.Windows.Visibility.Hidden;
            frm.PNL_GenerateClasses.Visibility = System.Windows.Visibility.Visible;
            frm.LBL_ClassProperties.Text = "Properties from " + frm.CodeGenerationService.ClassName;

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
        private ProjectItem FindProjectItemRecursive(ProjectItems items, string filePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (items == null) return null;

            foreach (ProjectItem item in items)
            {
                if (item.FileCount > 0)
                {
                    string projectFilePath = item.FileNames[0];
                    if (string.Equals(filePath, projectFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return item; // Encontrou o arquivo!
                    }
                }

                // Se o item contém sub-itens, busca recursivamente
                ProjectItem foundItem = FindProjectItemRecursive(item.ProjectItems, filePath);
                if (foundItem != null) return foundItem;
            }

            return null;
        }
    }
}

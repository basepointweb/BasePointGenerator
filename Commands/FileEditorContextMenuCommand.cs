using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

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

            frm.CodeGenerationService = new Services.CodeGenerationService(solution, GetSelectedFileName());

            if (!frm.CodeGenerationService.OriginalFileContent.Contains("BaseEntity"))
            {
                await VS.MessageBox.ShowWarningAsync("BasePoint code generator", "Selected class must inherit from 'BaseEntity'");
                return;
            }

            frm.ClassProperties = frm.CodeGenerationService.Properties;

            frm.GRD_Properties.ItemsSource = frm.ClassProperties;
            frm.BTN_Generate.IsEnabled = true;
            frm.BTN_Reload.IsEnabled = true;
            frm.PNL_InstructionsToLoadClass.Visibility = System.Windows.Visibility.Hidden;
            frm.PNL_GenerateClasses.Visibility = System.Windows.Visibility.Visible;
            frm.LBL_ClassProperties.Text = "Properties from " + frm.CodeGenerationService.FileName.Replace(":", "").Replace(".cs", "");

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}

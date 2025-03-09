using BasePointGenerator.Dtos;
using BasePointGenerator.Services;
using EnvDTE;
using EnvDTE80;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace BasePointGenerator
{
    /// <summary>
    /// Interaction logic for frmCodeGenerationOptionsControl.
    /// </summary>
    public partial class frmCodeGenerationOptionsControl : UserControl
    {
        private DTE2 _dte;
        public CodeGenerationService CodeGenerationService { get; set; }
        public IList<PropertyInfo> ClassProperties { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="frmCodeGenerationOptionsControl"/> class.
        /// </summary>
        public frmCodeGenerationOptionsControl()
        {
            this.InitializeComponent();
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;
        }

        public void WriteFileNewCreatedFileMessageToOutPutWindow(string message, bool blankLine = false)
        {
            WriteToOutputWindow(message, blankLine);
        }

        public void WriteToOutputWindow(string message, bool blankLine = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.OutputWindow outputWindow = _dte.ToolWindows.OutputWindow;

            EnvDTE.OutputWindowPane pane = null;
            try
            {
                foreach (EnvDTE.OutputWindowPane p in outputWindow.OutputWindowPanes)
                {
                    if (p.Name == "BasePoint generator")
                    {
                        pane = p;
                        break;
                    }
                }

                if (pane == null)
                {
                    pane = outputWindow.OutputWindowPanes.Add("BasePoint generator");
                }
            }
            catch (COMException)
            {

            }

            pane?.Activate();
            pane?.OutputString(message + "\n");
        }


        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void BTN_Generate_Click(object sender, RoutedEventArgs e)
        {
            GenerateAsync(false).Wait();
        }
        private void BTN_DeleteGenerated_Click(object sender, RoutedEventArgs e)
        {
            var deletionConfirm = VS.MessageBox.ShowConfirmAsync("BasePoint generator", "Confirm deletion of generated files.");

            if (!deletionConfirm.Result)
                return;

            GenerateAsync(true).Wait();

            foreach (var generatedFile in CodeGenerationService.GeneratedFiles)
            {
                File.Delete(generatedFile);
            }

            WriteFileNewCreatedFileMessageToOutPutWindow("Files below were deleted...");
            WriteFileNewCreatedFileMessageToOutPutWindow("", true);

            foreach (var file in CodeGenerationService.GeneratedFiles)
            {
                WriteFileNewCreatedFileMessageToOutPutWindow(file, true);
            }
        }

        private async Task GenerateAsync(bool onlyProcessFilePaths)
        {
            CodeGenerationService.GenerateFiles(
                generateCreateUseCase: SEL_GenerateCreateUseCase.IsChecked.Value,
                generateUpdateUseCase: SEL_GenerateUpdateUseCase.IsChecked.Value,
                generateDeleteUseCase: SEL_GenerateDeleteUseCase.IsChecked.Value,
                generateGetUseCase: SEL_GenerateGetUseCase.IsChecked.Value,
                onlyProcessFilePaths);

            if (!onlyProcessFilePaths)
            {
                WriteFileNewCreatedFileMessageToOutPutWindow("Files below were automatically generated...");
                WriteFileNewCreatedFileMessageToOutPutWindow("", true);

                foreach (var file in CodeGenerationService.GeneratedFiles)
                {
                    WriteFileNewCreatedFileMessageToOutPutWindow(file, true);
                }

                WriteFileNewCreatedFileMessageToOutPutWindow("", true);
                WriteFileNewCreatedFileMessageToOutPutWindow("Double click on file path to navigate.");
                WriteFileNewCreatedFileMessageToOutPutWindow("Please, remember to finish coding with your specific needs...");
            }

            if (!onlyProcessFilePaths)
                await VS.MessageBox.ShowWarningAsync("BasePoint generator", "Files generated with success.\nCheck out output window for details.");
        }

        private void BTN_Reload_Click(object sender, RoutedEventArgs e)
        {
            CodeGenerationService.ReloadFileInformations();

            this.ClassProperties = CodeGenerationService.Properties;
            GRD_Properties.ItemsSource = this.ClassProperties;
            BTN_Generate.IsEnabled = true;
            LBL_ClassProperties.Text = "Properties from " + CodeGenerationService.FileName.Replace(":", "").Replace(".cs", "");
        }
        private void SEL_GenerateAllUseCase_Checked(object sender, RoutedEventArgs e)
        {
            SEL_GenerateCreateUseCase.IsChecked = true;
            SEL_GenerateUpdateUseCase.IsChecked = true;
            SEL_GenerateDeleteUseCase.IsChecked = true;
            SEL_GenerateGetUseCase.IsChecked = true;
        }

        private void SEL_GenerateAllUseCase_Unchecked(object sender, RoutedEventArgs e)
        {
            SEL_GenerateCreateUseCase.IsChecked = false;
            SEL_GenerateUpdateUseCase.IsChecked = false;
            SEL_GenerateDeleteUseCase.IsChecked = false;
            SEL_GenerateGetUseCase.IsChecked = false;
        }

        private void BTN_GoToEntity_Click(object sender, RoutedEventArgs e)
        {
            VS.Documents.OpenAsync(Path.Combine(CodeGenerationService.OriginalFilePath, CodeGenerationService.FileName)).Wait();
        }

        private void SimulateKeyPress(Key key)
        {
            // Cria uma simulação do KeyEvent
            KeyEventArgs keyEventArgs = new KeyEventArgs(
                Keyboard.PrimaryDevice,
                PresentationSource.FromVisual(this),
                0,
                key)
            {
                RoutedEvent = Keyboard.KeyDownEvent
            };

            // Dispara o evento
            InputManager.Current.ProcessInput(keyEventArgs);
        }

        private void GRD_Properties_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var dataGrid = sender as DataGrid;

            if (dataGrid == null)
                return;

            if (e.Delta > 0)
                SimulateKeyPress(Key.Up);

            else if (e.Delta < 0)
                SimulateKeyPress(Key.Down);

            e.Handled = true;
        }

        private void OnlyNumbers(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void BlockSpace(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space)
                e.Handled = true;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;

            var entryValue = textBox.Text;

            var intValue = 0;

            int.TryParse(entryValue, out intValue);

            if (intValue <= 0)
                textBox.Text = "0";
        }
    }
}
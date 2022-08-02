using H264ToH265BatchConverter.Controls;
using H264ToH265BatchConverter.Logic;
using H264ToH265BatchConverter.Model;
using H264ToH265BatchConverter.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace H264ToH265BatchConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly List<string> CONST_FileExtensionsSupported = new List<string>{ ".mp4", ".mkv" };

        public List<FileConversion> CurrentFiles { get; set; }

        public bool Recursive = true;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            FileConversion.GlobalLogger += (log) => { Log(log); };

            CurrentFiles = new List<FileConversion>();
        }

        #region Conversion Management
        private async void ConvertDirectory(DirectoryInfo dir, bool recursive = true)
        {
            if (dir != null)
            {
                UpdateFileComponents(dir, recursive);

                foreach (var file in CurrentFiles)
                {
                    await ConvertFile(file);
                }
            }
        }

        private async Task ConvertFile(FileConversion file)
        {
            await ConvertAsync(file);

            LogConversionResult(file);

            UpdateTotalProgress();
        }

        private async Task ConvertAsync(FileConversion file)
        {
            var task = file.Convert();

            await task.WaitAsync(new CancellationToken());
        }
        #endregion

        #region Logs
        private void LogConversionResult(FileConversion file)
        {
            string log = "";

            if (file.ConversionSuccessed)
            {
                log = file.File.File.FullName + " converted !";
            }
            else
            {
                if (file.ConversionStatus == ConversionStatus.Failed)
                {
                    log = file.File.File.FullName + " conversion failed !";
                }
                else if (file.ConversionStatus == ConversionStatus.AlreadyConverted)
                {
                    log = file.File.File.FullName + " have been already converted in x265 !";
                }
            }

            Log(log);
        }

        private void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) { return; }
            tbLogs.Dispatcher?.Invoke(() => tbLogs.AppendText("[" + DateTime.Now.ToString("G") + "] " + message + Environment.NewLine));
            tbLogs.Dispatcher?.Invoke(() => tbLogs.ScrollToEnd());
        }
        #endregion

        #region UI Events
        private void btnConvertSelectedFolders_Click(object sender, RoutedEventArgs e)
        {
            string inputFolder;

            //CommonOpenFileDialog dialog = new CommonOpenFileDialog();

            FolderBrowserDialog openFolderDialog = new();
            openFolderDialog.ShowDialog();

            inputFolder = openFolderDialog.SelectedPath;

            if (Directory.Exists(inputFolder))
            {
                progressBarTotal.Value = 0;

                DirectoryInfo dir = new(inputFolder);

                ConvertDirectory(dir, Recursive);
            }
        }

        private async void btnConvertSelectedFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Multiselect = true,
                Filter = "Video Files|*.mp4;*.mkv;"
            };
            openFileDialog.ShowDialog();

            if (openFileDialog.FileNames.Length == 0) { return; }

            wrpPanelFiles.Children.Clear();
            CurrentFiles.Clear();
            progressBarTotal.Value = 0;

            foreach (var inputFile in openFileDialog.FileNames)
            {
                if (File.Exists(inputFile))
                {
                    FileInfo f = new(inputFile);

                    AddFileComponent(f);
                }
            }

            foreach (var file in CurrentFiles)
            {
                await ConvertFile(file);
            }
        }

        private void chkbRecursive_Checked(object sender, RoutedEventArgs e)
        {
            Recursive = true;
        }

        private void chkbRecursive_Unchecked(object sender, RoutedEventArgs e)
        {
            Recursive = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // On s'assure que tous les converter sont bien arrêté
            foreach (var f in CurrentFiles)
            {
                f.StopConversion();
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            string inputFolder;

            FolderBrowserDialog openFolderDialog = new();
            openFolderDialog.ShowDialog();

            inputFolder = openFolderDialog.SelectedPath;

            if (Directory.Exists(inputFolder))
            {
                DirectoryInfo dir = new(inputFolder);

                ConvertDirectory(dir, false);
            }
        }
        #endregion

        #region UI Update
        private void UpdateFileComponents(DirectoryInfo dir, bool recursive = true)
        {
            wrpPanelFiles.Children.Clear();
            CurrentFiles.Clear();

            AddFilesComponentFromFolder(dir, recursive);
        }

        private void AddFilesComponentFromFolder(DirectoryInfo dir, bool recursive = true)
        {
            var files = dir.EnumerateFiles();

            foreach (var f in files)
            {
                if (!CONST_FileExtensionsSupported.Contains(f.Extension.ToLower())) { continue; }
                AddFileComponent(f);
            }

            if (recursive)
            {
                var dirs = dir.EnumerateDirectories();

                foreach (var d in dirs)
                {
                    AddFilesComponentFromFolder(d, recursive);
                }
            }
        }

        private void AddFileComponent(FileInfo file)
        {
            var viewModel = new FileConversionViewModel(new Lakio.Framework.Core.IO.FileObject(file.FullName));

            wrpPanelFiles.Children.Add(new FileConversionComponent(viewModel));
            CurrentFiles.Add(new FileConversion(viewModel));
        }

        private void UpdateTotalProgress()
        {
            var total = CurrentFiles.Count;

            var done = CurrentFiles.Where(o => o.ConversionStatus != ConversionStatus.Pending && o.ConversionStatus != ConversionStatus.NotStarted).Count();

            if(total == 0) { return; }

            var currentPercentage = done * 100 / total;

            progressBarTotal.Dispatcher?.Invoke(() => { progressBarTotal.Value = currentPercentage; });
        }

        #endregion
    }
}

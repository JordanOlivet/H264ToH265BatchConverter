using H264ToH265BatchConverter.Controls;
using H264ToH265BatchConverter.Logic;
using H264ToH265BatchConverter.Model;
using H264ToH265BatchConverter.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;

namespace H264ToH265BatchConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly List<string> CONST_FileExtensionsSupported = new List<string> { ".mp4", ".mkv" };

        public List<FileConversion> CurrentFiles { get; set; }

        public bool Recursive = true;
        public bool ShowAlreadyConvertedFiles = false;

        private static string logFile = @".\log.txt";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            DisplayApplicationVersion();

            FileConversion.GlobalLogger += (log) => { Log(log); };

            CurrentFiles = new List<FileConversion>();

            System.Windows.Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private static void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is TaskCanceledException) { return; }

            File.WriteAllText(logFile, $"[{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}] {e.Exception.Message}{Environment.NewLine}{e.Exception.StackTrace}");
        }

        private void DisplayApplicationVersion()
        {
            tbVersion.Text = "v" + Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString();
        }

        #region Conversion Management

        private async Task DetectEncodingBeforeConversionAsync()
        {
            if (CurrentFiles == null || CurrentFiles.Count == 0) { return; }

            Dispatcher?.Invoke(() =>
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            });

            Log("Start analyzing files encoding ...");

            foreach (var file in CurrentFiles)
            {
                await Task.Run(async () =>
                {
                    await AsyncManager.DetectEncodingAsync(file, encoding =>
                    {
                        if (encoding.Equals("hevc"))
                        {
                            file.ConversionStatus = ConversionStatus.AlreadyConverted;
                            file.ConversionSucceeded = false;
                            file.SetFileAlreadyConverted();
                            file.Watch = new System.Diagnostics.Stopwatch();
                            file.UpdateFileConversionDuration();
                            file.SetVisibility(ShowAlreadyConvertedFiles);

                            LogConversionResult(file);

                            UpdateTotalProgress();

                            System.Windows.Forms.Application.DoEvents();
                        }
                    });
                });

                System.Windows.Forms.Application.DoEvents();
            }

            Log("All files have been analyzed. Only files in h264 encoding will be converted.");

            Dispatcher?.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }

        private List<FileInfo> GetFilesFromDirectory(DirectoryInfo dir)
        {
            var result = new List<FileInfo>();
            var files = dir.EnumerateFiles();

            foreach (var f in files)
            {
                if (!CONST_FileExtensionsSupported.Contains(f.Extension.ToLower()))
                {
                    continue;
                }

                result.Add(f);
            }

            if (Recursive)
            {
                var dirs = dir.EnumerateDirectories();

                foreach (var d in dirs)
                {
                    result.AddRange(GetFilesFromDirectory(d));
                }
            }

            return result;
        }

        private void DisplayAllFiles(List<FileInfo> files)
        {
            ClearUIFilesAndResetProgress();

            foreach (FileInfo file in files)
            {
                AddFileComponent(file);
                System.Windows.Forms.Application.DoEvents();
            }

            System.Windows.Forms.Application.DoEvents();
        }

        #endregion

        #region Logs

        private void LogConversionResult(FileConversion file)
        {
            string log = "";


            if (file.ConversionSucceeded)
            {
                if (file.ConversionStatus == ConversionStatus.NotNecessary)
                {
                    log = file.File.File.FullName + " : output file size exceeds file size input !";
                }
                else
                {
                    log = file.File.File.FullName + " converted !";
                }
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
                else if (file.ConversionStatus == ConversionStatus.OutputAlreadyExist)
                {
                    log = $"Output file ({file.Output.FullName}) already exist. Conversion aborted !";
                }
            }

            Log(log);
        }

        private void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            tbLogs.Dispatcher?.Invoke(() => tbLogs.AppendText("[" + DateTime.Now.ToString("G") + "] " + message + Environment.NewLine));
            tbLogs.Dispatcher?.Invoke(() => tbLogs.ScrollToEnd());
        }

        #endregion

        #region UI Events

        private async void btnSelectFolders_Click(object sender, RoutedEventArgs e)
        {
            string inputFolder;

            FolderBrowserDialog openFolderDialog = new();
            openFolderDialog.ShowDialog();

            inputFolder = openFolderDialog.SelectedPath;

            if (Directory.Exists(inputFolder))
            {
                btnStartConversion.IsEnabled = false;

                DirectoryInfo dir = new(inputFolder);

                DisplayAllFiles(GetFilesFromDirectory(dir));

                await DetectEncodingBeforeConversionAsync();

                btnStartConversion.IsEnabled = true;
            }
        }

        private async void btnSelectFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Multiselect = true,
                Filter = "Video Files|*.mp4;*.mkv;"
            };
            openFileDialog.ShowDialog();

            if (openFileDialog.FileNames.Length == 0)
            {
                return;
            }

            btnStartConversion.IsEnabled = false;

            var files = new List<FileInfo>();
            foreach (var inputFile in openFileDialog.FileNames)
            {
                if (File.Exists(inputFile))
                {
                    FileInfo f = new(inputFile);

                    files.Add(f);
                }
            }

            DisplayAllFiles(files);

            await DetectEncodingBeforeConversionAsync();

            btnStartConversion.IsEnabled = true;
        }


        private async void btnStartConversion_Click(object sender, RoutedEventArgs e)
        {
            btnSelectedFiles.IsEnabled = false;
            btnSelectedFolders.IsEnabled = false;
            btnStartConversion.IsEnabled = false;

            Log("Conversion started ...");

            foreach (var file in CurrentFiles.Where(o => o.ConversionStatus == ConversionStatus.NotStarted))
            {
                await AsyncManager.ConvertAsync(file, f =>
                {
                    LogConversionResult(f);
                    UpdateTotalProgress();
                });
            }

            Log("Conversion done.");

            btnSelectedFiles.IsEnabled = true;
            btnSelectedFolders.IsEnabled = true;
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
            // string inputFolder;

            // FolderBrowserDialog openFolderDialog = new();
            // openFolderDialog.ShowDialog();

            // inputFolder = openFolderDialog.SelectedPath;

            // if (Directory.Exists(inputFolder))
            // {
            //     DirectoryInfo dir = new(inputFolder);

            // }
        }

        private void chkbShowAlreadyConverted_Unchecked(object sender, RoutedEventArgs e)
        {
            ShowAlreadyConvertedFiles = false;
            UpdateFilesAlreadyConvertedVisibility();
        }

        private void chkbShowAlreadyConverted_Checked(object sender, RoutedEventArgs e)
        {
            ShowAlreadyConvertedFiles = true;
            UpdateFilesAlreadyConvertedVisibility();
        }

        #endregion

        #region UI Update

        private void AddFilesComponentFromFolder(DirectoryInfo dir, bool recursive = true)
        {
            var files = dir.EnumerateFiles();

            foreach (var f in files)
            {
                if (!CONST_FileExtensionsSupported.Contains(f.Extension.ToLower()))
                {
                    continue;
                }

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

            var conv = new FileConversion(viewModel);

            var compo = new FileConversionComponent(viewModel, (comp) =>
            {
                if (comp != null && conv != null)
                {
                    if (conv.ConversionStatus == ConversionStatus.Pending)
                    {
                        return; // Not allowed to remove a file if it is still converting
                    }

                    conv.ConversionStatus = ConversionStatus.Removed;
                    wrpPanelFiles.Children.Remove(comp);

                    UpdateTotalProgress();
                }
            });

            wrpPanelFiles.Children.Add(compo);
            CurrentFiles.Add(conv);
        }

        private void UpdateTotalProgress()
        {
            var total = CurrentFiles.Count;

            var done = CurrentFiles.Where(o => o.ConversionStatus != ConversionStatus.Pending && o.ConversionStatus != ConversionStatus.NotStarted).Count();

            if (total == 0)
            {
                return;
            }

            var currentPercentage = done * 100 / total;

            progressBarTotal.Dispatcher?.Invoke(() => { progressBarTotal.Value = currentPercentage; });
        }

        private void ClearUIFilesAndResetProgress()
        {
            wrpPanelFiles.Children.Clear();
            CurrentFiles.Clear();
            progressBarTotal.Value = 0;
        }



        private void UpdateFilesAlreadyConvertedVisibility()
        {
            foreach (var file in CurrentFiles)
            {
                if (file.ConversionStatus == ConversionStatus.AlreadyConverted)
                {
                    file.SetVisibility(ShowAlreadyConvertedFiles);
                }
            }
        }

        private void UpdateUIFileComponents()
        {
            foreach (var file in CurrentFiles)
            {
                var comp = file?.File?.FileComponent;

                if (comp != null)
                {
                    wrpPanelFiles.Children.Remove(comp);
                }
            }
        }

        #endregion

    }
}
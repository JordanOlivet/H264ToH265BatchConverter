using H264ToH265BatchConverter.Logic;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace H264ToH265BatchConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        double TotalMinutes = 0;

        bool Recursive { get; set; } = true;

        private H264Converter converter;

        public MainWindow()
        {
            InitializeComponent();
            converter = new();
            //converter.Logger += DisplayLog;
            converter.OnProgressChanged += Converter_OnProgressChanged;
            converter.onMessageDispath += Converter_MessageDispatch;
        }

        private void Converter_OnProgressChanged(double percentage)
        {
            Log("Pending ... " + percentage + "%");
        }

        private void Converter_MessageDispatch(String message)
        {
            Log(message);
        }

        private void ConvertFolderInBackground(DirectoryInfo dir, bool recursive = true)
        {
            BackgroundWorker wk = new()
            {
                WorkerReportsProgress = true
            };

            wk.DoWork += (s, e) =>
            {
                Stopwatch watch = new();
                watch.Start();

                ConvertDirectory(dir, wk, recursive);

                watch.Stop();
                TotalMinutes = Math.Round(watch.Elapsed.TotalMinutes, 2);
            };

            wk.ProgressChanged += (p, o) => { tbLogs.Dispatcher.Invoke(() => { Log((string)o.UserState); }); };

            wk.RunWorkerCompleted += (s, e) => { Log("Process done. Total time in minutes : " + TotalMinutes); };

            Log("Processing started");

            wk.RunWorkerAsync();
        }

        private void ConvertFileInBackground(FileInfo file)
        {
            BackgroundWorker wk = new()
            {
                WorkerReportsProgress = true
            };

            wk.DoWork += (s, e) =>
            {
                Stopwatch watch = new();
                watch.Start();

                ConvertFile(file, wk);

                watch.Stop();
                TotalMinutes = Math.Round(watch.Elapsed.TotalMinutes, 2);
            };

            wk.ProgressChanged += (p, o) => { tbLogs.Dispatcher.Invoke(() => { Log((string)o.UserState); }); };

            wk.RunWorkerCompleted += (s, e) => { Log("Process done. Total time in minutes : " + TotalMinutes); };

            wk.RunWorkerAsync();
        }

        private void ConvertDirectory(DirectoryInfo dir, BackgroundWorker wk = null, bool recursive = true)
        {
            if (dir != null)
            {
                var dirs = dir.EnumerateDirectories();
                var files = dir.EnumerateFiles();

                foreach (var f in files)
                {
                    ConvertFile(f, wk);
                }

                if (recursive)
                {
                    foreach (var d in dirs)
                    {
                        ConvertDirectory(d, wk);
                    }
                }
            }
        }

        private void ConvertFile(FileInfo f, BackgroundWorker wk = null)
        {
            if (f.Extension.ToLower() != ".mp4" && f.Extension.ToLower() != ".mkv")
            {
                return;
                ;
            }

            string input = f.FullName;
            string output = f.FullName.Replace(f.Extension, string.Empty) + "_h265" + f.Extension;

            Log("Processing started : " + input);

            if (converter.ToH265(input, output, true))
            {
                if (wk != null)
                {
                    wk.ReportProgress(0, f.FullName + " converted");
                }

                RemoveInputAndRenameOutput(f, output);
            }
        }

        private void RemoveInputAndRenameOutput(FileInfo input, string outputPath)
        {
            string tmp = input.FullName;
            input.Delete();
            FileInfo fOutput = new(outputPath);
            fOutput.MoveTo(tmp);
        }

        private void Log(string message)
        {
            tbLogs.Dispatcher?.Invoke(() =>
                tbLogs.Text += "[" + DateTime.Now.ToString("G") + "] " + message + Environment.NewLine);
        }

        private void btnConvertSoloFolder_Click(object sender, RoutedEventArgs e)
        {
            string inputFolder;

            FolderBrowserDialog openFolderDialog = new();
            openFolderDialog.ShowDialog();

            inputFolder = openFolderDialog.SelectedPath;

            if (Directory.Exists(inputFolder))
            {
                DirectoryInfo dir = new(inputFolder);

                ConvertFolderInBackground(dir, false);
            }
        }

        private void btnConvertMultiFolders_Click(object sender, RoutedEventArgs e)
        {
            string inputFolder;

            FolderBrowserDialog openFolderDialog = new();
            openFolderDialog.ShowDialog();

            inputFolder = openFolderDialog.SelectedPath;

            if (Directory.Exists(inputFolder))
            {
                DirectoryInfo dir = new(inputFolder);

                ConvertFolderInBackground(dir);
            }
        }

        private void btnConvertSoloFile_Click(object sender, RoutedEventArgs e)
        {
            string inputFile;

            System.Windows.Forms.OpenFileDialog openFileDialog = new();
            openFileDialog.ShowDialog();

            inputFile = openFileDialog.FileName;

            if (File.Exists(inputFile))
            {
                FileInfo f = new(inputFile);

                ConvertFileInBackground(f);
            }
        }

        private void btnConvertSelectedFolders_Click(object sender, RoutedEventArgs e)
        {
            string inputFolder;

            //CommonOpenFileDialog dialog = new CommonOpenFileDialog();

            FolderBrowserDialog openFolderDialog = new();
            openFolderDialog.ShowDialog();

            inputFolder = openFolderDialog.SelectedPath;

            if (Directory.Exists(inputFolder))
            {
                DirectoryInfo dir = new(inputFolder);

                ConvertFolderInBackground(dir);
            }
        }

        private void btnConvertSelectedFiles_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new();
            openFileDialog.Multiselect = true;
            openFileDialog.ShowDialog();

            foreach (var inputFile in openFileDialog.FileNames)
            {
                if (File.Exists(inputFile))
                {
                    FileInfo f = new(inputFile);

                    ConvertFileInBackground(f);
                }
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
            converter.Stop();
        }
    }
}
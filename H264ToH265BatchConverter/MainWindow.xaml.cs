using H264ToH265BatchConverter.Logic;
using Microsoft.Win32;
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

        public MainWindow()
        {
            InitializeComponent();
            H264Converter.Logger += (log) =>
            {
                tbLogs.Dispatcher.Invoke(() => { tbLogs.Text += log + Environment.NewLine;  });
            };
        }

        private void btnConvertMulti_Click(object sender, RoutedEventArgs e)
        {
            string inputFolder;

            FolderBrowserDialog openFolderDialog = new();
            openFolderDialog.ShowDialog();

            inputFolder = openFolderDialog.SelectedPath;

            if(Directory.Exists(inputFolder))
            {
                DirectoryInfo dir = new(inputFolder);

                ConvertInBackground(dir);
            }
        }

        private void ConvertInBackground(DirectoryInfo dir)
        {
            BackgroundWorker wk = new()
            {
                WorkerReportsProgress = true
            };

            wk.DoWork += (s, e) =>
            {
                Stopwatch watch = new();
                watch.Start();

                ConvertDirectory(dir, wk);

                watch.Stop();
                TotalMinutes = Math.Round(watch.Elapsed.TotalMinutes,2);
            };

            wk.ProgressChanged += (p, o) =>
            {
                tbLogs.Dispatcher.Invoke(() => { Log((string)o.UserState); });
            };

            wk.RunWorkerCompleted += (s, e) =>
            {
                Log("Process done. Total time in minutes : " + TotalMinutes);
            };

            Log("Processing started");

            wk.RunWorkerAsync();
        }

        private void ConvertDirectory(DirectoryInfo dir, BackgroundWorker wk = null)
        {
            if (dir != null)
            {
                var dirs = dir.EnumerateDirectories();
                var files = dir.EnumerateFiles();

                foreach (var f in files)
                {
                    if (f.Extension.ToLower() != ".mp4" && f.Extension.ToLower() != ".mkv") { continue; }
                    
                    string input = f.FullName;
                    string output = f.FullName.Replace(f.Extension, string.Empty) + "_h265" + f.Extension;

                    if (H264Converter.ToH265(input, output, false))
                    {
                        if (wk != null)
                        {
                            wk.ReportProgress(0, f.FullName + " converted");
                        }
                        string tmp = f.FullName;
                        f.Delete();
                        FileInfo fOutput = new(output);
                        fOutput.MoveTo(tmp);
                    }
                }

                foreach (var d in dirs)
                {
                    ConvertDirectory(d, wk);
                }
            }
        }

        private void Log(string message)
        {
            tbLogs.Text += "[" + DateTime.Now.ToString("G") + "] " + message + Environment.NewLine;
        }
    }
}

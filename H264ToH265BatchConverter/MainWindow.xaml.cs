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


        private void btnConvert_Click(object sender, RoutedEventArgs e)
        {
            string input;
            string output;

            //OpenFileDialog openFileDialog = new();
            //openFileDialog.Title = "Chosse file to convert";
            //openFileDialog.ShowDialog(this);

            //input = openFileDialog.FileName;

            //SaveFileDialog saveFileDialog = new();
            //saveFileDialog.Title = "Chosse destination file";
            //saveFileDialog.ShowDialog(this);

            //output = saveFileDialog.FileName;

            input = @"C:\Users\Lakio\Desktop\test_video_h264.mp4";
            output = @"C:\Users\Lakio\Desktop\test_video_h265.mp4";

            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output))
            {
                System.Windows.MessageBox.Show("At least one of the two files path is empty, please select correct path.");
                return;
            }

            tbLogs.Clear();

            H264Converter.ToH265(input, output);

            //if (!H264Converter.ToH265(input, output))
            //{
            //    MessageBox.Show("Convertion failed");
            //}
            //else
            //{
            //    MessageBox.Show("Convertion succeed");
            //}
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
                TotalMinutes = watch.Elapsed.TotalMinutes;
            };

            wk.ProgressChanged += (p, o) =>
            {
                tbLogs.Dispatcher.Invoke(() => { tbLogs.Text += o.UserState + Environment.NewLine; tbLogs.ScrollToEnd(); });
            };

            wk.RunWorkerCompleted += (s, e) =>
            {
                tbLogs.Text += "Total time in minutes : " + TotalMinutes + Environment.NewLine;
            };

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
    }
}

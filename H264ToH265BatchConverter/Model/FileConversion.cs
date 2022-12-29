using H264ToH265BatchConverter.Logic;
using H264ToH265BatchConverter.ViewModels;
using Lakio.Framework.Core.IO;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;

namespace H264ToH265BatchConverter.Model
{
    public class FileConversion
    {
        private const string CONST_PathImagePending = @"Resources\pending.png";

        //private const string CONST_PathImagePending = @"Resources\wait.gif";
        private const string CONST_PathImageConversionOk = @"Resources\ok.png";
        private const string CONST_PathImageConversionNotNecessary = @"Resources\conversionNotNecessary.png";
        private const string CONST_PathImageConversionKo = @"Resources\cross.png";
        private const string CONST_PathImageConversionAlreadyDone = @"Resources\conversionAlreadyDone.png";
        private const string CONST_H265Suffix = @"_h265";

        public static Action<string> GlobalLogger { get; set; }

        public FileConversionViewModel File { get; set; }

        public bool ConversionSucceeded { get; set; }

        public ConversionStatus ConversionStatus { get; set; } = ConversionStatus.NotStarted;

        public TimeSpan TotalMinutes { get; set; }

        public H264Converter Converter { get; set; }

        public Task InternalTask { get; private set; }

        private FileObject Output { get; set; }

        internal Stopwatch Watch { get; set; }

        public FileConversion(FileConversionViewModel file)
        {
            File = file;
        }

        public Task Convert()
        {
            InternalTask = new(() =>
            {
                ConversionStatus = ConversionStatus.Pending;
                Watch = new();
                Watch.Start();

                Timer timer = new()
                {
                    Interval = 1000
                };
                timer.Elapsed += (s, e) => { UpdateFileConversionDuration(); };
                timer.Start();

                Converter = new();

                Converter.OnProgressChanged += Converter_OnProgressChanged;

                //Converter.OnMessageDispath += Converter_MessageDispatch;

                Output = new FileObject(File.File.FullName.Replace(File.File.Extension, string.Empty) + CONST_H265Suffix + File.File.Extension);

                UpdateFileImageSource(CONST_PathImagePending);

                ConversionStatus = Converter.ToH265(File.File.FullName, Output.FullName);

                if (ConversionStatus == ConversionStatus.Success)
                {
                    if (OutputSmallerThanInput(Output, File.File))
                    {
                        RemoveInputAndRenameOutput(Output);
                        UpdateFileImageSource(CONST_PathImageConversionOk);
                    }
                    else
                    {
                        ConversionStatus = ConversionStatus.NotNecessary;
                        RemoveFile(Output);
                        UpdateFileImageSource(CONST_PathImageConversionNotNecessary);
                    }

                    ConversionSucceeded = true;
                }
                else
                {
                    ConversionSucceeded = false;

                    if (ConversionStatus == ConversionStatus.Failed)
                    {
                        UpdateFileImageSource(CONST_PathImageConversionKo);
                    }
                    else if (ConversionStatus == ConversionStatus.AlreadyConverted)
                    {
                        SetFileAlreadyConverted();
                    }
                }

                Watch.Stop();
                timer.Stop();
                TotalMinutes = Watch.Elapsed;
                UpdateFileConversionDuration();
            });

            InternalTask.Start();

            return InternalTask;
        }

        private bool OutputSmallerThanInput(FileObject output, FileObject input)
        {
            if (!output.Exists()) { return false; }

            bool outputSmallerThanInput = !(output.Size > input.Size);

            return outputSmallerThanInput;
        }

        private void RemoveFile(FileObject fileToDelete)
        {
            if (!fileToDelete.Exists()) { return; }

            fileToDelete.Delete();
        }

        public void StopConversion()
        {
            Converter?.Stop();

            if (File.File.Exists() && (Output?.Exists() ?? false) && Output.FullName.Contains(CONST_H265Suffix))
            {
                Output.Delete();
            }
        }

        private void RemoveInputAndRenameOutput(FileObject output)
        {
            string tmp = File.File.FullName;
            RemoveFile(File.File);
            output.MoveTo(tmp);
        }

        internal void Converter_OnProgressChanged(double percentage)
        {
            File.Dispatcher?.Invoke(() => { File.Progress = percentage; });
        }

        internal void UpdateFileImageSource(string pathImage)
        {
            File.Dispatcher?.Invoke(() => { File.SetImageSource(pathImage); });
        }

        internal void UpdateFileConversionDuration()
        {
            File.Dispatcher?.Invoke(() => { File.Duration = Watch.Elapsed.ToString(@"mm\:ss"); });
        }

        internal void SetFileAlreadyConverted()
        {
            UpdateFileImageSource(CONST_PathImageConversionAlreadyDone);
            // Forcing the progress to 100% because the file is indeed already converted
            Converter_OnProgressChanged(100);
        }

        internal void SetVisibility(bool visible)
        {
            File.Dispatcher?.Invoke(() => { File.FileVisibility = visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed; });
        }
    }
}
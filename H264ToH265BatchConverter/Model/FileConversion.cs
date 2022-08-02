using H264ToH265BatchConverter.Logic;
using H264ToH265BatchConverter.ViewModels;
using Lakio.Framework.Core.IO;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace H264ToH265BatchConverter.Model
{
    public class FileConversion
    {
        private const string CONST_PathImagePending = @"Resources\pending.png";
        //private const string CONST_PathImagePending = @"Resources\wait.gif";
        private const string CONST_PathImageConversionOk = @"Resources\ok.png";
        private const string CONST_PathImageConversionKo = @"Resources\cross.png";
        private const string CONST_PathImageConversionAlreadyDone = @"Resources\convertionAlreadyDone.png";

        public static Action<string> GlobalLogger { get; set; }

        public FileConversionViewModel File { get; set; }

        public bool ConversionSuccessed { get; set; }

        public ConversionStatus ConversionStatus { get; set; } = ConversionStatus.NotStarted;

        public TimeSpan TotalMinutes { get; set; }

        public H264Converter Converter { get; set; }

        public  Task InternalTask { get; private set; }

        private string Output { get; set; }

        private Stopwatch Watch { get; set; }

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

                Output = File.File.FullName.Replace(File.File.Extension, string.Empty) + "_h265" + File.File.Extension;

                UpdateFileImageSource(CONST_PathImagePending);

                ConversionStatus = Converter.ToH265(File.File.FullName, Output);

                if (ConversionStatus == ConversionStatus.Success)
                {
                    RemoveInputAndRenameOutput(Output);
                    UpdateFileImageSource(CONST_PathImageConversionOk);
                    ConversionSuccessed = true;
                }
                else
                {
                    ConversionSuccessed = false;

                    if (ConversionStatus == ConversionStatus.Failed)
                    {
                        UpdateFileImageSource(CONST_PathImageConversionKo);
                    }
                    else if(ConversionStatus == ConversionStatus.AlreadyConverted)
                    {
                        UpdateFileImageSource(CONST_PathImageConversionAlreadyDone);
                        // Forcing the progress to 100% because the file is indeed already converted
                        Converter_OnProgressChanged(100);
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

        public void StopConversion()
        {
            Converter?.Stop();

            if (System.IO.File.Exists(File.File.FullName) && !string.IsNullOrWhiteSpace(Output) && ConversionSuccessed)
            {
                FileInfo fi = new(Output);
                
                if(fi.Exists)
                {
                    fi.Delete();
                }
            }
        }

        private void RemoveInputAndRenameOutput(string outputPath)
        {
            string tmp = File.File.FullName;
            File.File.Delete();
            FileObject fOutput = new(outputPath);
            fOutput.MoveTo(tmp);
        }

        private void Converter_OnProgressChanged(double percentage)
        {
            File.Dispatcher?.Invoke(() => { File.Progress = percentage; });
        }

        //private void Converter_MessageDispatch(string message)
        //{
        //    GlobalLogger?.Invoke(message);
        //}

        private void UpdateFileImageSource(string pathImage)
        {
            File.Dispatcher?.Invoke(() => { File.SetImageSource(pathImage); });
        }

        private void UpdateFileConversionDuration()
        {
            File.Dispatcher?.Invoke(() => { File.Duration = Watch.Elapsed.ToString(@"mm\:ss"); });
        }
    }
}

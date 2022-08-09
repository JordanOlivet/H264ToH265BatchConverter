using Lakio.Framework.Core.System;
using System;
using System.Linq;

namespace H264ToH265BatchConverter.Logic
{
    public enum ConversionStatus
    {
        Success = 0,
        Failed = 1,
        AlreadyConverted = 2,
        Pending = 3,
        NotStarted = 4,
    }

    public class H264Converter
    {
        public double Percentage { get; private set; }

        public delegate void ProgressChanged(double percentage);

        public event ProgressChanged OnProgressChanged;
        
        //public delegate void MessageDispatcher(string message);

        //public event MessageDispatcher OnMessageDispath;

        private ProcessObject MpegProcess;

        private int TotalFrames = 0;
        
        public ConversionStatus ToH265(string input, string output)
        {
            TotalFrames = 0;

            // First, we retrieve the total frame count of the input file
            ProcessObject probeProcess = new(@".\ffmpeg\ffprobe.exe")
            {
                Arguments = $@"-v error -select_streams v:0 -count_packets -show_entries stream=nb_read_packets -of csv=p=0 ""{input}""",
                RedirectStandardError = false,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            probeProcess.Initialize((output) => { TotalFrames = Convert.ToInt32(output); });

            probeProcess.Start();

            probeProcess.WaitForCompletion();

            var fileEncoding = DetectFileEncoding(input);

            if (!fileEncoding.Equals("hevc"))
            {
                // Then we start the conversion
                MpegProcess = new(@".\ffmpeg\ffmpeg.exe")
                {
                    Arguments = $@"-hide_banner -loglevel error -stats -hwaccel cuda -hwaccel_device 0 -hwaccel_output_format cuda -v verbose -i ""{input}"" -c:v hevc_nvenc -gpu:v 0 -preset llhp -rc:v cbr -c:a copy ""{output}""",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                MpegProcess.Initialize(ComputeProgress);

                MpegProcess.Start();

                MpegProcess.WaitForCompletion();

                return MpegProcess.ExitCode == 0 ? ConversionStatus.Success : ConversionStatus.Failed;
            }
            else
            {
                //OnMessageDispath?.Invoke("File already encoded in x265");
                return ConversionStatus.AlreadyConverted;
            }
        }

        private static string DetectFileEncoding(string input)
        {
            // Secondly we detect the file encoding
            ProcessObject detectEncodingProcess = new(@".\ffmpeg\ffprobe.exe")
            {
                Arguments = $@" -v error -select_streams v:0 -show_entries stream=codec_name -of default=nokey=1:noprint_wrappers=1 -i ""{input}""",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string fileEncoding = "";
            detectEncodingProcess.Initialize((encoding) => { fileEncoding = encoding; });

            detectEncodingProcess.Start();

            detectEncodingProcess.WaitForCompletion();
            return fileEncoding;
        }

        private void ComputeProgress(string log)
        {
            if (!log.StartsWith("frame"))
            {
                return;
            }

            log = log.Replace("frame=", string.Empty);
            var res = log.Split(" ");

            try
            {
                double currentFrame = Convert.ToInt32(res.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o)));
                Percentage = Math.Round(currentFrame * 100 / TotalFrames, 1);
            }
            catch { }

            OnProgressChanged?.Invoke(Percentage);
        }

        public void Stop()
        {
            if (MpegProcess != null)
            {
                MpegProcess.Stop();
            }
        }
    }
}

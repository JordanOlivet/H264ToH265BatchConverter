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

        private TimeSpan TotalTime = TimeSpan.Zero;

        public ConversionStatus ToH265(string input, string output)
        {
            TotalTime = TimeSpan.Zero;

            DetectNumberOfFrames(input);

            var fileEncoding = DetectFileEncoding(input);

            if (!fileEncoding.Equals("hevc"))
            {
                // Then we start the conversion
                MpegProcess = new(@".\ffmpeg\ffmpeg.exe")
                {
                    Arguments =
                        $@" -c:v h264_cuvid -i ""{input}"" -hide_banner -loglevel error -stats -v verbose -map 0 -c:v hevc_nvenc -cq:v 19 -b:v 1643k -minrate 1150k -maxrate 2135k -bufsize 3286k -spatial_aq:v 1 -rc-lookahead:v 32 -c:a aac -c:s copy -max_muxing_queue_size 9999 ""{output}""",
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

            return ConversionStatus.Success;
        }

        private void DetectNumberOfFrames(string input)
        {
            ProcessObject probeProcess = new(@".\ffmpeg\ffprobe.exe")
            {
                Arguments =
                    $@"-v error -show_entries format=duration -v quiet -sexagesimal -of csv=p=0 ""{input}""",
                RedirectStandardError = false,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            probeProcess.Initialize((output) =>
            {
                int hours = Convert.ToInt32(output.Split(":")[0]);
                int minutes = Convert.ToInt32(output.Split(":")[1]);
                string secondsMilliseconds = output.Split(":")[2];
                int seconds = Convert.ToInt32(secondsMilliseconds.Split(".")[0]);
                TotalTime = new TimeSpan(0,
                    hours,
                    minutes,
                    seconds,
                    0);
            });

            probeProcess.Start();

            probeProcess.WaitForCompletion();
        }

        private static string DetectFileEncoding(string input)
        {
            // Secondly we detect the file encoding
            ProcessObject detectEncodingProcess = new(@".\ffmpeg\ffprobe.exe")
            {
                Arguments =
                    $@" -v error -select_streams v:0 -show_entries stream=codec_name -of default=nokey=1:noprint_wrappers=1 -i ""{input}""",
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

            // log = log.Replace("time=", ";;;;");
            // var res = log.Split(";;;;");
            
            int pFrom = log.IndexOf("time=") + "time=".Length;
            int pTo = log.LastIndexOf(" bitrate=");
            String result = log.Substring(pFrom, pTo - pFrom);
            try
            {
            
                string currentTime = result;
                
                int hours = Convert.ToInt32(currentTime.Split(":")[0]);
                int minutes = Convert.ToInt32(currentTime.Split(":")[1]);
                string secondsMilliseconds = currentTime.Split(":")[2];
                int seconds = Convert.ToInt32(secondsMilliseconds.Split(".")[0]);
                TimeSpan timeSpan = new TimeSpan(0, hours, minutes, seconds, 0);
                Percentage = ((double)timeSpan.Ticks / (double)TotalTime.Ticks) * 100;
            }
            catch
            {
            }

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
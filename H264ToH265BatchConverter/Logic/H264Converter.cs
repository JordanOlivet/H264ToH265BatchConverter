using Lakio.Framework.Core.System;
using System;
using System.ComponentModel;
using System.Linq;

namespace H264ToH265BatchConverter.Logic
{
    public class H264Converter
    {
        public double Percentage { get; private set; }

        //public Action<string> Logger { get; set; }

        public delegate void ProgressChanged(double percentage);

        public event ProgressChanged OnProgressChanged;

        private ProcessObject MpegProcess;

        private int TotalFrames = 0;

        //public void ToH265Async(string input, string output)
        //{
        //    BackgroundWorker wk = new();
        //    wk.DoWork += (s ,e)=>
        //    {
        //        if (ToH265(input, output, false))
        //        {

        //        }
        //        else
        //        {

        //        }
        //    };

        //    wk.RunWorkerCompleted += (s, e) => 
        //    {

        //    };
        //}

        public bool ToH265(string input, string output, bool logOn = true)
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

            // The we start the conversion
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

            if (MpegProcess.ExitCode == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ComputeProgress(string log)
        {
            if (!log.StartsWith("frame")) { return; }

            log = log.Replace("frame=", string.Empty);
            var res = log.Split(" ");

            try
            {
                double currentFrame = Convert.ToInt32(res.FirstOrDefault(o=>!string.IsNullOrWhiteSpace(o)));
                Percentage = Math.Round(currentFrame * 100 / TotalFrames, 1);
            }
            catch { }

            OnProgressChanged?.Invoke(Percentage);
        }

        public void Stop()
        {
            if(MpegProcess != null)
            {
                MpegProcess.Stop();
            }
        }

    }
}

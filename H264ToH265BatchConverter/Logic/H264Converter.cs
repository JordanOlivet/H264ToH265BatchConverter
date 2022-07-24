using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace H264ToH265BatchConverter.Logic
{
    public static class H264Converter
    {
        public static Action<string> Logger { get; set; }

        public static void ToH265Async(string input, string output)
        {
            BackgroundWorker wk = new();
            wk.DoWork += (s ,e)=>
            {
                if (ToH265(input, output, false))
                {

                }
                else
                {

                }
            };

            wk.RunWorkerCompleted += (s, e) => 
            {

            };
        }

        public static bool ToH265(string input, string output, bool logOn = true)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = @".\ffmpeg\ffmpeg.exe";
            proc.StartInfo.Arguments = $@"-hwaccel cuda -hwaccel_device 0 -hwaccel_output_format cuda -v verbose -i ""{input}"" -c:v hevc_nvenc -gpu:v 0 -preset llhp -rc:v cbr -c:a copy ""{output}""";

            //if (logOn) { Logger?.Invoke("Command line : " + proc.StartInfo.Arguments + Environment.NewLine); }

            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;

            if (!proc.Start())
            {
                if (logOn) { Logger?.Invoke("Starting failed"); }
                return false;
            }
            StreamReader reader = proc.StandardError;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (logOn) { Logger?.Invoke(line); }
            }

            int exitCode = proc.ExitCode;

            proc.Close();

            if (exitCode == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}

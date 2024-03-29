﻿using System.ComponentModel;
using System.Diagnostics;

namespace Lakio.Framework.Core.System
{
    public class ProcessObject
    {
        private Process internalProcess;

        private BackgroundWorker internalWorker;

        public string PathExe { get; set; }

        public string Arguments { get; set; }

        public bool RedirectStandardError { get; set; }

        public bool RedirectStandardOutput { get; set; }

        public bool UseShellExecute { get; set; }

        public bool CreateNoWindow { get; set; }

        public int ExitCode { get; set; }

        public List<string> Logs { get; private set; }
        public TimeSpan TotalExecutionTime { get; private set; }

        public delegate void LogRaised(string log);

        public event LogRaised OnLogRaised;

        public ProcessObject(string pathExe, string args = "")
        {
            PathExe = pathExe;
            Arguments = args;
            Logs = new List<string>();
        }

        public void Initialize(Action<string> logger = null)
        {
            internalProcess = new Process();
            internalProcess.StartInfo.FileName = PathExe;
            internalProcess.StartInfo.Arguments = Arguments;
            internalProcess.StartInfo.RedirectStandardError = RedirectStandardError;
            internalProcess.StartInfo.RedirectStandardOutput = RedirectStandardOutput;
            internalProcess.StartInfo.UseShellExecute = UseShellExecute;
            internalProcess.StartInfo.CreateNoWindow = CreateNoWindow;

            if (logger != null)
            {
                OnLogRaised += (log) => { logger(log); };
            }
        }

        public bool Start()
        {
            var res = internalProcess != null && internalProcess.Start();
            if (res)
            {
                internalWorker = new BackgroundWorker
                {
                    WorkerSupportsCancellation = true
                };
                internalWorker.DoWork += (o, e) =>
                {
                    if (RedirectStandardError)
                    {
                        using (StreamReader reader = internalProcess.StandardError)
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                Logs.Add(line);
                                OnLogRaised?.Invoke(line);
                            }
                        }
                    }
                    if (RedirectStandardOutput)
                    {
                        using (StreamReader reader = internalProcess.StandardOutput)
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                Logs.Add(line);
                                OnLogRaised?.Invoke(line);
                            }
                        }
                    }
                    return;
                };

                internalWorker.RunWorkerCompleted += (o, e) =>
                {
                    TotalExecutionTime = internalProcess?.TotalProcessorTime ?? new TimeSpan();
                    ExitCode = internalProcess?.ExitCode ?? 0;
                    internalProcess?.Close();
                };

                internalWorker.RunWorkerAsync();
            }
            return res;
        }

        public void Stop()
        {
            if (internalProcess != null)
            {
                try
                {
                    // On teste si le process est toujours vivant et rattaché à notre objet
                    var _ = internalProcess.Handle; // <== Si cette ligne plante, c'est que le process n'existe plus

                    if (!internalProcess.HasExited)
                    {
                        internalProcess.Kill();
                        internalProcess.WaitForExit();
                        internalProcess.Dispose();
                        internalProcess = null;
                    }
                }
                catch { }
            }

            if (internalWorker != null && internalWorker.IsBusy)
            {
                internalWorker.CancelAsync();
                internalWorker.Dispose();
                internalWorker = null;
            }
        }

        public void WaitForCompletion()
        {
            if (internalWorker != null && internalWorker.IsBusy)
            {
                while (internalWorker != null && internalWorker.IsBusy)
                {
                    Thread.Sleep(10);
                }
            }
        }

    }
}

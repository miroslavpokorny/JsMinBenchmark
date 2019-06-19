using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JsMinBenchmark.Util
{
    public static class ProcessStartInfoExtensions
    {
        public static Task<int> RunProcessAsync(this ProcessStartInfo processStartInfo)
        {
            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }

        // Inspired by https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
        public static IProcessResult RunAndMeasureProcess(this ProcessStartInfo processStartInfo)
        {
            using (var process = new Process{StartInfo = processStartInfo})
            {
                var stdOut = new StringBuilder();
                var stdErr = new StringBuilder();

                using (var outputWaitHandle = new AutoResetEvent(false))
                using (var errorWaitHandle = new AutoResetEvent(false))
                {
                    if (processStartInfo.RedirectStandardOutput)
                    {
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                                return;
                            }

                            stdOut.AppendLine(e.Data);
                        };
                    }

                    if (processStartInfo.RedirectStandardError)
                    {
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                errorWaitHandle.Set();
                                return;
                            }

                            stdErr.AppendLine(e.Data);
                        };                        
                    }

                    var stopWatch = Stopwatch.StartNew();

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    var timeout = 60000;

                    var isProcessFinished = process.WaitForExit(timeout) &&
                                            (!processStartInfo.RedirectStandardOutput ||
                                             outputWaitHandle.WaitOne(timeout)) &&
                                            (!processStartInfo.RedirectStandardError ||
                                             errorWaitHandle.WaitOne(timeout));
                    
                    stopWatch.Stop();
                    return new ProcessResult
                    {
                        IsTimeoutExpired = !isProcessFinished,
                        ExecutionTime = stopWatch.Elapsed,
                        ExitCode = isProcessFinished ? process.ExitCode : 0,
                        StdErr = stdErr.ToString(),
                        StdOut = stdOut.ToString()
                    };
                }
            }
        }
    }
}
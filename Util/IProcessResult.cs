using System;

namespace JsMinBenchmark.Util
{
    public interface IProcessResult
    {
        bool IsTimeoutExpired { get; }
        TimeSpan ExecutionTime { get; }
        int ExitCode { get; }
        string StdOut { get; }
        string StdErr { get; }
    }
}
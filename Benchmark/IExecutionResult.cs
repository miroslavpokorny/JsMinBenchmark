using System;

namespace JsMinBenchmark.Benchmark
{
    public interface IExecutionResult
    {
        string ToolName { get; }
        TimeSpan ExecutionTime { get; }
        string Result { get; }
        string Error { get; }
        int ExitCode { get; }
        bool IsTimeoutExpired { get; }
        long Utf8Size { get; }
        long GZipSize { get; }
    }
}
using System;

namespace JsMinBenchmark.Benchmark
{
    public interface IExecutionResult
    {
        string ToolName { get; }
        TimeSpan ExecutionTime { get; }
        string Result { get; }
        int ExitCode { get; }
        bool IsTimeoutExpired { get; }
        int Utf8Size { get; }
    }
}
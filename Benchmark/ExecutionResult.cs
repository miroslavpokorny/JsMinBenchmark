using System;
using JsMinBenchmark.Util;

namespace JsMinBenchmark.Benchmark
{
    public class ExecutionResult : IExecutionResult
    {
        public string ToolName { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string Result { get; set; }
        public string Error { get; set; }
        public int ExitCode { get; set; }
        public bool IsTimeoutExpired { get; set; }
        public long Utf8Size => Result.Utf8Length();
        public long GZipSize => Result.GZipLength();
    }
}
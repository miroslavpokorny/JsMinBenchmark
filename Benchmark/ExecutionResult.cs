using System;
using System.Text;

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
        public int Utf8Size => Encoding.UTF8.GetBytes(Result).Length;
    }
}
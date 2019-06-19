using System;

namespace JsMinBenchmark.Util
{
    class ProcessResult : IProcessResult
    {
        public bool IsTimeoutExpired { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public int ExitCode { get; set; }
        public string StdOut { get; set; }
        public string StdErr { get; set; }
    }
}
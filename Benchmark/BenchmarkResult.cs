using System.Collections.Generic;

namespace JsMinBenchmark.Benchmark
{
    public class BenchmarkResult : IBenchmarkResult
    {
        public BenchmarkResult(string libraryName, long originalUtf8Size)
        {
            LibraryName = libraryName;
            OriginalUtf8Size = originalUtf8Size;
        }

        public string LibraryName { get; }
        public long OriginalUtf8Size { get; }
        public List<IExecutionResult> ExecutionResults { get; } = new List<IExecutionResult>();
    }
}
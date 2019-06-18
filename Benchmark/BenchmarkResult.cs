using System.Collections.Generic;

namespace JsMinBenchmark.Benchmark
{
    public class BenchmarkResult : IBenchmarkResult
    {
        public BenchmarkResult(string libraryName)
        {
            LibraryName = libraryName;
        }

        public string LibraryName { get; }
        public List<IExecutionResult> ExecutionResults { get; } = new List<IExecutionResult>();
    }
}
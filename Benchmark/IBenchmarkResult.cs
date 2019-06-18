using System.Collections.Generic;

namespace JsMinBenchmark.Benchmark
{
    public interface IBenchmarkResult
    {
        string LibraryName { get; }
        List<IExecutionResult> ExecutionResults { get; }


    }
}
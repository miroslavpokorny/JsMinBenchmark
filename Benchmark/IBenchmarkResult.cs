using System.Collections.Generic;

namespace JsMinBenchmark.Benchmark
{
    public interface IBenchmarkResult
    {
        string LibraryName { get; }
        long OriginalUtf8Size { get; }
        long OriginalGZipSize { get; }
        List<IExecutionResult> ExecutionResults { get; }
    }
}
using System.Collections.Generic;

namespace JsMinBenchmark.Benchmark
{
    public class BenchmarkResult : IBenchmarkResult
    {
        public BenchmarkResult(string libraryName, long originalUtf8Size, long originalGZipSize)
        {
            LibraryName = libraryName;
            OriginalUtf8Size = originalUtf8Size;
            OriginalGZipSize = originalGZipSize;
        }

        public string LibraryName { get; }
        public long OriginalUtf8Size { get; }
        public long OriginalGZipSize { get; }
        public List<IExecutionResult> ExecutionResults { get; } = new List<IExecutionResult>();
    }
}
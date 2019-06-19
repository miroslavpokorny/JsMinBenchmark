using System.Collections.Generic;
using JsMinBenchmark.Benchmark;

namespace JsMinBenchmark.Output
{
    public interface IOutput
    {
        string GenerateOutput(IList<IBenchmarkResult> benchmarkResults);
    }
}
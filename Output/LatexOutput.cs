using System.Collections.Generic;
using System.Text;
using JsMinBenchmark.Benchmark;

namespace JsMinBenchmark.Output
{
    public class LatexOutput : IOutput
    {
        private StringBuilder _result;
        
        public string GenerateOutput(IList<IBenchmarkResult> benchmarkResults)
        {
            _result = new StringBuilder();
            if (benchmarkResults.Count == 0)
            {
                return string.Empty;
            }

            GenerateSizeTable(benchmarkResults);
            GenerateTimeTable(benchmarkResults);

            return _result.ToString();
        }

        private void GenerateSizeTable(IList<IBenchmarkResult> benchmarkResults)
        {
            var columns = benchmarkResults[0].ExecutionResults.Count + 2; 

            BeginTable(columns);
            var columnNames = new List<string> {"Library Name", "Original size"};
            foreach (var tool in benchmarkResults[0].ExecutionResults)
            {
                columnNames.Add(tool.ToolName);
            }
            GenerateHeaderRow(columnNames);

            foreach (var benchmarkResult in benchmarkResults)
            {
                GenerateSizeRow(benchmarkResult);
            }

            EndTable();
        }

        private void GenerateTimeTable(IList<IBenchmarkResult> benchmarkResults)
        {
            var columns = benchmarkResults[0].ExecutionResults.Count + 1;

            BeginTable(columns);
            var columnNames = new List<string> {"Library Name"};
            foreach (var tool in benchmarkResults[0].ExecutionResults)
            {
                columnNames.Add(tool.ToolName);
            }
            GenerateHeaderRow(columnNames);

            foreach (var benchmarkResult in benchmarkResults)
            {
                GenerateTimeRow(benchmarkResult);
            }

            EndTable();
        }

        private void BeginTable(int columns)
        {
            _result.AppendLine("\\begin{table}[]");
            _result.AppendLine($"\\begin{{tabular}}{{|{RepeatString("l|", columns)}}}");
            _result.AppendLine("\\hline");
        }

        private void GenerateHeaderRow(IList<string> columnNames)
        {
            for (var i = 0; i < columnNames.Count; i++)
            {
                if (i != 0)
                {
                    _result.Append(" & ");
                }
                _result.Append(columnNames[i]);
            }

            _result.AppendLine(" \\\\ \\hline");
        }

        private void GenerateTimeRow(IBenchmarkResult benchmarkResult)
        {
            var columns = benchmarkResult.ExecutionResults.Count + 1;
            _result.Append($"{benchmarkResult.LibraryName}");
            foreach (var result in benchmarkResult.ExecutionResults)
            {
                _result.Append($" & {result.ExecutionTime:s\\.fff}s");
            }
            
//            _result.AppendLine($" \\\\ \\cline{{2-{columns}}}");
            _result.AppendLine(" \\\\ \\hline");
        }

        private void GenerateSizeRow(IBenchmarkResult benchmarkResult)
        {
            var columns = benchmarkResult.ExecutionResults.Count + 2;
            _result.Append($"{benchmarkResult.LibraryName} & {benchmarkResult.OriginalUtf8Size}");
            foreach (var result in benchmarkResult.ExecutionResults)
            {
                _result.Append($" & {result.Utf8Size}");
            }
            
//            _result.AppendLine($" \\\\ \\cline{{2-{columns}}}");
            _result.AppendLine(" \\\\ \\hline");
        }

        private void EndTable()
        {
            _result.AppendLine("\\end{tabular}");
            _result.AppendLine("\\end{table}");
        }

        private string RepeatString(string text, int repeat)
        {
            var sb = new StringBuilder(text.Length * repeat);
            for (var i = 0; i < repeat; i++)
            {
                sb.Append(text);
            }

            return sb.ToString();
        }
    }
}
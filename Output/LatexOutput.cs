using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsMinBenchmark.Benchmark;

namespace JsMinBenchmark.Output
{
    public class LatexOutput : IOutput
    {
        private readonly int _maxToolsPerRow;
        private StringBuilder _result;

        public LatexOutput(int maxToolsPerRow)
        {
            _maxToolsPerRow = maxToolsPerRow;
        }

        public string GenerateOutput(IList<IBenchmarkResult> benchmarkResults)
        {
            _result = new StringBuilder();
            if (benchmarkResults.Count == 0)
            {
                return string.Empty;
            }

            GenerateSizeTables(benchmarkResults);
            GenerateTimeTables(benchmarkResults);

            return _result.ToString();
        }

        private void GenerateSizeTables(IList<IBenchmarkResult> benchmarkResults)
        {
            foreach (var i in Enumerable.Range(0, benchmarkResults[0].ExecutionResults.Count / _maxToolsPerRow))
            {
                var currentStartIndex = i * _maxToolsPerRow;
                var columnNames = new List<string> {"Library Name", "Original size"};
                
                foreach (var tool in benchmarkResults[0].ExecutionResults.GetRange(currentStartIndex, _maxToolsPerRow))
                {
                    columnNames.Add(tool.ToolName);
                }
                
                BeginTable(columnNames.Count);
                
                GenerateHeaderRow(columnNames);
                
                foreach (var benchmarkResult in benchmarkResults)
                {
                    GenerateSizeRow(benchmarkResult, currentStartIndex);
                }
                
                EndTable();
            }
        }

        private void GenerateTimeTables(IList<IBenchmarkResult> benchmarkResults)
        {
            foreach (var i in Enumerable.Range(0, benchmarkResults[0].ExecutionResults.Count / _maxToolsPerRow))
            {
                var currentStartIndex = i * _maxToolsPerRow;
                var columnNames = new List<string> {"Library Name"};

                foreach (var tool in benchmarkResults[0].ExecutionResults.GetRange(currentStartIndex, _maxToolsPerRow))
                {
                    columnNames.Add(tool.ToolName);
                }
                
                BeginTable(columnNames.Count);
                GenerateHeaderRow(columnNames);
                
                foreach (var benchmarkResult in benchmarkResults)
                {
                    GenerateTimeRow(benchmarkResult, currentStartIndex);
                }
                
                EndTable();
            }
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

        private void GenerateTimeRow(IBenchmarkResult benchmarkResult, int startIndex)
        {
            _result.Append($"{benchmarkResult.LibraryName}");
            foreach (var result in benchmarkResult.ExecutionResults.GetRange(startIndex, _maxToolsPerRow))
            {
                _result.Append($" & {result.ExecutionTime:s\\.fff}s");
            }
            
            _result.AppendLine(" \\\\ \\hline");
        }

        private void GenerateSizeRow(IBenchmarkResult benchmarkResult, int startIndex)
        {
            _result.Append($"{benchmarkResult.LibraryName} & {benchmarkResult.OriginalUtf8Size}");
            foreach (var result in benchmarkResult.ExecutionResults.GetRange(startIndex, _maxToolsPerRow))
            {
                _result.Append($" & {result.Utf8Size}");
            }
            
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
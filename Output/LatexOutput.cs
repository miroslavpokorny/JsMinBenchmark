using System;
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
            GenerateSizeTables(benchmarkResults, true);
            GenerateTimeTables(benchmarkResults);

            return _result.ToString();
        }

        private void GenerateSizeTables(IList<IBenchmarkResult> benchmarkResults, bool gZippedSize = false)
        {
            BeginTable();
            GenerateCaption(gZippedSize ? "gzip size table" : "Size table");
            var upperIndex = (int)Math.Ceiling(benchmarkResults[0].ExecutionResults.Count / (double)_maxToolsPerRow);
            foreach (var i in Enumerable.Range(0, upperIndex))
            {
                var currentStartIndex = i * _maxToolsPerRow;
                var columnNames = new List<string> {"Library Name"};
                if (i == 0)
                {
                    columnNames.Add(gZippedSize ? "gzip size" : "Original size");
                }

                var numberOfItems = Math.Min(_maxToolsPerRow,
                    benchmarkResults[0].ExecutionResults.Count - i * _maxToolsPerRow);
                foreach (var tool in benchmarkResults[0].ExecutionResults.GetRange(currentStartIndex, numberOfItems))
                {
                    columnNames.Add(tool.ToolName);
                }
                
                BeginTabular(columnNames.Count);
                
                GenerateHeaderRow(columnNames);
                
                foreach (var benchmarkResult in benchmarkResults)
                {
                    GenerateSizeRow(benchmarkResult, currentStartIndex, numberOfItems, gZippedSize);
                }
                
                EndTabular();
            }

            GenerateLabel(gZippedSize ? "tab:GzipSizeTable" : "tab:SizeTable");
            EndTable();
        }

        private void GenerateTimeTables(IList<IBenchmarkResult> benchmarkResults)
        {
            BeginTable();
            GenerateCaption("Duration table");
            var upperIndex = (int)Math.Ceiling(benchmarkResults[0].ExecutionResults.Count / (double)_maxToolsPerRow);
            foreach (var i in Enumerable.Range(0, upperIndex))
            {
                var currentStartIndex = i * _maxToolsPerRow;
                var columnNames = new List<string> {"Library Name"};
                var numberOfItems = Math.Min(_maxToolsPerRow,
                    benchmarkResults[0].ExecutionResults.Count - i * _maxToolsPerRow);
                foreach (var tool in benchmarkResults[0].ExecutionResults.GetRange(currentStartIndex, numberOfItems))
                {
                    columnNames.Add(tool.ToolName);
                }
                
                BeginTabular(columnNames.Count);
                GenerateHeaderRow(columnNames);
                
                foreach (var benchmarkResult in benchmarkResults)
                {
                    GenerateTimeRow(benchmarkResult, currentStartIndex, numberOfItems);
                }
                
                EndTabular();
            }

            GenerateLabel("tab:DurationTable");
            EndTable();
        }
        
        private void GenerateCaption(string caption)
        {
            _result.AppendLine($"\\caption{{{caption}}}");
        }

        private void GenerateLabel(string label)
        {
            _result.AppendLine($"\\label{{{label}}}");
        }

        private void BeginTable()
        {
            _result.AppendLine("\\begin{table}[!ht]");
        }

        private void BeginTabular(int columns)
        {
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

        private void GenerateTimeRow(IBenchmarkResult benchmarkResult, int startIndex, int numberOfItems)
        {
            _result.Append($"{benchmarkResult.LibraryName}");
            foreach (var result in benchmarkResult.ExecutionResults.GetRange(startIndex, numberOfItems))
            {
                _result.Append($" & {result.ExecutionTime:s\\.fff}s");
            }
            
            _result.AppendLine(" \\\\ \\hline");
        }

        private void GenerateSizeRow(IBenchmarkResult benchmarkResult, int startIndex, int numberOfItems, bool gZippedSize)
        {
            _result.Append($"{benchmarkResult.LibraryName}");
            if (startIndex == 0)
            {
                _result.Append($" & {(gZippedSize ? benchmarkResult.OriginalGZipSize : benchmarkResult.OriginalUtf8Size)}");
            }
            foreach (var result in benchmarkResult.ExecutionResults.GetRange(startIndex, numberOfItems))
            {
                _result.Append($" & {(gZippedSize ? result.GZipSize : result.Utf8Size)}");
            }
            
            _result.AppendLine(" \\\\ \\hline");
        }

        private void EndTable()
        {
            _result.AppendLine("\\end{table}");
        }

        private void EndTabular()
        {
            _result.AppendLine("\\end{tabular}");
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
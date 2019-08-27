using CommandLine;

namespace JsMinBenchmark.Cli
{
    [Verb("benchmark", HelpText = "Runs minification benchmark")]
    public class BenchmarkOptions
    {
        [Option('o', "output", Required = false, Default = "file", HelpText = "Specify where output should go. Possible values are file|console")]
        public string Output { get; set;}

        [Option("output-file", Required = false, Default = "benchmark-result.tex", HelpText = "Override output file name")]
        public string OutputFile { get; set; }
        
        [Option('t', "tools-per-row", Required = false, Default = 5, HelpText = "Maximal number of tools per row in output")]
        public int MaxToolsPerRow { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using CommandLine;
using JsMinBenchmark.Benchmark;
using JsMinBenchmark.Cli;
using JsMinBenchmark.JsonInput;
using JsMinBenchmark.Output;
using JsMinBenchmark.Tools;
using JsMinBenchmark.Util;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace JsMinBenchmark
{
    public class Application
    {
        private readonly string[] _args;
        private readonly ILogger _logger;
        private IOutput _output;
        private bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;
        private string ShellExecutable => IsWindows ? "cmd.exe" : "/bin/bash";

        public Application(string[] args)
        {
            _args = args;
            InitializeLogger();
            _logger = LogManager.GetCurrentClassLogger();
        }
        
        static void InitializeLogger()
        {
            var config = new LoggingConfiguration();
            var coloredConsoleTarget = new ColoredConsoleTarget();
#if DEBUG            
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, coloredConsoleTarget);
#else
            config.AddRule(LogLevel.Info, LogLevel.Fatal, coloredConsoleTarget);
#endif
            LogManager.Configuration = config;
        }
        
        public int Start()
        {
            try
            {
                return Parser.Default.ParseArguments<BenchmarkOptions, InitializeToolsOptions>(_args)
                    .MapResult(
                        (BenchmarkOptions opts) => RunBenchmark(opts),
                        (InitializeToolsOptions opts) => RunInitializeTools(opts),
                        errs => (int) ErrorCodes.InvalidArguments
                    );
            }
            catch (Exception exception)
            {
                _logger.Fatal(exception);
                return (int) ErrorCodes.FatalError;
            }
        }
        
        int RunBenchmark(BenchmarkOptions options)
        {
            InitializeOutput(options.MaxToolsPerRow);
            _logger.Info("Starting Benchmark");
            var toolsInfo = JsonConvert.DeserializeObject<ToolsJson>(File.ReadAllText("./Tools/tools.json"));
            var testFilesDir = "./TestFiles";
            var workingDir = "./temp";
            var testFilesInfo =
                JsonConvert.DeserializeObject<TestFilesJson>(File.ReadAllText($"{testFilesDir}/testFiles.json"));

            var benchmarkResults = new List<IBenchmarkResult>();

            foreach (var testFile in testFilesInfo.TestFiles)
            {
                var testFilePath = Path.GetFullPath($"{testFilesDir}/{testFile.Directory}/lib.js");
                if (!File.Exists(testFilePath))
                {
                    _logger.Warn($"test file: {testFilePath} Was not found!");
                    continue;
                }

                _logger.Info($"Starting benchmark suite of {testFile.Name}@{testFile.Version}");

                var originalContent = File.ReadAllText(testFilePath);
                var result = new BenchmarkResult($"{testFile.Name}@{testFile.Version}", originalContent.Utf8Length(), originalContent.GZipLength());
                foreach (var tool in toolsInfo.Tools)
                {
                    _logger.Info($"Starting benchmark with tool {tool.Name}");
                    var toolDirPath = $"{workingDir}/{tool.Name.Replace(' ', '_')}{(tool.Npm == null ? "" : "/node_modules/.bin")}{(tool.ExecDir == null ? "" : $"/{tool.ExecDir}")}";

                    var execCommand = tool.ExecCommand;
                    var execArguments = tool.ExecArguments.Replace("%INPUT_FILE%", testFilePath);
                    var isScript = tool.ExecCommand.StartsWith("./");

                    if (isScript)
                    {
                        execArguments = $"{(IsWindows ? $"/C {execCommand.Substring(2)}" : $"-c \"{execCommand}")} {execArguments}{(IsWindows ? "" : "\"")}";
                        execCommand = ShellExecutable;
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = execCommand,
                        Arguments = execArguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetFullPath(toolDirPath),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    var processResult = startInfo.RunAndMeasureProcess();
                    if (processResult.IsTimeoutExpired)
                    {
                        _logger.Warn("Benchmark has timeouted!");
                    }
                    else
                    {
                        _logger.Info("Benchmark finished");
                    }
                    
                    result.ExecutionResults.Add(new ExecutionResult
                    {
                        ToolName = tool.Name,
                        ExecutionTime = processResult.ExecutionTime,
                        Result = processResult.StdOut,
                        Error = processResult.StdErr,
                        ExitCode = processResult.ExitCode,
                        IsTimeoutExpired = processResult.IsTimeoutExpired
                    });
                }

                benchmarkResults.Add(result);
                _logger.Info($"Benchmark suite of {testFile.Name}@{testFile.Version} ended");
            }
            
            _logger.Info("Benchmark done");

            var output = _output.GenerateOutput(benchmarkResults);
            switch(options.Output.ToLower()) {
                case "file":
                    File.WriteAllText(options.OutputFile, output);
                    break;
                case "console": 
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine(output);
                    break;
                default: 
                    throw new ArgumentException("output argument has unknown value");
            }
            
            return 0;
        }

        private void InitializeOutput(int maxToolsPerRow)
        {
            _output = new LatexOutput(maxToolsPerRow);
        }

        int RunInitializeTools(InitializeToolsOptions options)
        {
            _logger.Info("starting InitializeTools command");
            var workingDir = "./temp";
            if (Directory.Exists(workingDir))
            {
                Directory.Delete(workingDir, true);
            }

            Directory.CreateDirectory(workingDir);
            
            var toolsInfo = JsonConvert.DeserializeObject<ToolsJson>(File.ReadAllText("./Tools/tools.json"));

            var tasks = new List<Task>(toolsInfo.Tools.Count);
            foreach (var tool in toolsInfo.Tools)
            {
                if (tool.Npm != null)
                {
                    tasks.Add(InitNpmTool(tool));
                    continue;
                }
                if (tool.Download != null)
                {
                    tasks.Add(InitDownloadTool(tool));
                    continue;
                }

                if (tool.GitSource != null)
                {
                    tasks.Add(InitGitSourceTool(tool));
                    continue;
                }
                
                throw new SyntaxErrorException($"tools.json is invalid. Look at entry with name = {tool.Name}");
            }
            
            Task.WhenAll(tasks).GetAwaiter().GetResult();

            _logger.Info("InitializeTools command done");
            return 0;

            async Task InitNpmTool(Tool tool)
            {
                var package = $"{tool.Npm.Package}@{tool.Npm.Version}";
                var workingDirectory = $"{workingDir}/{tool.Name.Replace(' ', '_')}";
                Directory.CreateDirectory(workingDirectory);
                _logger.Info($"Initialization of npm package {package}");
                var processStartInfo = ShellProcessStartInfo($"npm install {package}", workingDirectory);
                var exitCode = await processStartInfo.RunProcessAsync();
                if (exitCode != 0)
                {
                    throw new Exception($"npm process for package {package} exited with non zero code {exitCode}");
                }
                _logger.Info($"npm package {package} is successfully initialized");
            }

            async Task InitDownloadTool(Tool tool)
            {
                _logger.Info($"Downloading file from {tool.Download.Url}");
                var webClient = new WebClient();
                var downloadFileName = $"{workingDir}/{tool.Download.FileName}";
                await webClient.DownloadFileTaskAsync(tool.Download.Url, downloadFileName);
                _logger.Info($"File downloaded {tool.Download.FileName}. starting extraction of archive ");
                ZipFile.ExtractToDirectory(downloadFileName, $"{workingDir}/{tool.Name.Replace(' ', '_')}");
                _logger.Info($"Archive {tool.Download.FileName} extracted");
            }

            async Task InitGitSourceTool(Tool tool)
            {
                var workingDirectory = $"{workingDir}/{tool.Name.Replace(' ', '_')}";
                _logger.Info($"Clonning git repository {tool.GitSource.Url}");
                var processStartInfo = ShellProcessStartInfo($"git clone {tool.GitSource.Url} {tool.Name}", workingDir);
                var exitCode = await processStartInfo.RunProcessAsync();
                if (exitCode != 0)
                {
                    throw new Exception($"Clonning of repository {tool.GitSource.Url} exited with non zero code {exitCode}");
                }
                _logger.Info($"Start building {tool.Name} from git sources");
                processStartInfo = ShellProcessStartInfo($"{tool.GitSource.BuildCommand}", workingDirectory);
                exitCode = await processStartInfo.RunProcessAsync();
                if (exitCode != 0)
                {
                    throw new Exception($"Build of {tool.Name} exited with non zero code {exitCode}");
                }
                _logger.Info($"Tool {tool.Name} has been successfully builded");
            }
        }

        private ProcessStartInfo ShellProcessStartInfo(string command, string workingDirectory)
        {
            return new ProcessStartInfo
            {
                FileName = ShellExecutable,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"{(IsWindows ? "/C " : "-c \"")}{command}{(IsWindows ? "" : "\"")}",
                WorkingDirectory = workingDirectory
            };
        }
    }
}
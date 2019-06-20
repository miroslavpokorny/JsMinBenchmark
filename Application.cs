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
        private readonly IOutput _output;
        
        public Application(string[] args)
        {
            _args = args;
            InitializeLogger();
            _logger = LogManager.GetCurrentClassLogger();
            _output = new LatexOutput();
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
            _logger.Info("Starting Benchmark");
            var toolsInfo = JsonConvert.DeserializeObject<ToolsJson>(File.ReadAllText("./Tools/tools.json"));
            var testFilesDir = "./TestFiles";
            var workingDir = "./temp";
            var testFilesInfo =
                JsonConvert.DeserializeObject<TestFilesJson>(File.ReadAllText($"{testFilesDir}/testFiles.json"));

            var benchmarkResults = new List<IBenchmarkResult>();

            foreach (var testFile in testFilesInfo.TestFiles)
            {
                var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
                var testFilePath = Path.GetFullPath($"{testFilesDir}/{testFile.Directory}/lib.js");
                if (!File.Exists(testFilePath))
                {
                    _logger.Warn($"test file: {testFilePath} Was not found!");
                    continue;
                }

                _logger.Info($"Starting benchmark suite of {testFile.Name}@{testFile.Version}");

                var result = new BenchmarkResult($"{testFile.Name}@{testFile.Version}", new FileInfo(testFilePath).Length);
                foreach (var tool in toolsInfo.Tools)
                {
                    _logger.Info($"Starting benchmark with tool {tool.Name}");
                    var toolDirPath = $"{workingDir}/{tool.Name.Replace(' ', '_')}{(tool.Npm == null ? "" : "/node_modules/.bin")}{(tool.ExecDir == null ? "" : $"/{tool.ExecDir}")}";

                    var execCommand = tool.ExecCommand;
                    var execArguments = tool.ExecArguments.Replace("%INPUT_FILE%", testFilePath);
                    var isScript = tool.ExecCommand.StartsWith("./");

                    if (isScript)
                    {
                        execArguments = $"{(isWindows ? $"/C {execCommand.Substring(2)}" : $"-c \"{execCommand}")} {execArguments}{(isWindows ? "" : "\"")}";
                        execCommand = isWindows ? "cmd.exe" : "/bin/bash";
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

            File.WriteAllText("benchmark-result.tex", _output.GenerateOutput(benchmarkResults));
            return 0;
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
                
                throw new SyntaxErrorException($"tools.json is invalid. Look at entry with name = {tool.Name}");
            }
            
            Task.WhenAll(tasks).GetAwaiter().GetResult();

            _logger.Info("InitializeTools command done");
            return 0;

            async Task InitNpmTool(Tool tool)
            {
                var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
                var package = $"{tool.Npm.Package}@{tool.Npm.Version}";
                var workingDirectory = $"{workingDir}/{tool.Name.Replace(' ', '_')}";
                Directory.CreateDirectory(workingDirectory);
                _logger.Info($"Initialization of npm package {package}");
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = isWindows ? "cmd.exe" : "/bin/bash",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = $"{(isWindows ? "/C " : "-c \"")}npm install {package}{(isWindows ? "" : "\"")}",
                    WorkingDirectory = workingDirectory
                };
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
        }
    }
}
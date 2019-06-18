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
using JsMinBenchmark.Tools;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Fluent;
using NLog.Targets;

namespace JsMinBenchmark
{
    public class Application
    {
        private readonly string[] _args;
        private readonly ILogger _logger;
        
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
            _logger.Info("Starting Benchmark");
            var toolsInfo = JsonConvert.DeserializeObject<ToolsJson>(File.ReadAllText("./Tools/tools.json"));
            var testFilesDir = "./TestFiles";
            var workingDir = "./temp";
            var testFilesInfo =
                JsonConvert.DeserializeObject<TestFilesJson>(File.ReadAllText($"{testFilesDir}/testFiles.json"));

            var benchmarkResults = new List<BenchmarkResult>();
            
            var stopwatch = new Stopwatch();
            
            foreach (var testFile in testFilesInfo.TestFiles)
            {
                var testFilePath = Path.GetFullPath($"{testFilesDir}/{testFile.Directory}/lib.js");
                if (!File.Exists(testFilePath))
                {
                    _logger.Warn($"test file: {testFilePath} Was not found!");
                    continue;
                }

                _logger.Info($"Starting benchmark of {testFile.Name}@{testFile.Version}");

                var result = new BenchmarkResult($"{testFile.Name}@{testFile.Version}");
                foreach (var tool in toolsInfo.Tools)
                {
                    var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
                    var toolDirPath = $"{workingDir}/{tool.Name}{(tool.Npm == null ? "" : "/node_modules/.bin")}{(tool.ExecDir == null ? "" : $"/{tool.ExecDir}")}";
                    var outputData = new List<string>();
                    var processFinished = false;
                    var processExitCode = 0;
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = isWindows ? "cmd.exe" : "bash",
                            // FileName = tool.ExecCommand,
                            // Arguments = tool.ExecArguments.Replace("%INPUT_FILE%", testFilePath),
                            UseShellExecute = false,
                            CreateNoWindow = false,
                            WorkingDirectory = toolDirPath,
                            RedirectStandardOutput = true,
                            RedirectStandardInput = true
                        },
                        EnableRaisingEvents = true
                    };

                    process.Exited += (sender, args) =>
                    {
                        processExitCode = process.ExitCode;
                        processFinished = true;
                        process.Dispose();
                    };

                    process.OutputDataReceived += (sender, args) => { outputData.Add(args.Data); };
                    
                    stopwatch.Reset();
                    
                    process.Start();
                    // TODO debug 
                    process.StandardInput.WriteLine($"{tool.ExecCommand} {tool.ExecArguments.Replace("%INPUT_FILE%", testFilePath)} {(isWindows ? " & exit %errorlevel%" : " && exit $?")}");
                    process.WaitForExit(60 * 1000); // Timeout 1 minute
                    var timeoutExpired = !processFinished;
                    stopwatch.Stop();
                    
                    result.ExecutionResults.Add(new ExecutionResult
                    {
                        ToolName = tool.Name,
                        ExecutionTime = stopwatch.Elapsed,
                        Result = string.Join("", outputData),
                        ExitCode = processExitCode,
                        IsTimeoutExpired = timeoutExpired
                    });
                }

                benchmarkResults.Add(result);
                _logger.Info($"Benchmark of {testFile.Name}@{testFile.Version} ended");
            }
            
            _logger.Info("Benchmark done");
            
            
            throw new NotImplementedException();
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
                var package = $"{tool.Npm.Package}@{tool.Npm.Version}";
                var workingDirectory = $"{workingDir}/{tool.Name}";
                Directory.CreateDirectory(workingDirectory);
                _logger.Info($"Initialization of npm package {package}");
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "npm",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    Arguments = $"install {package}",
                    WorkingDirectory = workingDirectory
                };
                var exitCode = await RunProcessAsync(processStartInfo);
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
                ZipFile.ExtractToDirectory(downloadFileName, $"{workingDir}/{tool.Name}");
                _logger.Info($"Archive {tool.Download.FileName} extracted");
            }
        }
        
        static Task<int> RunProcessAsync(ProcessStartInfo processStartInfo)
        {
            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }
    }
}
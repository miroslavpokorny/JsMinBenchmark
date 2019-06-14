using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using CommandLine;
using JsMinBenchmark.Cli;
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
            _logger.Info("Benchmark");
            var toolsInfo = JsonConvert.DeserializeObject<ToolsJson>(File.ReadAllText("./Tools/tools.json"));
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
                var workingDirectory = $"{workingDir}/{tool.Npm.Package}";
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
                ZipFile.ExtractToDirectory(downloadFileName, $"{workingDir}/{Path.GetFileNameWithoutExtension(downloadFileName)}");
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
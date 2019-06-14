using System;
using System.IO;
using System.Net;
using CommandLine;
using JsMinBenchmark.Cli;
using JsMinBenchmark.Tools;
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
            _logger.Info("InitializeTools");
            var toolsInfo = JsonConvert.DeserializeObject<ToolsJson>(File.ReadAllText("./Tools/tools.json"));
            throw new NotImplementedException();
        }
    }
}
using System;
using CommandLine;
using JsMinBenchmark.Cli;
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
            throw new NotImplementedException();
        }

        int RunInitializeTools(InitializeToolsOptions options)
        {
            _logger.Info("InitializeTools");
            throw new NotImplementedException();
        }
    }
}
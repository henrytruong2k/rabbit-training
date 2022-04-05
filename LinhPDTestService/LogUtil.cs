using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Security.Cryptography;
using LoggerManager = log4net.Core.LoggerManager;

namespace LinhPDTestService
{
    public static class LogUtil
    {
        private static readonly string VALID_CHAR = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(ConfigureLogging);
        private static readonly ILogger _logger = _loggerFactory.CreateLogger("");
        static LogUtil()
        {
            Dictionary<string, MethodInfo> methods = typeof(LoggerManager).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).ToDictionary(x => x.Name, x => x);
            AppDomain.CurrentDomain.ProcessExit -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), null, methods["OnProcessExit"]);
            AppDomain.CurrentDomain.DomainUnload -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), null, methods["OnDomainUnload"]);
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs eventArgs) =>
            {
                if (eventArgs.ExceptionObject is Exception ex)
                    _logger.LogError(ex);
            };
            TaskScheduler.UnobservedTaskException += (object sender, UnobservedTaskExceptionEventArgs eventArgs) =>
            {
                _logger.LogError(eventArgs.Exception);
            };
        }

        public static void ConfigureLogging(ILoggingBuilder loggingBuilder)
        {
            if (!string.IsNullOrEmpty(Configuration.Log4NetConfigurationPath) && File.Exists(Path.Combine(AppContext.BaseDirectory, Configuration.Log4NetConfigurationPath)))
            {
                loggingBuilder.AddLog4Net(Path.Combine(AppContext.BaseDirectory, Configuration.Log4NetConfigurationPath));
            }
            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
        }

        public static ILogger CreateLogger(string categoryName = "") => _loggerFactory.CreateLogger(categoryName);

        public static string GenerateLMID()
        {
            string lmid = string.Empty;
            for (int i = 0; i < 6; i++)
            {
                lmid += VALID_CHAR[RandomNumberGenerator.GetInt32(0, VALID_CHAR.Length)];
            }
            if (lmid.Equals("AAAAAA"))
            {
                return GenerateLMID();
            }
            return lmid;
        }

        public static void LogError(this ILogger logger, Exception ex) => logger.LogError(ex, "");
    }
}

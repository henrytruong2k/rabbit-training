using Microsoft.Extensions.Logging;
using rabbit.client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETUtilities.Utils
{
    public static class LogUtil
    {
        private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(ConfigureLogging);
        public static ILogger CreateLogger(string categoryName = "") => _loggerFactory.CreateLogger(categoryName);

        public static void ConfigureLogging(ILoggingBuilder loggingBuilder)
        {
            if (!string.IsNullOrEmpty(BaseConfiguration.Log4NetConfigurationPath) && File.Exists(Path.Combine(AppContext.BaseDirectory, BaseConfiguration.Log4NetConfigurationPath)))
            {
                loggingBuilder.AddLog4Net(Path.Combine(AppContext.BaseDirectory, BaseConfiguration.Log4NetConfigurationPath));
            }
            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
        }
    }
}

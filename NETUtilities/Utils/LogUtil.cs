using Microsoft.Extensions.Logging;
using rabbit.client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NETUtilities.Utils
{
    public static class LogUtil
    {
        private static readonly string VALID_CHAR = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
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

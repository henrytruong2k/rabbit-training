using Microsoft.Extensions.Configuration;

namespace LinhPDTestService
{
    public static class Configuration
    {
        private static readonly IConfiguration _appSettings = new ConfigurationBuilder()
           .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
           .Build();
        public static readonly string Log4NetConfigurationPath = _appSettings["Log4NetConfigurationPath"];
        public static readonly string AppName = _appSettings["AppName"];
        public static readonly string RabbitMQHosts = _appSettings["RabbitMQ:Hosts"];
        public static readonly string RabbitMQUsername = _appSettings["RabbitMQ:Username"];
        public static readonly string RabbitMQVirtualHost = _appSettings["RabbitMQ:VirtualHost"] ?? "/";
        public static readonly string RabbitMQPassword = _appSettings["RabbitMQ:Password"];
    }
}

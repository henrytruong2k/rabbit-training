
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rabbit.client
{
    public static class BaseConfiguration
    {
        private static readonly IConfiguration _appSettings = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "baseconfiguration.json"), true)
            .Build();

        public static readonly string RabbitMQHosts = _appSettings["RabbitMQ:Hosts"];
        public static readonly string RabbitMQUsername = _appSettings["RabbitMQ:Username"];
        public static readonly string RabbitMQPassword = _appSettings["RabbitMQ:Password"];
        public static readonly string RabbitMQVirtualHost = _appSettings["RabbitMQ:VirtualHost"] ?? "/";
        public static readonly string RabbitMQQueueName = _appSettings["RabbitMQ:QueueName"];

        public static IConfiguration GetConfiguration() => _appSettings;
    }
}

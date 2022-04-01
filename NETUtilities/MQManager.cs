using rabbit.client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETUtilities;

public static class MQManager
{
    private static readonly Dictionary<string, IModel> _models = new();
    private static readonly ConnectionFactory _connectionFactory = new()
    {
        UserName = BaseConfiguration.RabbitMQUsername,
        Password = BaseConfiguration.RabbitMQPassword,
        VirtualHost = BaseConfiguration.RabbitMQVirtualHost
    };

    public static IConnection GetConnection() => _connectionFactory.CreateConnection(BaseConfiguration.RabbitMQHosts.Split(';').Select(x => new AmqpTcpEndpoint(new Uri((x.StartsWith("amqp://") ? string.Empty : "amqp://") + x))).ToList());


}

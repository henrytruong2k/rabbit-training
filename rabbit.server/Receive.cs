using rabbit.client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

class Receive
{
    public static void Main()
    {
        var factory = new ConnectionFactory();
        factory.UserName = BaseConfiguration.RabbitMQUsername;
        factory.Password = BaseConfiguration.RabbitMQPassword;
        factory.VirtualHost = BaseConfiguration.RabbitMQVirtualHost;
        factory.HostName = BaseConfiguration.RabbitMQHosts;
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: BaseConfiguration.RabbitMQQueueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);
            };
            channel.BasicConsume(queue: BaseConfiguration.RabbitMQQueueName,
                                 autoAck: true,
                                 consumer: consumer);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
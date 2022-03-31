using System;
using RabbitMQ.Client;
using System.Text;
using rabbit.client;

class Send
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

            string message = "Hello ATG!";
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "",
                                 routingKey: BaseConfiguration.RabbitMQQueueName,
                                 basicProperties: null,
                                 body: body);
            Console.WriteLine(" [x] Sent {0}", message);
        }

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
}
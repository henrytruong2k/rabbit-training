using System;
using RabbitMQ.Client;
using System.Text;
using rabbit.client;
using NETUtilities;

class Send
{
    public static void Main()
    {
        using (var connection = MQManager.GetConnection())
        using (var channel = MQManager.GetConnection().CreateModel())
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
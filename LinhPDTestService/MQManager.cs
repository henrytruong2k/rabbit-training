using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace LinhPDTestService;
public static class MQManager
{
    private static readonly ILogger _logger = LogUtil.CreateLogger();
    private static readonly Dictionary<string, IModel> _models = new();
    private static readonly ConnectionFactory _connectionFactory = new()
    {
        UserName = Configuration.RabbitMQUsername,
        Password = Configuration.RabbitMQPassword,
        VirtualHost = Configuration.RabbitMQVirtualHost
    };

    public static object JsonOption { get; private set; }

    private static IConnection GetConnection() => _connectionFactory.CreateConnection(Configuration.RabbitMQHosts.Split(';').Select(x => new AmqpTcpEndpoint(new Uri((x.StartsWith("amqp://") ? string.Empty : "amqp://") + x))).ToList());

    private static void CreateConsumer(QueueActionType actionType, QueueFormat format, Action<BasicDeliverEventArgs> handler)
    {
        try
        {
            string queueName = $"{Configuration.AppName}.{actionType}.{format}";
            IModel channel = GetConnection().CreateModel();
            channel.QueueDeclare(queueName, false, false, false);
            EventingBasicConsumer consumer = new(channel);
            consumer.Received += (model, message) =>
            {
                try
                {
                    handler(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{message.BasicProperties.MessageId}: {Configuration.AppName}.{actionType}.{format}");
                }
            };
            channel.BasicConsume(consumer, queueName, autoAck: true);
            _models.Add(queueName, channel);
            _logger.LogInformation($"CreateConsumer successfully: QueueName={Configuration.AppName}, QueueActionType={actionType}, QueueFormat={format}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CreateConsumer failed: QueueName={Configuration.AppName}, QueueActionType={actionType}, QueueFormat={format}");
        }
    }


    /// <param name="action">fromChannel, lmid, functionName, body</param>
    public static void CreateJsonConsumer(QueueActionType actionType, Action<string, string, string, string> action)
    {
        CreateConsumer(actionType, QueueFormat.Json, eventAgrs =>
        {
            if (string.IsNullOrEmpty(eventAgrs.BasicProperties.MessageId))
            {
                eventAgrs.BasicProperties.MessageId = LogUtil.GenerateLMID();
            }
            string body = Encoding.UTF8.GetString(eventAgrs.Body.Span);
            string functionName = Encoding.UTF8.GetString(eventAgrs.BasicProperties.Headers["FunctionName"] as byte[]);
            _logger.LogDebug($"{eventAgrs.BasicProperties.MessageId}: fr{eventAgrs.BasicProperties.ReplyTo}+{actionType}+{functionName}: {body}");
            action(eventAgrs.BasicProperties.ReplyTo, eventAgrs.BasicProperties.MessageId, functionName, body);
        });
    }

    public static IModel CreateProducer(string app, QueueActionType actionType, QueueFormat format)
    {
        try
        {
            string queueName = $"{app}.{actionType}.{format}";
            if (!_models.ContainsKey(queueName))
            {
                IModel channel = GetConnection().CreateModel();
                channel.QueueDeclare(queueName, false, false, false);
                _models.Add(queueName, channel);
            }
            _logger.LogInformation($"CreateProducer successfully: QueueName={app}, QueueActionType={actionType}, QueueFormat={format}");
            return _models[queueName];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CreateProducer failed: QueueName={app}, QueueActionType={actionType}, QueueFormat={format}");
        }
        return null;
    }

    public static bool SendJsonRequest<T>(string lmid, string app, string functionName, T data, string requestID = null) where T : class
    {
        try
        {
            string queueName = $"{app}.{QueueActionType.Request}.{QueueFormat.Json}";
            RequestDTO<T> requestDTO = new()
            {
                RequestID = requestID ?? Guid.NewGuid().ToString(),
                Data = data,
                LMID = lmid
            };
            IModel channel = CreateProducer(app, QueueActionType.Request, QueueFormat.Json);
            IBasicProperties properties = channel.CreateBasicProperties();
            properties.MessageId = lmid;
            properties.ReplyTo = Configuration.AppName;
            properties.Headers = new Dictionary<string, object>
            {
                { "FunctionName", functionName }
            };
            _logger.LogDebug($"{lmid}: to{app}+{QueueActionType.Request}+{functionName}: {JsonSerializer.Serialize(data)}. RequestID: {requestDTO.RequestID}");
            channel.BasicPublish("", queueName, properties, JsonSerializer.SerializeToUtf8Bytes(requestDTO));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{lmid}: Cannot send to: {app}+{QueueActionType.Request}");
            return false;
        }
    }
}

public class RequestDTO<TRequest> where TRequest : class
{
    public string LMID { get; set; }

    public string RequestID { get; set; }

    public TRequest Data { get; set; }
}

public enum QueueFormat
{
    Json
}

public enum QueueActionType
{
    Request, Response
}
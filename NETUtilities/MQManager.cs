using Microsoft.Extensions.Logging;
using NETUtilities.Utils;
using rabbit.client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NETUtilities;

public static class MQManager
{
    private static readonly ILogger _logger = LogUtil.CreateLogger();
    private static readonly Dictionary<string, IModel> _models = new();
    private static readonly ConnectionFactory _connectionFactory = new()
    {
        UserName = BaseConfiguration.RabbitMQUsername,
        Password = BaseConfiguration.RabbitMQPassword,
        VirtualHost = BaseConfiguration.RabbitMQVirtualHost
    };

    public static IConnection GetConnection() => _connectionFactory.CreateConnection(BaseConfiguration.RabbitMQHosts.Split(';').Select(x => new AmqpTcpEndpoint(new Uri((x.StartsWith("amqp://") ? string.Empty : "amqp://") + x))).ToList());

    private static void CreateConsumer(QueueActionType actionType, QueueFormat format, Action<BasicDeliverEventArgs> handler)
    {
        try
        {
            string queueName = $"{BaseConfiguration.AppName}{BaseConfiguration.Environment}.{actionType}.{format}";
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
                    _logger.LogError(ex, $"{message.BasicProperties.MessageId}: {BaseConfiguration.AppName}.{actionType}.{format}");
                }
            };
            channel.BasicConsume(consumer, queueName, autoAck: true);
            _models.Add(queueName, channel);
            _logger.LogInformation($"CreateConsumer successfully: QueueName={BaseConfiguration.AppName}{BaseConfiguration.Environment}, QueueActionType={actionType}, QueueFormat={format}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CreateConsumer failed: QueueName={BaseConfiguration.AppName}, QueueActionType={actionType}, QueueFormat={format}");
        }
    }

    /// <param name="action">fromChannel, lmid, functionName, body</param>
    public static void CreateJsonConsumer(QueueActionType actionType, Action<Apps, string, string, string> action)
    {
        CreateConsumer(actionType, QueueFormat.Json, eventAgrs =>
        {
            if (string.IsNullOrEmpty(eventAgrs.BasicProperties.MessageId))
            {
                eventAgrs.BasicProperties.MessageId = LogUtil.GenerateLMID();
            }
            string body = Encoding.UTF8.GetString(eventAgrs.Body.Span);
            string functionName = Encoding.UTF8.GetString(eventAgrs.BasicProperties.Headers["FunctionName"] as byte[]);
            _logger.LogDebug($"{eventAgrs.BasicProperties.MessageId}: fr{eventAgrs.BasicProperties.ReplyTo}+{actionType}+{functionName}: {DataMasking.MaskJson(body)}");
            action(new Apps(eventAgrs.BasicProperties.ReplyTo), eventAgrs.BasicProperties.MessageId, functionName, body);
        });
    }

    public static IModel CreateProducer(Apps app, QueueActionType actionType, QueueFormat format)
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

    public static bool SendJsonRequest<T>(string lmid, Apps app, string functionName, T data, string requestID = null) where T : class
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
            properties.ReplyTo = BaseConfiguration.AppName;
            properties.Headers = new Dictionary<string, object>
            {
                { "FunctionName", functionName }
            };
            _logger.LogDebug($"{lmid}: to{app}+{QueueActionType.Request}+{functionName}: {DataMasking.MaskJson(data)}. RequestID: {requestDTO.RequestID}");
            channel.BasicPublish("", queueName, properties, JsonSerializer.SerializeToUtf8Bytes(requestDTO, JsonOption.SerializerOptions));
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
    [JsonIgnore]
    public string LMID { get; set; }

    [Required]
    public string RequestID { get; set; }

    [Required]
    public TRequest Data { get; set; }
}

public class ResponseDTO<TResponse> : RequestDTO<TResponse> where TResponse : class
{
    public string ResponseCode { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ResponseStatus Status { get; set; }

    public BadRequestDetail[] BadRequestDetails { get; set; }
}

public class BadRequestDetail
{
    public string FieldName { get; set; }

    public string Message { get; set; }
}


public enum QueueActionType
{
    Request, Response
}

public struct Apps
{
    public static readonly Apps TransactionService = new("TransactionService");
    public static readonly Apps HuyTQService = new("HuyTQService");

    private readonly string _app;
    public Apps(string app) { _app = app + BaseConfiguration.Environment; }
    public static implicit operator string(Apps app) => app._app;
    public static explicit operator Apps(string app) => new(app);
    public override string ToString() => _app;
}

public enum QueueFormat
{
    Json, Iso
}

public enum ResponseStatus : int
{
    Success = 200,
    BadRequest = 400,
    DuplicationRequest = 409,
    InternalServerError = 500,
    ExternalServerError = 505,
    RequestTimeout = 504,

    InvalidFunction = 001,
    CardCancelled = 002,
    CardExpired = 003,
    InvalidData = 004,
    BranchNotExist = 005,

    InsertDBFailure = 010,

    InvalidCurrencyLine = 100,
    InvalidAsset = 101,
    InsufficientBalance = 102,
    OverDailyLimit = 103,
    OverNumOfTrxnDailyLimit = 104,
    OverMonthlyLimit = 105,

    CardNotExist = 107,
    CMSAccountNotExist = 108,
    OverLimit = 113,
    CardLocked = 114,
    InvalidAmount = 115,
    NoData = 116,
    CustomerAlreadyExists = 117,
    CustomerNotExist = 118,
    BlacklistedCustomers = 119,

    TransactionNotExist = 120,
    TransactionWasSettlemented = 121,
    SettlementBusy = 123,
    NotEnoughMoney = 124
}
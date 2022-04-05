using NETUtilities;
using Microsoft.Extensions.Logging;
using NETUtilities.Utils;
using rabbit.client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

class Receive
{
    private static readonly ILogger _logger = LogUtil.CreateLogger();
    public static void Main()
    {
        MQManager.CreateJsonConsumer(QueueActionType.Response, (fromChannel, lmid, functionName, body) => { });
    }
}
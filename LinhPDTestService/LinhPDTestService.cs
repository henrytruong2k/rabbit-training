using LinhPDTestService;
using Microsoft.Extensions.Logging;

ILogger logger = LogUtil.CreateLogger();
MQManager.CreateJsonConsumer(QueueActionType.Response, (channel, lmid, functionName, body) =>
{
});
MQManager.SendJsonRequest(LogUtil.GenerateLMID(), "TransactionService", "AccountBalanceInquiry", new { AccountNumber = "12345678" });
Console.ReadLine();
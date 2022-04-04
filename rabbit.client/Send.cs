using System;
using RabbitMQ.Client;
using System.Text;
using rabbit.client;
using NETUtilities;
using NETUtilities.Utils;
using System.ComponentModel.DataAnnotations;

class Send
{
    public static void Main()
    {
        string lmid = LogUtil.GenerateLMID();
        MQManager.SendJsonRequest(lmid, Apps.TransactionService, "AccountBalanceInquiry", new AccountBalanceInquiryRequest() { AccountNumber = "1234488144544023" });
    }
}

public class AccountBalanceInquiryRequest
{
    public string AccountNumber { get; set; }
}
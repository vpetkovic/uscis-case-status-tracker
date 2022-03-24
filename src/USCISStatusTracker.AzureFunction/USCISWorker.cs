using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using USCIS.Common;

namespace Worker.USCIS;

public class UscisWorker
{
    private readonly StatusTrackerClient _statusTrackerClient;
    private readonly IConfiguration _config;
    public UscisWorker(
        StatusTrackerClient statusTrackerClient,
        IConfiguration configuration)
    {
        _statusTrackerClient = statusTrackerClient;
        _config = configuration;
    }

    [FunctionName("Cases")]
    public async Task<IActionResult> GetCasesStatusCheck(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "uscis/casestatus/{receipts}/{persistResults}")]
        HttpRequest req, string receipts, ILogger log, bool persistResults = false)
    {
        log.LogInformation("Checking cases: {Receipts}", receipts);

        bool toPersist = persistResults && _config.GetValue<bool>("AzureTableStorage:Enabled");
        var result = await _statusTrackerClient.GetReceiptStatusAsync(
            receipts.Split(';').Select(x => new Receipt() { ReceiptNumber = x }),
            persistCaseStatus:toPersist);

        return new OkObjectResult(result);
    }

    [FunctionName("CasesOnTimer")]
    public async Task GetCasesStatusCheckPeriodically([TimerTrigger("0 * * * * *", RunOnStartup = true)] TimerInfo
    myTimer, ILogger log)
    {
        if (_config.GetSection("Cases").Exists() && _config.GetSection("Cases").GetChildren().Any())
        {
            
            var cases = _config.GetSection("Cases").GetChildren().Select(x => new Receipt(){ ReceiptNumber = x.Value });

            var result = await _statusTrackerClient.GetReceiptStatusAsync(cases, persistCaseStatus:_config.GetValue<bool>("AzureTableStorage:Enabled"));

            result.ForEach(x => log.LogInformation("{Receipt} new updates => {Status}", x.ReceiptNumber, x.HasNewStatus.ToString().ToUpperInvariant()));

            #region Send Email
            if (_config.GetValue<bool>("SendGridSettings:Enabled") && result.Any(n => n.HasNewStatus))
            {
                var sendgrid = _config.GetSection("SendGridSettings");

                if (!string.IsNullOrEmpty(sendgrid["ToAddresses"]) && sendgrid["ToAddresses"].Split(';').Length > 0)
                {
                    var emailTo = sendgrid["ToAddresses"].Split(';').Select(e => new EmailAddress(e)).ToList();
                    var msg = new SendGridMessage()
                    {
                        From = new EmailAddress(sendgrid["FromAddress"], sendgrid["FromName"]),
                        Subject = _config["SendGridSettings:Subject"],
                        PlainTextContent = FormatCollectionForEmailBody(result.Where(x => x.HasNewStatus).ToList(), isPlainText:true),
                        HtmlContent = FormatCollectionForEmailBody(result.Where(x => x.HasNewStatus).ToList())
                    };
                    msg.AddTos(emailTo);
                    try
                    {
                        var client = new SendGridClient(_config["SendGridSettings:ApiKey"]);
                        var status = await client.SendEmailAsync(msg);

                        log.LogInformation("Email sent: {Status}", status.IsSuccessStatusCode.ToString());
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Failed to send email");
                    }
                }
            }

            #endregion

            #region Send SMS
            if (_config.GetValue<bool>("TwilioSMS:Enabled") && result.Any(n => n.HasNewStatus))
            {
                var twilio = _config.GetSection("TwilioSMS");
                var apiKey = twilio["ApiKey"];
                var accountSid = twilio["AccountSid"];
                var fromPhoneNum = twilio["FromPhoneNumber"];
                var toPhoneNums = twilio.GetSection("ToPhoneNumbers").GetChildren().Select(x => x.Value).ToList();
                var smsContent = string.Join("\n\n", result.Where(x => x.HasNewStatus).Select(m => m.CurrentStatusDetails));

                if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(fromPhoneNum) && toPhoneNums.Any())
                {
                    TwilioClient.Init(accountSid, apiKey);

                    toPhoneNums.ForEach(phone =>
                    {
                        try
                        {
                            var message = MessageResource.CreateAsync(
                                body: smsContent,
                                from: new Twilio.Types.PhoneNumber(fromPhoneNum),
                                to: new Twilio.Types.PhoneNumber(phone)
                            );

                            log.LogInformation("SMS to {Phone} sent: {Status}", phone, message.Result.Status.ToString());
                        }
                        catch (Exception ex)
                        {
                            log.LogError(ex, "Failed to send SMS to {Phone}", phone);
                        }
                    });
                }


            }
            #endregion
        }
    }

    #region Private Members
        private string FormatCollectionForEmailBody(IEnumerable<ReceiptStatus> data, bool isPlainText
            = false)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var item in data)
            {
                sb.Append(@$"
                    {(isPlainText ? "Last Check:" : "<strong>Last Check:</strong>")} {DateTime.SpecifyKind(item.LastChecked,
                    DateTimeKind.Utc).ToLocalTime().ToString("G")} {(isPlainText ? " " : "<br>")}
                    {(isPlainText ? "Receipt#:" : "<strong>Receipt#:</strong>")} {item.ReceiptNumber} {(isPlainText ? " " : "<br>")}
                    {(isPlainText ? "Status:" : "<strong>Status:</strong>")} {(item.HasNewStatus ? @"(NEW STATUS)":"(NOT CHANGED)")} 
                        {item.CurrentStatus} {(isPlainText ? " " : "<br>")}
                    {(isPlainText ? "Description:" : "<strong>Description:</strong>")} {item.CurrentStatusDetails} {(isPlainText ? " " : "<br>")}
                ");
                sb.Append($@"
                    {(isPlainText ? "----------------------------------" : "<hr>")} {(isPlainText ? " " : "<br>")}
                ");
            }

            return sb.ToString();
        }


    #endregion

}
using HtmlAgilityPack;

namespace USCIS.Common;

public class StatusTrackerClient
{
    private readonly HttpClient _httpClient;
    private readonly IDataAccess _db;

    public StatusTrackerClient(HttpClient httpClient, IDataAccess db)
    {
        _httpClient = httpClient;
        _db = db;
        if (_httpClient.BaseAddress is null) throw new ArgumentNullException(nameof(httpClient.BaseAddress));
    }

    /// <summary>
    /// This method will scrape case status from USCIS's website
    /// </summary>
    /// <param name="receiptsToCheck"></param>
    /// <param name="persistCaseStatus"></param>
    /// <remarks><paramref name="persistCaseStatus"/> requires Azure Cloud Tables setup</remarks>
    /// <returns></returns>
    public async Task<List<ReceiptStatus>> GetReceiptStatusAsync(IEnumerable<Receipt> receiptsToCheck, bool
    persistCaseStatus = false)
    {
        var receiptStatuses = new List<ReceiptStatus>();

        foreach (var receipt in receiptsToCheck)
        {
            var requestContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
            {
                new("appReceiptNum", receipt.ReceiptNumber),
                new("caseStatusSearchBtn", "CHECK STATUS")
            });
            var response = await (await _httpClient.PostAsync(_httpClient.BaseAddress, requestContent)).Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            receiptStatuses.Add(new ReceiptStatus()
            {
                ReceiptNumber = receipt.ReceiptNumber,
                CurrentStatus = doc.QuerySelector(".rows .text-center h1").InnerText,
                CurrentStatusDetails = doc.QuerySelector(".rows .text-center p").InnerText,
                LastChecked = DateTime.UtcNow
            });
        }

        if (persistCaseStatus)
        {
            _db.PersistCases(receiptStatuses);
            return _db.GetCases();
        }

        return receiptStatuses;

    }
}
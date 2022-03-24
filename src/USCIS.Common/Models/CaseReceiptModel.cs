using Microsoft.Azure.Cosmos.Table;

namespace USCIS.Common;

public class ReceiptStatus : Receipt
{
    public string? CurrentStatus { get; set; }
    public string? CurrentStatusDetails { get; set; }
    public bool HasNewStatus { get; set; }
    public DateTime LastChecked { get; set; }

    public string ToStatusString(bool colored = false)
    {
        var statusString = HasNewStatus ? "(NEW STATUS)" : "(UNCHANGED)";
        return colored ? $"[{(HasNewStatus ? "green" : "grey")}]{statusString}[/]" : statusString;
    }
}

public class Receipt
{
    public string ReceiptNumber { get; set; }
}

public class ReceiptEntity : TableEntity
{
    public ReceiptEntity(string receiptNumber)
    {
        PartitionKey = "USCIS";
        RowKey = receiptNumber;
    }

    public ReceiptEntity() {}

    public string? CurrentStatus { get; set; } = "";
    public string? CurrentStatusDetails { get; set; } = "";
    public DateTime LastCheckedOn { get; set; }
    public bool HasNewStatus { get; set; }
}
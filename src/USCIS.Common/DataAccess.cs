using Microsoft.Azure.Cosmos.Table;

namespace USCIS.Common;

public interface IDataAccess
{
    Task<ReceiptStatus> GetCase(string receiptNumber);
    List<ReceiptStatus> GetCases(string? filter = null);
    void PersistCases(IEnumerable<ReceiptStatus> cases);
}

public class DataAccess : IDataAccess
{
    private readonly CloudTable _table;

    public DataAccess(string connectionString, string tableName)
    {
        if (!string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(tableName))
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            _table = tableClient.GetTableReference(tableName);
            _table.CreateIfNotExists();
        }
    }

    private async Task UpsertCase(ReceiptStatus receiptStatus)
    {
        var checkExistingCaseStatus = (await GetCase(receiptStatus.ReceiptNumber)).CurrentStatus;

        TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(
            new ReceiptEntity(receiptStatus.ReceiptNumber)
            {
                CurrentStatus = receiptStatus.CurrentStatus,
                CurrentStatusDetails = receiptStatus.CurrentStatusDetails,
                LastCheckedOn = receiptStatus.LastChecked,
                HasNewStatus = checkExistingCaseStatus != receiptStatus.CurrentStatus
            });

        await _table.ExecuteAsync(insertOrMergeOperation);
    }

    public void PersistCases(IEnumerable<ReceiptStatus> cases)
    {
        try
        {
            cases.ToList().ForEach(p => UpsertCase(p).GetAwaiter().GetResult());
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to persist cases for current run. Error: {ex.Message}");
        }

    }

    public async Task<ReceiptStatus> GetCase(string receiptNumber) {
        TableOperation retrieveOperation = TableOperation.Retrieve<ReceiptEntity>("USCIS", receiptNumber);

        TableResult result = await _table.ExecuteAsync(retrieveOperation);
        ReceiptEntity retrievedCase = result.Result as ReceiptEntity ?? new ReceiptEntity();

        return new ReceiptStatus()
        {
            ReceiptNumber = retrievedCase.RowKey ?? "",
            CurrentStatus = retrievedCase.CurrentStatus ?? "",
            CurrentStatusDetails = retrievedCase.CurrentStatusDetails ?? "",
            LastChecked = retrievedCase.LastCheckedOn,
            HasNewStatus = retrievedCase.HasNewStatus
        };
    }

    public List<ReceiptStatus> GetCases(string? filter = null)
    {
        List<ReceiptStatus> entities = new();

        TableQuery<ReceiptEntity> query = new TableQuery<ReceiptEntity>();

        foreach (ReceiptEntity dEntity in _table.ExecuteQuery(query))
        {
            entities.Add(new ReceiptStatus()
            {
                ReceiptNumber = dEntity.RowKey,
                CurrentStatus = dEntity.CurrentStatus,
                CurrentStatusDetails = dEntity.CurrentStatusDetails,
                LastChecked = dEntity.LastCheckedOn,
                HasNewStatus = dEntity.HasNewStatus
            });
        }

        return entities;
    }

}

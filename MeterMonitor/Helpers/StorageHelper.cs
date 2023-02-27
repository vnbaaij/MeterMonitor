using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using MeterMonitor.Configuration;
using Microsoft.Extensions.Options;

namespace MeterMonitor.Helpers;

public class StorageHelper
{
    private readonly ILogger<StorageHelper> logger;
    private readonly AppConfig config;

    public StorageHelper(ILogger<StorageHelper> logger, IOptions<AppConfig> config)
    {
        this.logger = logger;
        this.config = config.Value;

        this.logger.LogDebug("StorrageConnectionString: {StorageConnectionstring}", this.config.StorageConnectionstring);
    }

    public async Task<TableClient> GetTableAsync(string tablename)
    {

        logger.LogInformation("Checking storage...");

        TableServiceClient serviceClient = new (config.StorageConnectionstring);

        Response<TableItem> result = await serviceClient.CreateTableIfNotExistsAsync(tablename);

        if (result != null)
        {
            logger.LogInformation("table '{table}' created", tablename);
        }
        else
        {
            logger.LogInformation("table '{table}' exists", tablename);
        }

        TableClient table = serviceClient.GetTableClient(tablename);


        return table;
    }
}

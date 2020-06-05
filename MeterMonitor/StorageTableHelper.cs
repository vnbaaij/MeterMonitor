using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MeterMonitor
{

    public class StorageTableHelper
    {

        private string StorageConnectionString { get; set; }

        public StorageTableHelper() { }

        public StorageTableHelper(string storageConnectionString)
        {
            StorageConnectionString = storageConnectionString;
        }


        public async Task<CloudTable> GetTableAsync(string tablename)
        {
            Console.Write("Checking storage...");
            CloudStorageAccount storageAccount = this.CreateStorageAccountFromConnectionString();
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            CloudTable table = tableClient.GetTableReference(tablename);

            if (await table.CreateIfNotExistsAsync())
            {
                Console.WriteLine("table '{0}' created", tablename);
            }
            else
            {
                Console.WriteLine("table '{0}' exists", tablename);
            }

            return table;
        }

        public CloudStorageAccount CreateStorageAccountFromConnectionString()
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid storage account information provided.");
                throw;
            }
            return storageAccount;
        }
    }
}

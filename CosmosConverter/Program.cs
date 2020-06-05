using Azure.Cosmos;
using MeterMonitor;
using DSMRParser;
using DSMRParser.Models;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.SystemFunctions;

namespace CosmosConverter
{
    class Program
    {
        private static CosmosClient _cosmosClient;
        private static CosmosContainer _cosmosContainer;

        private static StorageTableHelper _storageTableHelper;

        private static CloudTable _table;

        static async Task Main(string[] args)
        {
            Console.WriteLine("==> Press Enter to start Cosmos converter...");
            Console.Read();


            ConnectToCosmos();
            ConnectToStorage();

            await Run();
        }

        private static void ConnectToCosmos()
        {
            var endpointUrl = "https://powerdb2.documents.azure.com:443/";
            var authorizationKey = "qRQYHkQKbxfMaLqfkdV1C8C73TNxYvBWA7ejleKikSQ5GTUs7tS8B3O5Nw0RGiDCQdOYhG8qUZqUbKgCFHtjJw==";
            var database = "powerdb";
            var container = "meterdata";

            //Init CosmosDB here
            var options = new CosmosClientOptions()
            {
                Serializer = new CosmosJsonSerializer(),
            };
            _cosmosClient = new CosmosClient(endpointUrl, authorizationKey, options);
            _cosmosContainer = _cosmosClient.GetContainer(database, container);
        }

        private static void ConnectToStorage()
        {
            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=powermonitor;AccountKey=EAnvIcfIS/h7ms6hKMWYaIz+ctp7kHGbkgFbtSx2i4si3/wkFzCy48GhtHzjCloPcjL3+F6LpbQO+0CdenV38g==;EndpointSuffix=core.windows.net";
            _storageTableHelper = new StorageTableHelper(storageConnectionString);

            // Create a table client for interacting with the table service
            _table = _storageTableHelper.GetTableAsync("meterdata202006").Result;
        }

        private static async Task Run()
        {
            await QueryItemsAsync("");

            //await QueryItemsAsync("20191227");
            //await QueryItemsAsync("20191228");
            //await QueryItemsAsync("20191229");
            //await QueryItemsAsync("20191230");
            //await QueryItemsAsync("20191231");

            //string key = "";
            //for (int m = 1; m <= DateTime.Now.Month; m++)
            //{
            //    for (int d = 1; d < DateTime.DaysInMonth(2020, m); d++)
            //    {
            //        key = "2020" + m.ToString("d2") + d.ToString("d2");
            //        await QueryItemsAsync(key);
            //    }
            //}

        }

        /// <summary>
        /// Run a query (using Azure Cosmos DB SQL syntax) against the container
        /// </summary>
        private static async Task QueryItemsAsync(string key)
        {
            TelegramV1 lastComplete = null;
            int counter = 0;
            var sqlQueryText = $"SELECT * FROM c"; // WHERE c.key = '{key}'";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);


            await foreach (TelegramV1 old in _cosmosContainer.GetItemQueryIterator<TelegramV1>(queryDefinition))
            {
                counter++;

                if (!string.IsNullOrEmpty(old.MessageHeader))
                {
                    lastComplete = old;
                }
                else
                {

                    Telegram newTelegram = new Telegram
                    {

                        MeterTimestamp = old.Timestamp,
                        RowKey = old.Timestamp.ToString("yyyyMMddHHmmss"),
                        PartitionKey = old.Timestamp.ToString("dd")
                    };


                    var telegramType = typeof(Telegram);
                    var telegramV1Type = typeof(TelegramV1);
                    try
                    {

                        foreach (var item in telegramType.GetProperties())
                        {
                            var v = item.Name switch
                            {
                                "MeterTimestamp" => newTelegram.MeterTimestamp,
                                "PartitionKey" => newTelegram.PartitionKey,
                                "RowKey" => old.Id,
                                "Timestamp" => null,
                                "ETag" => null,
                                "GasUsage" => null,
                                "GasTimestamp" => null,
                                _ => telegramV1Type.GetProperty(item.Name).GetValue(old) ?? telegramV1Type.GetProperty(item.Name).GetValue(lastComplete),
                            };

                            var x = telegramType.GetProperty(item.Name).PropertyType.Name;

                            if (v != null)
                            {
                                if (x == "Double" && Type.GetTypeCode(v.GetType()) == TypeCode.Double)
                                {
                                    if ((double)v == 0 && (double)telegramV1Type.GetProperty(item.Name).GetValue(lastComplete) > 0)
                                        v = telegramV1Type.GetProperty(item.Name).GetValue(lastComplete);
                                }
                                if (x == "Int32" && Type.GetTypeCode(v.GetType()) == TypeCode.Int32)
                                {
                                    if ((int)v == 0 && (int)telegramV1Type.GetProperty(item.Name).GetValue(lastComplete) > 0)
                                        v = telegramV1Type.GetProperty(item.Name).GetValue(lastComplete);
                                }
                            }
                            telegramType.GetProperty(item.Name).SetValue(newTelegram, v);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }

                    //newTelegram.Dump();

                    //New telegram is ready, let's put it in storage
                    await StoreTelegram(newTelegram);

                    if (counter % 100 == 0)
                        Console.WriteLine($"{counter} records updated");
                }
            }
            Console.WriteLine($"{key}: {counter} results");
        }

        private static async Task StoreTelegram(Telegram telegram)
        {
            string tablename = telegram.GetTablename("meterdata");

            if (_table?.Name != tablename)
                _table = await _storageTableHelper.GetTableAsync(tablename);

            try
            {
                // Create the InsertOrReplace table operation
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(telegram);

                // Execute the operation.
                TableResult result = await _table.ExecuteAsync(insertOrMergeOperation);
                if (result.HttpStatusCode != (int)HttpStatusCode.NoContent)
                    Console.WriteLine($"Error with storing telegram {telegram.RowKey} / {tablename}");

                return;
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                throw;
            };
        }


    }
}

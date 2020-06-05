using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSMRParser;
using DSMRParser.Models;
using MeterMonitor.Configuration;
using MeterMonitor.Reader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Cosmos;
using System.Net;
using Microsoft.Azure.Cosmos.Table;

namespace MeterMonitor
{
    public class MeterWorker : BackgroundService
    {
        private readonly ILogger<MeterWorker> _logger;
        private readonly IMeterReader _meterReader;
        public readonly ConfigSettings _config;
        private readonly StorageTableHelper _storageTableHelper;
        private CloudTable _table;

        //private CosmosClient _cosmosClient;
        //private CosmosContainer _cosmosContainer;

        private Telegram _previous;
        private Telegram _telegram;
        private Telegram _delta;


        public MeterWorker(ILogger<MeterWorker> logger, IMeterReader meterReader, IOptions<ConfigSettings> config)
        {
            _logger = logger;
            _meterReader = meterReader;
            _config = config.Value;

            // Retrieve storage account information from connection string.
            _storageTableHelper = new StorageTableHelper(_config.StorageConnectionstring);
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"MeterMontor started at: {DateTimeOffset.Now}");


            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            FileInfo[] files = null;
            int counter = 0;


            _logger.LogInformation($"MeterMonitor executing at: {DateTimeOffset.Now}" );

            GetFilesList(ref files);

            while (!stoppingToken.IsCancellationRequested)
            {
                SetSourceToFile(files, ref counter);

                _telegram = await _meterReader.ReadAsStreamAsync();

                _logger.LogInformation($"Extracted data from {_meterReader.Source}\n{_telegram.MeterTimestamp}\nConsumption (low/high):\t{_telegram.PowerConsumptionTariff1}/{_telegram.PowerConsumptionTariff2}\nProduction (low/high):\t{_telegram.PowerProductionTariff1}/{_telegram.PowerProductionTariff2}\n");
                _logger.LogInformation($"Calculated CRC: {_telegram.ComputeChecksum()}");

                if (_telegram.ComputeChecksum() != _telegram.CRC)
                {
                    _logger.LogError("Telegram not extracted correctly. Calculated CRC not equal to stored CRC ");
                }

                //GetDifferences();
                SaveDataFile();

                //if (counter % 60 == 0)
                    SaveDataJson(_telegram);
                //else
                //    SaveDataJson(_delta);

                //counter++;

                await Task.Delay(_config.ReadInterval, stoppingToken);
            }
        }

        private void GetDifferences()
        {
            if (_previous != null)
            {
                _delta = null;

                //var comparer = new ObjectsComparer.Comparer<Telegram>();
                //comparer.IgnoreMember("Lines");

                //var isEqual = comparer.Compare(_previous, _telegram, out IEnumerable<Difference> differences);
                //var props = differences.Aggregate(string.Empty, (a, next) => $"{ a }\r\n\t{ next.MemberPath } {next.Value1}  { next.Value2 }");

                //_logger.LogInformation($"There were { differences.Count() } variances on these properties: { props }");

                _delta = _telegram.GetDelta(_previous);
                //_delta.Key = _delta.Id.Substring(0, 8);
                _delta.PartitionKey = _telegram.PartitionKey;
            }


            _previous = _telegram;
        }

        private void SaveDataFile()
        {
            if (_config.SaveDataFiles)
            {
                IWriteFile fileWriter = new FileWriter();

                fileWriter.WithPath(_config.DataFilesPath)
                    //.WithFilename(_telegram.Id + ".dsmrdata")
                    .WithFilename(_telegram.RowKey + ".dsmrdata")
                    .WithContents(_telegram.ToString())
                    .Write();
            }
        }

        private async void SaveDataJson(Telegram telegram)
        {
            if (telegram == null)
            {
                throw new ArgumentNullException("telegram");
            }

            //telegram.Dump();

            // Create or reference an existing table
            string tablename = telegram.GetTablename(_config.TablenamePrefix);

            if (_table?.Name != tablename)
                _table = await _storageTableHelper.GetTableAsync(tablename);

            try
            {
                // Create the InsertOrReplace table operation
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(telegram);

                // Execute the operation.
                TableResult result = await _table.ExecuteAsync(insertOrMergeOperation);
                if (result.HttpStatusCode == (int)HttpStatusCode.NoContent)
                    Console.WriteLine($"Telegram {telegram.RowKey} stored in table {tablename}");

                return;
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                //throw;
            }
        }

        private void SetSourceToFile(FileInfo[] files, ref int counter)
        {
            if (_meterReader is FileReader)
            {
                if (counter >= files.Length)
                    counter = 0;

                _meterReader.Source = files[counter].FullName;
            }
        }

        private void GetFilesList(ref FileInfo[] files)
        {
            if (_meterReader is FileReader)
            {

                DirectoryInfo di = new DirectoryInfo(_config.DataFilesPath);
                files = di.GetFiles("*.dsmrdata");
            }
        }
    }
}

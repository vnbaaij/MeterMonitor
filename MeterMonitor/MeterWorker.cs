using DSMRParser;
using DSMRParser.Models;
using MeterMonitor.Configuration;
using MeterMonitor.Reader;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MeterMonitor
{
    public class MeterWorker : BackgroundService
    {
        private readonly ILogger<MeterWorker> _logger;
        private readonly IMeterReader _meterReader;
        public readonly ConfigSettings _config;
        private readonly StorageTableHelper _storageTableHelper;
        private CloudTable _table;
        private Telegram _telegram;

        public MeterWorker(ILogger<MeterWorker> logger, IMeterReader meterReader, IOptions<ConfigSettings> config)
        {
            _logger = logger;
            _meterReader = meterReader;
            _config = config.Value;

            // Retrieve storage account information from connection string.
            _storageTableHelper = new StorageTableHelper(_config.StorageConnectionstring);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            FileInfo[] files = null;
            int counter = 0;


            _logger.LogInformation($"MeterMonitor executing at: {DateTimeOffset.Now}" );

            GetFilesList(ref files);

            while (!stoppingToken.IsCancellationRequested)
            {
                SetSourceToFiles(files, ref counter);

                _telegram = await _meterReader.ReadAsStreamAsync();

                _logger.LogInformation($"Extracted data from {_meterReader.Source}\n{_telegram.MeterTimestamp}\nConsumption (low/high):\t{_telegram.PowerConsumptionTariff1}/{_telegram.PowerConsumptionTariff2}\nProduction (low/high):\t{_telegram.PowerProductionTariff1}/{_telegram.PowerProductionTariff2}\n");
                _logger.LogInformation($"Calculated CRC: {_telegram.ComputeChecksum()}");

                if (_telegram.ComputeChecksum() != _telegram.CRC)
                {
                    _logger.LogError("Telegram not extracted correctly. Calculated CRC not equal to stored CRC ");
                }

                SaveDataToFile();
                SaveDataToStorageTable();

                await Task.Delay(_config.ReadInterval, stoppingToken);
            }
        }

        private void SaveDataToFile()
        {
            if (_config.SaveDataFiles)
            {
                IWriteFile fileWriter = new FileWriter();

                fileWriter.WithPath(_config.DataFilesPath)
                    .WithFilename($"{_telegram.RowKey}.dsmrdata")
                    .WithContents(_telegram.ToString())
                    .Write();
            }
        }

        private async void SaveDataToStorageTable()
        {

            //_telegram.Dump();

            // Create or reference an existing table
            string tablename = _telegram.GetTablename(_config.TablenamePrefix);

            if (_table?.Name != tablename)
                _table = await _storageTableHelper.GetTableAsync(tablename);

            try
            {
                // Create the InsertOrReplace table operation
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(_telegram);

                // Execute the operation.
                TableResult result = await _table.ExecuteAsync(insertOrMergeOperation);
                if (result.HttpStatusCode == (int)HttpStatusCode.NoContent)
                    Console.WriteLine($"Telegram {_telegram.RowKey} stored in table {tablename}");

                return;
            }
            catch (StorageException e)
            {
                Console.WriteLine($"Error when saving telegram {_telegram.RowKey}: {e.Message}");
            }
        }

        private void SetSourceToFiles(FileInfo[] files, ref int counter)
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

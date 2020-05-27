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

namespace MeterMonitor
{
    public class MeterWorker : BackgroundService
    {
        private readonly ILogger<MeterWorker> _logger;
        private readonly IMeterReader _meterReader;
        public readonly ConfigSettings _config;
        private CosmosClient _cosmosClient;
        private CosmosContainer _cosmosContainer;

        private Telegram _previous;
        private Telegram _telegram;
        private Telegram _delta;

        public MeterWorker(ILogger<MeterWorker> logger, IMeterReader meterReader, IOptions<ConfigSettings> config)
        {
            _logger = logger;
            _meterReader = meterReader;
            _config = config.Value;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"MeterMontor started at: {DateTimeOffset.Now}");

            var endpointUrl = _config.CosmosDBSettings.EndpointUrl;
            var authorizationKey = _config.CosmosDBSettings.AuthorizationKey;
            var database = _config.CosmosDBSettings.Database;
            var container = _config.CosmosDBSettings.Container;

            //Init CosmosDB here

            var options = new CosmosClientOptions()
            {
                Serializer = new CosmosJsonSerializer(),
            };
            _cosmosClient = new CosmosClient(endpointUrl, authorizationKey,options);
            _ = await _cosmosClient.CreateDatabaseIfNotExistsAsync(database);
            _ = await _cosmosClient.GetDatabase(database).CreateContainerIfNotExistsAsync(container, "/key");

            _cosmosContainer = _cosmosClient.GetContainer(_config.CosmosDBSettings.Database, _config.CosmosDBSettings.Container);

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

                _logger.LogInformation($"Extracted data from {_meterReader.Source}\n{_telegram.Timestamp}\nConsumption (low/high):\t{_telegram.PowerConsumptionTariff1}/{_telegram.PowerConsumptionTariff2}\nProduction (low/high):\t{_telegram.PowerProductionTariff1}/{_telegram.PowerProductionTariff2}\n");
                _logger.LogInformation($"Calculated CRC: {_telegram.ComputeChecksum()}");

                if (_telegram.ComputeChecksum() != _telegram.CRC)
                {
                    _logger.LogError("Telegram not extracted correctly. Calculated CRC not equal to stored CRC ");
                }

                GetDifferences();
                SaveDataFile();

                if (counter % 60 == 0)
                    SaveDataJson(_telegram);
                else
                    SaveDataJson(_delta);

                counter++;

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
                _delta.Key = _delta.Id.Substring(0, 8);
            }


            _previous = _telegram;
        }

        private void SaveDataFile()
        {
            if (_config.SaveDataFiles)
            {
                IWriteFile fileWriter = new FileWriter();

                fileWriter.WithPath(_config.DataFilesPath)
                    .WithFilename(_telegram.Id + ".dsmrdata")
                    .WithContents(_telegram.ToString())
                    .Write();
            }
        }

        private async void SaveDataJson(Telegram data)
        {

            //var options = new JsonSerializerOptions
            //{
            //    IgnoreNullValues = true,
            //    WriteIndented = true,
            //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase

            //};

            //var json = JsonSerializer.Serialize(data,options);
            //_logger.LogInformation($"json: {json}");


            //try
            //{
            //    ItemResponse<Telegram> cosmosResponse = await _cosmosContainer.ReadItemAsync<Telegram>(data.Id, new PartitionKey(data.Key));
            //    _logger.LogInformation("Item in database with id: {0} already exists\n", cosmosResponse.Value.Id);
            //}
            //catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            //{
                ItemResponse<Telegram> cosmosResponse = await _cosmosContainer.CreateItemAsync<Telegram>(data, new PartitionKey(data.Key));
                _logger.LogInformation("Created item in database with id: {0}\n", cosmosResponse.Value.Id);

            //}
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

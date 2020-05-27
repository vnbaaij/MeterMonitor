using System;
using System.IO.Ports;

namespace MeterMonitor.Configuration
{
    public class SerialSettings
    {
        public string Port { get; set; }
        public int Baud { get; set; }
        public int DataBits { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }
    }

    public class CosmosDBSettings
    {
        public string EndpointUrl { get; set; }
        public string AuthorizationKey { get; set; }
        public string Database { get; set; }
        public string Container { get; set; }
    }

    public class ConfigSettings
    {
        public SerialSettings SerialSettings { get; set; }
        public TimeSpan ReadInterval { get; set; }
        public string DataFilesPath { get; set; }
        public bool SaveDataFiles { get; set; }
        public CosmosDBSettings CosmosDBSettings { get; set; }
    }
}


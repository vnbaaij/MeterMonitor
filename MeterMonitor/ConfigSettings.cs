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

    public class ConfigSettings
    {
        public SerialSettings SerialSettings { get; set; }
        public TimeSpan ReadInterval { get; set; }
        public string DataFilesPath { get; set; }
        public bool SaveDataFiles { get; set; }
        public string StorageConnectionstring { get; set; }

        public string TablenamePrefix { get; set; }
    }
}


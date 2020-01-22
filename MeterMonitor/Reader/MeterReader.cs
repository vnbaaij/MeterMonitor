using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using DSMRParser;
using DSMRParser.Models;
using MeterMonitor.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeterMonitor.Reader
{
    public class MeterReader : IMeterReader
    {
        private readonly ILogger<MeterReader> _logger;
        private readonly ConfigSettings _config;

        private readonly SerialPort serialPort;
        public string Source { get; set; } = "Serial Port";

        public MeterReader(ILogger<MeterReader> logger, IOptions<ConfigSettings> config)
        {
            _logger = logger;
            _config = config.Value;

            serialPort = CreateSerialPortStream();
        }

        public async Task<string> ReadAsStringAsync()
        {
            serialPort.Open();
            using StreamReader streamReader = new StreamReader(serialPort.BaseStream);
            return await GetStringFromReaderAsync(streamReader);

        }

        public async Task<Telegram> ReadAsStreamAsync()
        {
            serialPort.Open();
            using StreamReader streamReader = new StreamReader(serialPort.BaseStream);
            return await GetStreamFromReaderAsync(streamReader);
        }

        private static async Task<string> GetStringFromReaderAsync(StreamReader streamReader)
        {
            StringBuilder stringBuilder = new StringBuilder();

            string line = string.Empty;

            while (!line.StartsWith('!'))
            {
                line = await streamReader.ReadLineAsync();
                stringBuilder.AppendLine(line);
            }
            return stringBuilder.ToString();

        }

        private async Task<Telegram> GetStreamFromReaderAsync(StreamReader streamReader)
        {
            Parser parser = new Parser();

            Telegram telegram = new Telegram();
            await parser.ParseFromStreamReader(streamReader, (object sender, Telegram output) =>
            {
                telegram = output;
            });
            return telegram;
        }

        private SerialPort CreateSerialPortStream()
        {
            var serialSettings = _config.SerialSettings;
            string port = serialSettings.Port;
            int baud = serialSettings.Baud;
            int dataBits = serialSettings.DataBits;
            Parity parity = serialSettings.Parity;
            StopBits stopBits = serialSettings.StopBits;

            return new SerialPort(port, baud, parity, dataBits, stopBits);
        }
    }
}
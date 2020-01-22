using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DSMRParser;
using DSMRParser.Models;
using Microsoft.Extensions.Logging;

namespace MeterMonitor.Reader
{
    public class FileReader : IMeterReader
    {
        private readonly ILogger<MeterReader> _logger;

        public string Source { get; set; }

        public FileReader(ILogger<MeterReader> logger)
        {
            _logger = logger;
        }

        public async Task<string> ReadAsStringAsync()
        {
            // Read the file as one string.
            string message = await File.ReadAllTextAsync(Source);

            // Display the file contents to the console.
            //_logger.LogInformation("Contents of WriteText.txt = {0}", message);

            return message;
        }

        public async Task<Telegram> ReadAsStreamAsync()
        {
            // Read the file as one string.
            string message = await File.ReadAllTextAsync(Source);

            // Display the file contents to the console.
            //_logger.LogInformation("Contents of WriteText.txt = {0}", message);

            Parser parser = new Parser();

            return await parser.ParseFromString(message);
        }
    }
}
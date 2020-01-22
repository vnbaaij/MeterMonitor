using System.Threading.Tasks;
using DSMRParser.Models;

namespace MeterMonitor.Reader
{
    public interface IMeterReader
    {

        public string Source { get; set; }
        public Task<string> ReadAsStringAsync();
        public Task<Telegram> ReadAsStreamAsync();
    }
}
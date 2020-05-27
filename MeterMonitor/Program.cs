using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MeterMonitor.Reader;
using MeterMonitor.Configuration;

namespace MeterMonitor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();

                    services.Configure<ConfigSettings>(hostContext.Configuration.GetSection("Configuration"));
                    services.AddTransient<IMeterReader, MeterReader>();
                    services.AddHostedService<MeterWorker>();
                });
    }
}

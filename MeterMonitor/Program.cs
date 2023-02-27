using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MeterMonitor.Reader;
using MeterMonitor.Configuration;
using System;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
//using MeterMonitor.Helpers;

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
                .UseSystemd()
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    if (hostContext.HostingEnvironment.IsProduction())
                    {
                        var builtConfig = config.Build();

                        using var store = new X509Store(StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadOnly);
                        
                        X509Certificate2Collection certs = store.Certificates
                        .Find(X509FindType.FindByThumbprint,
                            builtConfig["AzureADCertThumbprint"], false);

                        config.AddAzureKeyVault(new Uri($"https://{builtConfig["KEYVAULTNAME"]}.vault.azure.net/"),
                                                new ClientCertificateCredential(builtConfig["AZUREADDIRECTORYID"], builtConfig["AZUREADAPPLICATIONID"], certs.OfType<X509Certificate2>().Single()),
                                                new KeyVaultSecretManager());

                        store.Close();
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<ConfigSettings>(hostContext.Configuration.GetSection("Configuration"));
                    //services.AddSingleton<StorageHelper>();
                    services.AddTransient<IMeterReader, MeterReader>();
                    services.AddHostedService<MeterWorker>();
                });
    }
}

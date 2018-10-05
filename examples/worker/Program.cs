using System;
using System.Threading.Tasks;

using common;
using common.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace worker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Start application");
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables();

                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddOptions();
                    services.Configure<ConnectionStrings>(context.Configuration.GetSection("ConnectionStrings"));

                    services.AddSingleton<IEventRepository, EventRepository>();
                })
                .ConfigureLogging((context, logging) => {
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                });
            await builder.RunConsoleAsync();
        }
    }
}

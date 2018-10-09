using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using common;
using common.Models;

using murmur;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RingHash;


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
                    services.Configure<Node>(context.Configuration.GetSection("Worker"));

                    services.AddSingleton<IConsistentHash<Node>>(new ConsistentHash<Node>(new Murmur32(), 1));
                    services.AddSingleton<IEventRepository, EventRepository>();
                    services.AddSingleton<IWorkerRepository, WorkerRepository>();
                    services.AddSingleton<IEventThreadRepository, EventThreadRepository>();
                    services.AddHostedService<WorkerService>();
                })
                .ConfigureLogging((context, logging) => {
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                });
            await builder.RunConsoleAsync();
        }
    }
}

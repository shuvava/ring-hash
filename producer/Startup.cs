using System.IO;

using common;
using common.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace producer
{
    public class Startup
    {

        public ServiceProvider ServiceProvider { get; }
        public IConfiguration Configuration { get; }

        public Startup(string[] args)
        {
            if (args == null)
            {
                args = new string[0];
            }
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            //setup our DI
            var serviceProvider = new ServiceCollection();
            ConfigureServices(serviceProvider);


            ServiceProvider = serviceProvider.BuildServiceProvider();

            Configure();
        }


        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddLogging(builder =>
                {
                    builder
                        .AddConfiguration(Configuration.GetSection("Logging"))
                        .AddConsole();
                })
                .AddOptions();
            // register config
            services.Configure<ConnectionStrings>(Configuration.GetSection("ConnectionStrings"));
            // register repositories
            services.AddSingleton<IEventRepository, EventRepository>();
            services.AddSingleton<ICommand, Command>();
        }


        public void Configure()
        {
            //ServiceProvider
            //    .GetService<ILoggerFactory>()
            //    .AddConsole(LogLevel.Debug);
        }
    }
}

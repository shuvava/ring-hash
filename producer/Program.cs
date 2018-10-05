using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace producer
{
    internal class Program
    {
        private static ILogger _logger;
        private static Startup _config;


        private static async Task Main(string[] args)
        {
            Console.WriteLine("Start application");
            _config = new Startup(args);
            _logger = _config.ServiceProvider.GetRequiredService<ILogger<Program>>();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            _logger.LogInformation(_config.Configuration.GetConnectionString("DefaultConnection"));
            Console.ReadKey();
        }


        private static void CurrentDomainOnUnhandledException(object sender,
            UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            var exception = unhandledExceptionEventArgs.ExceptionObject as Exception;
            _logger.LogError(exception, $"Unhandled Exception: sender = {sender}");
        }
    }
}

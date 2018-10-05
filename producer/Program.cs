using System;
using System.Threading.Tasks;

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

            var srv = _config.ServiceProvider.GetRequiredService<ICommand>();
            var count = 10;

            while (count > 0)
            {
                var excResult = await srv.Run(count).ConfigureAwait(false);
                Console.WriteLine($"{excResult} records added");
                Console.WriteLine("Do you want add more?");
                var result = Console.ReadLine();

                if (string.IsNullOrEmpty(result))
                {
                    continue;
                }

                if (!int.TryParse(result, out count))
                {
                    count = 0;
                }
            }
        }


        private static void CurrentDomainOnUnhandledException(object sender,
            UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            var exception = unhandledExceptionEventArgs.ExceptionObject as Exception;
            _logger.LogError(exception, $"Unhandled Exception: sender = {sender}");
        }
    }
}

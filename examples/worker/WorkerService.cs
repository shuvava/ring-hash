using System;
using System.Threading;
using System.Threading.Tasks;

using common.Models;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace worker
{
    public class WorkerService: IHostedService, IDisposable
    {
        private CancellationTokenSource _tokenSource;
        private readonly ILogger _logger;
        private Timer _timer;
        private readonly Node _settings;

        public WorkerService(ILogger<WorkerService> logger ,IOptions<Node> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is starting.");
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _timer = new Timer(async _ => await DoWork().ConfigureAwait(false), null, TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private async Task DoWork()
        {
            if (_tokenSource.IsCancellationRequested)
            {
                await StopAsync(_tokenSource.Token);
                return;
            }
            _logger.LogInformation($"Timed Background Service is working. {_settings.Description}");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}

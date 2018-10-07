using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using common;
using common.Models;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RingHash;


namespace worker
{
    public class WorkerService: IHostedService, IDisposable
    {
        private readonly IConsistentHash<Node> _hash;
        private CancellationTokenSource _tokenSource;
        private readonly ILogger _logger;
        private readonly IEventRepository _eventRepository;
        private readonly IWorkerRepository _workerRepository;
        private Timer _timerThreadLocker;
        private Timer _timer;
        private readonly Node _settings;

        public WorkerService(
            IConsistentHash<Node> hash,
            IEventRepository eventRepository,
            IWorkerRepository workerRepository,
            ILogger<WorkerService> logger,
            IOptions<Node> settings)
        {
            _hash = hash;
            _eventRepository = eventRepository;
            _workerRepository = workerRepository;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is starting.");
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await _workerRepository.PutWorkerAsync(_settings).ConfigureAwait(false);
            _timerThreadLocker = new Timer(async _ => await SetLock().ConfigureAwait(false), null, TimeSpan.Zero,
                TimeSpan.FromSeconds(60));
            _timer = new Timer(async _ => await DoWork().ConfigureAwait(false), null, TimeSpan.Zero,
                TimeSpan.FromSeconds(60));
        }


        private async Task DoWork()
        {
            _logger.LogInformation("Timed Background Service is working.");
        }

        private async Task SetLock()
        {
            if (_tokenSource.IsCancellationRequested)
            {
                await StopAsync(_tokenSource.Token);
                return;
            }

            await _workerRepository.CheckpointAsync(_settings).ConfigureAwait(false);
            var workers = await _workerRepository.GetWorkersAsync().ConfigureAwait(false);
            var removedWorkers =  _hash.GetNodes().Where(w => workers.All(a => a.Id != w.Id));

            foreach (var worker in removedWorkers)
            {
                _logger.LogWarning($"Remove worker from hash {worker} {worker.Description}");
                _hash.RemoveNode(worker);
            }

            foreach (var worker in workers)
            {
                _logger.LogInformation($"Add worker to hash {worker} {worker.Description}");
                _hash.AddNode(worker);
            }
            _logger.LogInformation($"Lock {_settings.Id} {_settings.Description}");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping.");

            _timerThreadLocker?.Change(Timeout.Infinite, 0);
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timerThreadLocker?.Dispose();
            _timer?.Dispose();
        }
    }
}

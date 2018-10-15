using System;
using System.Threading;
using System.Threading.Tasks;

using common;
using common.Extensions;
using common.Models;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RingHash;


namespace worker
{
    public class TimerWorkerService : WorkerService, IHostedService, IDisposable
    {
        private const int MaxDop = 1;
        private int _looker;
        private Timer _timer;
        private Timer _timerThreadLocker;
        private CancellationTokenSource _tokenSource;


        public TimerWorkerService(
            IConsistentHash<Node> hash,
            IEventThreadRepository eventThreadRepository,
            IEventRepository eventRepository,
            IWorkerRepository workerRepository,
            ILogger<TimerWorkerService> logger,
            IOptions<Node> settings) : base(hash, eventThreadRepository, eventRepository, workerRepository, logger,
            settings)
        {
        }


        public void Dispose()
        {
            _timerThreadLocker?.Dispose();
            _timer?.Dispose();
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Timed Background Service is starting.");
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await InitWorker().ConfigureAwait(false);

            _timerThreadLocker = new Timer(async _ => await SetLock().ConfigureAwait(false), null, TimeSpan.Zero,
                TimeSpan.FromSeconds(60));

            _timer = new Timer(async _ => await DoWork().ConfigureAwait(false), null, TimeSpan.Zero,
                TimeSpan.FromSeconds(60));
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Timed Background Service is stopping.");

            _timerThreadLocker?.Change(Timeout.Infinite, 0);
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }


        private async Task DoWork()
        {
            try
            {
                if (_looker == 1)
                {
                    return;
                }

                Interlocked.Exchange(ref _looker, 1);

                Logger.LogInformation("Timed Background Service is working.");
                await NodeHashes.ForEachAsync(MaxDop, ProcessDataAsync).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unknown exception");
            }

            Interlocked.Exchange(ref _looker, 0);
        }


        private async Task SetLock()
        {
            if (_tokenSource.IsCancellationRequested)
            {
                await StopAsync(_tokenSource.Token);

                return;
            }

            await CheckpointAsync().ConfigureAwait(false);
        }
    }
}

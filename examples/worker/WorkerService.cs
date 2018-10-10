using System;
using System.Collections.Generic;
using System.Linq;
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
    public class WorkerService: IHostedService, IDisposable
    {
        private const int Universe = 1024;
        private const int MaxDop = 1;
        private readonly IConsistentHash<Node> _hash;
        private CancellationTokenSource _tokenSource;
        private readonly ILogger _logger;
        private readonly IEventRepository _eventRepository;
        private readonly IEventThreadRepository _eventThreadRepository;
        private readonly IWorkerRepository _workerRepository;
        private Timer _timerThreadLocker;
        private Timer _timer;
        private readonly Node _settings;
        private IList<int> _nodeHashes;
        private int _looker;

        public WorkerService(
            IConsistentHash<Node> hash,
            IEventThreadRepository eventThreadRepository,
            IEventRepository eventRepository,
            IWorkerRepository workerRepository,
            ILogger<WorkerService> logger,
            IOptions<Node> settings)
        {
            _hash = hash;
            _eventThreadRepository = eventThreadRepository;
            _eventRepository = eventRepository;
            _workerRepository = workerRepository;
            _logger = logger;
            _settings = settings.Value;
            _nodeHashes = new List<int>();
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
            try
            {
                if (_looker == 1)
                {
                    return;
                }

                Interlocked.Exchange(ref _looker, 1);

                _logger.LogInformation("Timed Background Service is working.");
                await _nodeHashes.ForEachAsync(MaxDop, ProcessData).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown exception");
            }
            Interlocked.Exchange(ref _looker, 0);

        }


        private async Task ProcessData(int filter)
        {
            var thread = await _eventThreadRepository.GetThreadForHashAsync(filter).ConfigureAwait(false) ?? new EventThread
            {
                Hash = filter,
                ThreadCheckpoint = DateTime.UtcNow.AddYears(-2),
                WorkerId = _settings.Id
            };

            if (thread.WorkerId != _settings.Id)
            {
                if (await _eventThreadRepository.ChangeThreadOwnerAsync(filter, thread.WorkerId, _settings.Id))
                {
                    thread.WorkerId = _settings.Id;
                }
                else
                {
                    _logger.LogError($"Unable change owner of hash {filter} from {thread.WorkerId} to {_settings.Id}");
                }
            }

            var events = await _eventRepository.GetItemsAsync(thread.ThreadCheckpoint, filter).ConfigureAwait(false);

            if (!events.Any())
            {
                return;
            }
            var checkpoint = events.Max(s => s.CreateTime);
            var maxEventTime = events.Max(s => s.EventTime);
            thread.WorkerId = _settings.Id;
            thread.ThreadCheckpoint = checkpoint;
            _logger.LogInformation($"Worker {thread.WorkerId}; Filter {thread.Hash}; checkpoint {thread.ThreadCheckpoint:G}; records processed: {events.Count()}; max EventTime: {maxEventTime}");
            var result = await _eventThreadRepository.CheckpointAsync(thread).ConfigureAwait(false);

            if (!result)
            {
                _logger.LogError($"Unable update checkpoint of hash {filter} for {thread.WorkerId}");
            }

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
            var changed = false;
            foreach (var worker in removedWorkers)
            {
                changed = true;
                _logger.LogWarning($"Remove worker from hash {worker} {worker.Description}");
                _hash.RemoveNode(worker);
            }

            foreach (var worker in workers)
            {
                _logger.LogInformation($"Add worker to hash {worker} {worker.Description}");

                if (_hash.AddNode(worker))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                var nodeHashes = new List<int>();
                foreach (var i in Enumerable.Range(0, Universe))
                {
                    var node = _hash.GetShardForKey(i.ToString());

                    if (node.Id == _settings.Id)
                    {
                        nodeHashes.Add(i);
                    }
                }

                _nodeHashes = nodeHashes;
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

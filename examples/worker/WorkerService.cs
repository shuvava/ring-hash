using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using common;
using common.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RingHash;


namespace worker
{
    public abstract class WorkerService
    {
        private const int LockTime = 3 * 90;
        private const int Universe = 1024;
        private readonly IEventRepository _eventRepository;
        private readonly IEventThreadRepository _eventThreadRepository;
        private readonly IConsistentHash<Node> _hash;
        protected readonly ILogger Logger;
        private readonly Node _settings;
        private readonly IWorkerRepository _workerRepository;


        public WorkerService(
            IConsistentHash<Node> hash,
            IEventThreadRepository eventThreadRepository,
            IEventRepository eventRepository,
            IWorkerRepository workerRepository,
            ILogger logger,
            IOptions<Node> settings)
        {
            _hash = hash;
            _eventThreadRepository = eventThreadRepository;
            _eventRepository = eventRepository;
            _workerRepository = workerRepository;
            Logger = logger;
            _settings = settings.Value;
            NodeHashes = new List<int>();
        }


        public IReadOnlyList<int> NodeHashes { get; private set; }


        public Task InitWorker()
        {
            _settings.LockExpirationTime = DateTime.UtcNow.AddSeconds(30);
            return _workerRepository.PutWorkerAsync(_settings);
        }


        public async Task ProcessDataAsync(int filter)
        {
            var thread = await _eventThreadRepository.GetThreadForHashAsync(filter).ConfigureAwait(false) ??
                         new EventThread
                         {
                             Hash = filter,
                             Checkpoint = DateTime.UtcNow.AddYears(-2),
                             WorkerId = _settings.Id
                         };

            if (thread.WorkerId != _settings.Id)
            {
                if (thread.LockExpirationTime > DateTime.UtcNow)
                {
                    return;
                }
                if (await _eventThreadRepository.ChangeThreadOwnerAsync(filter, thread.WorkerId, _settings.Id, DateTime.UtcNow.AddSeconds(LockTime)))
                {
                    thread.WorkerId = _settings.Id;
                }
                else
                {
                    Logger.LogWarning($"Unable change owner of hash {filter} from {thread.WorkerId} to {_settings.Id}");

                    return;
                }
            }

            var events = await _eventRepository.GetItemsAsync(thread.Checkpoint, filter).ConfigureAwait(false);

            if (!events.Any())
            {
                return;
            }

            var checkpoint = events.Max(s => s.CreateTime);
            var maxEventTime = events.Max(s => s.EventTime);
            thread.WorkerId = _settings.Id;
            thread.Checkpoint = checkpoint;
            thread.LockExpirationTime = DateTime.UtcNow.AddSeconds(LockTime);

            Logger.LogInformation(
                $"Worker {thread.WorkerId}; Filter {thread.Hash}; checkpoint {thread.Checkpoint:G}; records processed: {events.Count()}; max EventTime: {maxEventTime}");
            var result = await _eventThreadRepository.CheckpointAsync(thread).ConfigureAwait(false);

            if (!result)
            {
                Logger.LogError($"Unable update checkpoint of hash {filter} for {thread.WorkerId}");
            }
        }


        public async Task CheckpointAsync()
        {
            _settings.LockExpirationTime = DateTime.UtcNow.AddSeconds(LockTime);
            await _workerRepository.CheckpointAsync(_settings).ConfigureAwait(false);
            var workers = await _workerRepository.GetWorkersAsync().ConfigureAwait(false);
            var removedWorkers = _hash.GetNodes().Where(w => workers.All(a => a.Id != w.Id));
            var changed = false;

            foreach (var worker in removedWorkers)
            {
                if (_hash.RemoveNode(worker))
                {
                    changed = true;
                    Logger.LogWarning($"Remove worker from hash {worker} {worker.Description}");
                }
            }

            foreach (var worker in workers)
            {
                if (_hash.AddNode(worker))
                {
                    Logger.LogInformation($"Add worker to hash {worker} {worker.Description}");
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

                NodeHashes = nodeHashes;
            }


            Logger.LogInformation($"Lock {_settings.Id} {_settings.Description}");
        }
    }
}

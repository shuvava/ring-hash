using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    public class ReactiveWorkerService : WorkerService, IHostedService, IDisposable
    {
        private int _looker;
        private const int MaxDop = 1;
        private readonly NewThreadScheduler _scheduler;
        private readonly BehaviorSubject<IEnumerable<int>> _subject;
        private long _counter;
        private CancellationTokenSource _tokenSource;
        private bool _mainSubscriptionCompleted;
        private bool _processingSubscriptionCompleted;


        public ReactiveWorkerService(IConsistentHash<Node> hash, IEventThreadRepository eventThreadRepository,
            IEventRepository eventRepository, IWorkerRepository workerRepository, ILogger<ReactiveWorkerService> logger,
            IOptions<Node> settings) : base(hash, eventThreadRepository, eventRepository, workerRepository, logger,
            settings)
        {
            _scheduler = new NewThreadScheduler(ts => new Thread(ts) {Name = "Emitter scheduler"});
            _subject = new BehaviorSubject<IEnumerable<int>>(Enumerable.Empty<int>());
        }


        public void Dispose()
        {
            _tokenSource?.Dispose();
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await InitWorker().ConfigureAwait(false);

            Observable
                .Generate(
                    _counter,
                    x => !_tokenSource.IsCancellationRequested,
                    x => _counter++,
                    x => x,
                    x => TimeSpan.FromSeconds(10),
                    _scheduler)
                .Subscribe(
                    async x => await DoWork(x).ConfigureAwait(false),
                    ex => Logger.LogError(ex, "DoWork: Unknown exception"),
                    () =>
                    {
                        _mainSubscriptionCompleted = true;
                        Logger.LogInformation("Main Subscription completed");
                    },
                    _tokenSource.Token
                );

            _subject
                //.TakeLast(1)
                .Subscribe(
                    x =>
                    {
                        try
                        {
                            //if (_looker == 1)
                            //{
                            //    return;
                            //}
                            Interlocked.Exchange(ref _looker, 1);
                            x.ForEachAsync(MaxDop, ProcessDataAsync).Wait();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Unknown exception");
                        }
                        Interlocked.Exchange(ref _looker, 0);
                    },
                    ex =>
                    {
                        Logger.LogError(ex, "ProcessDataAsync: Unknown exception");
                    },
                    () =>
                    {
                        _processingSubscriptionCompleted = true;
                        Logger.LogInformation("Processing Subscription completed");
                    },
                    _tokenSource.Token
                );
        }


        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _tokenSource?.Cancel();

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);

            while (!_mainSubscriptionCompleted && !_processingSubscriptionCompleted)
            {
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }


        private async Task DoWork(long counter)
        {
            Logger.LogInformation($"Iteration #{counter}");
            await CheckpointAsync().ConfigureAwait(false);
            _subject.OnNext(NodeHashes);
        }
    }
}

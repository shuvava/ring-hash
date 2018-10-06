using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using common.Models;

using Microsoft.Extensions.Options;


namespace worker
{
    public class WorkerFactory : IWorkerFactory
    {
        private CancellationTokenSource _token;
        private readonly IReadOnlyList<Node> _workers;
        public WorkerFactory(
            IOptions<List<Node>> workers)
        {
            _workers = workers.Value;
        }


        public async Task StartAsync(CancellationToken token = default)
        {
            await StopAsync().ConfigureAwait(false);
            _token = CancellationTokenSource.CreateLinkedTokenSource(token);

            foreach (var worker in _workers)
            {
                
            }

        }


        public Task StopAsync()
        {
            if (_token == null)
            {
                return Task.CompletedTask;
            }
            _token.Cancel();
            _token.Dispose();
            return Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

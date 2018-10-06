using System.Collections.Generic;

using common.Models;

using Microsoft.Extensions.Options;


namespace worker
{
    public class WorkerRunner : IWorkerRunner
    {
        private readonly IReadOnlyList<Node> _workers;
        public WorkerRunner(
            IOptions<List<Node>> workers)
        {
            _workers = workers;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using common.Models;


namespace common
{
    public interface IWorkerRepository
    {
        Task<IEnumerable<Node>> GetWorkersAsync();
        Task PutWorkerAsync(Node item);
        Task CheckpointAsync(Node item);
    }
}

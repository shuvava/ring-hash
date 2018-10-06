using System.Threading;
using System.Threading.Tasks;


namespace worker
{
    public interface IWorkerFactory
    {
        Task StartAsync(CancellationToken token = default);
        Task StopAsync();
    }
}

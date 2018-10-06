using System.Threading.Tasks;


namespace worker
{
    public interface IWorkerRunner
    {
        Task StartAsync();
    }
}

using System.Threading.Tasks;

namespace producer
{
    public interface ICommand
    {
        Task Run(int count);
    }
}

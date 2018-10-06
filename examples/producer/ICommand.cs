using System.Threading.Tasks;


namespace producer
{
    public interface ICommand
    {
        Task<int> Run(int count);
    }
}

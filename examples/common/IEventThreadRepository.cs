using System;
using System.Threading.Tasks;

using common.Models;


namespace common
{
    public interface IEventThreadRepository
    {
        Task<EventThread> GetThreadForHashAsync(int hash);
        Task<bool> CheckpointAsync(EventThread item);
        Task<bool> ChangeThreadOwnerAsync(int hash, int oldWorkerId, int newWorkerId, DateTime lockExpirationTime);
    }
}

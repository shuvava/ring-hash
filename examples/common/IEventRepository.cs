using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using common.Models;


namespace common
{
    public interface IEventRepository
    {
        Task<IEnumerable<Event>> GetItemsAsync(DateTime starTime, int mask);
        Task<int> PutItemsAsync(IEnumerable<Event> items);
    }
}

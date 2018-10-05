using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using common.Models;


namespace common
{
    public interface IEventRerpository
    {
        Task<IEnumerable<Event>> GetItems(DateTime starTime, int mask);
        Task<int> PutItems(IEnumerable<Event> items);
    }
}

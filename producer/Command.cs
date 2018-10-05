using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using common;
using common.Models;


namespace producer
{
    public class Command : ICommand
    {
        private readonly Random _random;
        private readonly IEventRepository _repository;


        public Command(IEventRepository repository)
        {
            _repository = repository;
            _random = new Random();
        }


        public Task<int> Run(int count)
        {
            var items = new List<Event>();

            foreach (var i in Enumerable.Range(1, count))
            {
                var model = new Event
                {
                    CreateTime = DateTime.UtcNow.AddMilliseconds(-_random.Next()),
                    EventData = Guid.NewGuid().ToString(),
                    EventTime = DateTime.UtcNow.AddMilliseconds(-_random.Next()),
                    Id = _random.Next(),
                    TransactionId = _random.Next(),
                    UserId = _random.Next()
                };

                items.Add(model);
            }

            return _repository.PutItemsAsync(items);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace common.Extensions
{
    public static class CollectionsAsync
    {
        public static Task WhenAll(this IEnumerable<Task> sourse)
        {
            return Task.WhenAll(sourse);
        }


        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Partitioner.Create(source)
                .GetPartitions(dop)
                .Select(partition => Task.Run(async () =>
                {
                    using (partition)
                    {
                        while (partition.MoveNext())
                        {
                            await body(partition.Current).ConfigureAwait(false);
                        }
                    }
                })).WhenAll();
        }
    }
}

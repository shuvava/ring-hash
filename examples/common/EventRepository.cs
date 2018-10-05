using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

using common.Models;

using Dapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace common
{
    public class EventRepository : IEventRepository
    {
        private readonly ConnectionStrings _connectionStrings;
        private readonly ILogger _logger;


        public EventRepository(
            ILogger<EventRepository> logger,
            IOptions<ConnectionStrings> connectionStrings)
        {
            _logger = logger;
            _connectionStrings = connectionStrings.Value;
        }


        public async Task<IEnumerable<Event>> GetItemsAsync(DateTime starTime, int mask)
        {
            var parameters = new DynamicParameters();
            parameters.Add("dt", starTime);

            using (var connection = new SqlConnection(_connectionStrings.DefaultConnection))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                return await connection.QueryAsync<Event>(
                        "[dbo].[Events_Get]",
                        commandType: CommandType.StoredProcedure,
                        param: parameters)
                    .ConfigureAwait(false);
            }
        }


        public async Task<int> PutItemsAsync(IEnumerable<Event> items)
        {
            using (var connection = new SqlConnection(_connectionStrings.DefaultConnection))
            {
                var command = new SqlCommand("dbo.EventStore_Put", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@events", DataTableUtill.MapListToDataTable(items));

                await connection.OpenAsync().ConfigureAwait(false);

                return (int) await command.ExecuteScalarAsync().ConfigureAwait(false);
            }
        }
    }
}

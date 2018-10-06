using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

using common.Models;

using Dapper;

using Microsoft.Extensions.Options;


namespace common
{
    public class EventThreadRepository : IEventThreadRepository
    {
        private readonly ConnectionStrings _connectionStrings;
        public EventThreadRepository(
            IOptions<ConnectionStrings> connectionStrings)
        {
            _connectionStrings = connectionStrings.Value;
        }


        public async Task<EventThread> GetThreadForHashAsync(int hash)
        {
            var parameters = new DynamicParameters();
            parameters.Add("hash", hash);

            using (var connection = new SqlConnection(_connectionStrings.DefaultConnection))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                var result = await connection.QueryAsync<EventThread>(
                        "[dbo].[EventThread_Get]",
                        commandType: CommandType.StoredProcedure,
                        param: parameters)
                    .ConfigureAwait(false);

                return result.FirstOrDefault();
            }
        }


        public async Task<bool> CheckpointAsync(EventThread item)
        {
            var parameters = new DynamicParameters();
            parameters.Add("hash", item.Hash);
            parameters.Add("workerId", item.WorkerId);
            parameters.Add("threadCheckpoint", item.ThreadCheckpoint);

            using (var connection = new SqlConnection(_connectionStrings.DefaultConnection))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                var result = (int)await connection.ExecuteScalarAsync(
                        "[dbo].[EventThread_Checkpoint]",
                        commandType: CommandType.StoredProcedure,
                        param: parameters)
                    .ConfigureAwait(false);

                return result >= 1;
            }
        }


        public async Task<bool> ChangeThreadOwnerAsync(int hash, int oldWorkerId, int newWorkerId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("hash", hash);
            parameters.Add("oldWorkerId", oldWorkerId);
            parameters.Add("newWorkerId", newWorkerId);

            using (var connection = new SqlConnection(_connectionStrings.DefaultConnection))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                var result = (int)await connection.ExecuteScalarAsync(
                        "[dbo].[EventThread_Update]",
                        commandType: CommandType.StoredProcedure,
                        param: parameters)
                    .ConfigureAwait(false);

                return result>=1;
            }
        }
    }
}

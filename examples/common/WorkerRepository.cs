using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

using common.Models;

using Dapper;

using Microsoft.Extensions.Options;


namespace common
{
    public class WorkerRepository : IWorkerRepository
    {
        private readonly ConnectionStrings _connectionStrings;


        public WorkerRepository(
            IOptions<ConnectionStrings> connectionStrings)
        {
            _connectionStrings = connectionStrings.Value;
        }


        public async Task<IEnumerable<Node>> GetWorkersAsync()
        {
            using (var connection = new SqlConnection(_connectionStrings.DefaultConnection))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                return await connection.QueryAsync<Node>(
                        "[dbo].[Workers_Get]",
                        commandType: CommandType.StoredProcedure)
                    .ConfigureAwait(false);
            }
        }


        public async Task PutWorkerAsync(Node item)
        {
            var parameters = new DynamicParameters();
            parameters.Add("Id", item.Id);
            parameters.Add("LockExpirationTime", item.LockExpirationTime);
            parameters.Add("Description", item.Description);

            using (var db = new SqlConnection(_connectionStrings.DefaultConnection))
            {
                await db.ExecuteScalarAsync<int>(
                    "[dbo].[Workers_Put]",
                    commandType: CommandType.StoredProcedure,
                    param: parameters
                ).ConfigureAwait(false);
            }
        }


        public async Task CheckpointAsync(Node item)
        {
            var parameters = new DynamicParameters();
            parameters.Add("Id", item.Id);
            parameters.Add("LockExpirationTime", item.LockExpirationTime);

            using (var db = new SqlConnection(_connectionStrings.DefaultConnection))
            {
                await db.ExecuteScalarAsync<int>(
                    "[dbo].[Workers_Checkpoint]",
                    commandType: CommandType.StoredProcedure,
                    param: parameters
                ).ConfigureAwait(false);
            }
        }
    }
}

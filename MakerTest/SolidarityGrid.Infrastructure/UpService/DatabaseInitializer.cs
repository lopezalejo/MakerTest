using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolidarityGrid.Infrastructure.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidarityGrid.Infrastructure.UpService
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
            var databaseOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var builder = new SqlConnectionStringBuilder(databaseOptions.SolidarityGrid);
            var databaseName = builder.InitialCatalog;
            builder.InitialCatalog = "master";

            for (var attempt = 1; attempt <= 30; attempt++)
            {
                try
                {
                    await using (var masterConnection = new SqlConnection(builder.ConnectionString))
                    {
                        await masterConnection.OpenAsync(cancellationToken);
                        await using var createDb = masterConnection.CreateCommand();
                        createDb.CommandText = $"""
                        IF DB_ID(@db) IS NULL
                            CREATE DATABASE [{databaseName.Replace("]", "]]")}];
                        """;
                        createDb.Parameters.Add(new SqlParameter("@db", databaseName));
                        await createDb.ExecuteNonQueryAsync(cancellationToken);
                    }

                    var dbContext = scope.ServiceProvider.GetRequiredService<SolidaryGridDBContext>();
                    await dbContext.Database.MigrateAsync(cancellationToken);
                    logger.LogInformation("SQL Server schema ready for database {Database}.", databaseName);
                    return;
                }
                catch (Exception ex) when (attempt < 30)
                {
                    logger.LogWarning(ex, "Waiting for SQL Server (attempt {Attempt}/30)...", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
            }
        }
    }
}

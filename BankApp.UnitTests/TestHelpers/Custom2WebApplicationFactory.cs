using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoftwareVerification_API.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankApp.UnitTests.TestHelpers
{
    public class Custom2WebApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram> where TProgram : class
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the original context registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<BankDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Register a shared SQLite file DB
                var connectionString = $"DataSource={_dbName}.db;Mode=ReadWriteCreate;Cache=Shared";
                var connection = new SqliteConnection(connectionString);
                connection.Open(); // Keeps the connection alive during the test

                services.AddDbContext<BankDbContext>(options =>
                    options.UseSqlite(connection));

                // Ensure DB is created
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
                db.Database.EnsureCreated();
            });
        }
    }
}

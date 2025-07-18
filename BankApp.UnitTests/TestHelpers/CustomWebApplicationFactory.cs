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
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private SqliteConnection _connection;

        public async Task InitializeAsync()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            await _connection.OpenAsync();

            using var context = new BankDbContext(
                new DbContextOptionsBuilder<BankDbContext>()
                    .UseSqlite(_connection)
                    .Options);
            await context.Database.EnsureCreatedAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DB context
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<BankDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Register new one using shared SQLite connection
                services.AddDbContext<BankDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });
            });
        }

        public new async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}

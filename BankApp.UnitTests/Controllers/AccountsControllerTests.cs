using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using SoftwareVerification_API.Data;
using SoftwareVerification_API.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using BankApp.UnitTests.TestHelpers;

namespace BankApp.UnitTests.Controllers
{
    public class AccountsControllerTests : IClassFixture<Custom2WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly BankDbContext _db;
        private readonly IServiceScope _scope;
        public AccountsControllerTests(Custom2WebApplicationFactory<Program> factory)
        {
            //var scopedFactory = factory.WithWebHostBuilder(builder =>
            //{
            //    builder.ConfigureServices(services =>
            //    {
            //        // Remove real DB context and use in-memory
            //        var descriptor = services.SingleOrDefault(
            //            d => d.ServiceType == typeof(DbContextOptions<BankDbContext>));
            //        if (descriptor != null)
            //            services.Remove(descriptor);

            //        services.AddDbContext<BankDbContext>(options =>
            //            options.UseInMemoryDatabase("TestDb"));
            //    });
            //});

            //_client = scopedFactory.CreateClient();
            //var scope = scopedFactory.Services.CreateScope();
            //_db = scope.ServiceProvider.GetRequiredService<BankDbContext>();


            _client = factory.CreateClient();

            _scope = factory.Services.CreateScope();
            _db = _scope.ServiceProvider.GetRequiredService<BankDbContext>();
        }

        [Fact]
        public async Task CloseAccount_ShouldRemoveAccount()
        {
            var account = new Account { AccountNumber = "X001", Balance = 100 };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            var response = await _client.PostAsync($"/api/accounts/close?accountNumber={account.AccountNumber}", null);
            response.EnsureSuccessStatusCode();

            //Assert.Empty(_db.Accounts);
            //var closedAccount = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == account.AccountNumber);

            using var refreshScope = _scope.ServiceProvider.CreateScope();
            var refreshedDb = refreshScope.ServiceProvider.GetRequiredService<BankDbContext>();
            var closedAccount = await refreshedDb.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == account.AccountNumber);

            Assert.NotNull(closedAccount);
            Assert.True(closedAccount!.IsClosed);
        }

        [Fact]
        public async Task OpenAccount_ShouldCreateAccount()
        {
            var response = await _client.PostAsJsonAsync("/api/accounts/open", new { Balance = 500 });
            response.EnsureSuccessStatusCode();

            var accounts = await _db.Accounts.ToListAsync();
            //Assert.Single(accounts);
            Assert.Equal(500, accounts.Last().Balance);
            //Assert.Equal(500, accounts[-1].Balance);
        }

        [Fact]
        public async Task Deposit_ShouldIncreaseBalance()
        {
            var account = new Account { AccountNumber = "X002", Balance = 100 };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            var response = await _client.PostAsJsonAsync("/api/accounts/deposit", new { AccountNumber = "X002", Amount = 50 });
            response.EnsureSuccessStatusCode();

            //var updated = await _db.Accounts.FirstAsync(a => a.AccountNumber == account.AccountNumber);

            using var refreshScope = _scope.ServiceProvider.CreateScope();
            var refreshedDb = refreshScope.ServiceProvider.GetRequiredService<BankDbContext>();
            var updated = await refreshedDb.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == account.AccountNumber);
            Assert.Equal(150, updated.Balance);
        }

        [Fact]
        public async Task Withdraw_ShouldDecreaseBalance()
        {
            var account = new Account { AccountNumber = "X003", Balance = 100 };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            var response = await _client.PostAsJsonAsync("/api/accounts/withdraw", new { AccountNumber = account.AccountNumber, Amount = 30 });
            response.EnsureSuccessStatusCode();

            //var updated = await _db.Accounts.FirstAsync(a => a.AccountNumber == account.AccountNumber);

            using var refreshScope = _scope.ServiceProvider.CreateScope();
            var refreshedDb = refreshScope.ServiceProvider.GetRequiredService<BankDbContext>();
            var updated = await refreshedDb.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == account.AccountNumber);
            Assert.Equal(70, updated.Balance);
        }

        [Fact]
        public async Task GetBalance_ShouldReturnCorrectBalance()
        {
            var account = new Account { AccountNumber = "X004", Balance = 200 };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            var response = await _client.GetAsync($"/api/accounts/balance/{account.AccountNumber}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BalanceDto>();
            Assert.Equal(200, result!.Balance);
        }

        [Fact]
        public async Task TransferFunds_ShouldTransferAmount()
        {
            var from = new Account { AccountNumber = "X005", Balance = 500 };
            var to = new Account { AccountNumber = "X006", Balance = 100 };
            _db.Accounts.AddRange(from, to);
            await _db.SaveChangesAsync();

            var response = await _client.PostAsJsonAsync("/api/accounts/transfer", new
            {
                from_account = "X005",
                to_account = "X006",
                amount = 150
            });
            response.EnsureSuccessStatusCode();

            //Assert.Equal(350, _db.Accounts.First(a => a.AccountNumber == "X005").Balance);
            //Assert.Equal(250, _db.Accounts.First(a => a.AccountNumber == "X006").Balance);

            using var refreshScope = _scope.ServiceProvider.CreateScope();
            var refreshedDb = refreshScope.ServiceProvider.GetRequiredService<BankDbContext>();

            var fromUpdated = await refreshedDb.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == from.AccountNumber);
            var toUpdated = await refreshedDb.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == to.AccountNumber);

            Assert.Equal(350, fromUpdated.Balance);
            Assert.Equal(250, toUpdated.Balance);
        }

        [Fact]
        public async Task TransactionHistory_ShouldReturnEntries()
        {
            var account = new Account { AccountNumber = "X007", Balance = 1000 };
            _db.Accounts.Add(account);
            _db.Transactions.AddRange(
                new Transaction { AccountNumber = "X007", Type = "Deposit", Amount = 100, Timestamp = DateTime.UtcNow },
                new Transaction { AccountNumber = "X007", Type = "Withdraw", Amount = 50, Timestamp = DateTime.UtcNow }
            );
            await _db.SaveChangesAsync();

            var response = await _client.GetAsync("/api/accounts/transactions/X007");
            response.EnsureSuccessStatusCode();

            var txns = await response.Content.ReadFromJsonAsync<List<Transaction>>();
            Assert.Equal(2, txns?.Count);
            Assert.Contains(txns, t => t.Type == "Deposit");
            Assert.Contains(txns, t => t.Type == "Withdraw");
        }

        // Dispose the scope at the end
        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}

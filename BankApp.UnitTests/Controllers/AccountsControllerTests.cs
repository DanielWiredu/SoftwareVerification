using BankApp.UnitTests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Org.BouncyCastle.Asn1.Ocsp;
using SoftwareVerification_API.Data;
using SoftwareVerification_API.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using Xunit;

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

        // The current test suite already covers most core account operations.
        // To further improve test coverage and ensure robustness, we add the following types of unit and integration tests

        // 1. Negative and Edge Case Testing
        //Invalid Withdraw (e.g., insufficient funds)
        [Fact]
        public async Task Withdraw_WithInsufficientFunds_ShouldFail()
        {
            var account = new Account { AccountNumber = "X008", Balance = 50 };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            var response = await _client.PostAsJsonAsync("/api/accounts/withdraw", new { AccountNumber = "X008", Amount = 100 });

            Assert.False(response.IsSuccessStatusCode);
        }
        //Invalid Transfer (e.g., non-existent target account)
        [Fact]
        public async Task TransferFunds_ToNonExistentAccount_ShouldFail()
        {
            var from = new Account { AccountNumber = "X009", Balance = 500 };
            _db.Accounts.Add(from);
            await _db.SaveChangesAsync();

            var response = await _client.PostAsJsonAsync("/api/accounts/transfer", new
            {
                from_account = "X009",
                to_account = "INVALID",
                amount = 100
            });

            Assert.False(response.IsSuccessStatusCode);
        }

        //  2. Validation & Error Messages
        //Check for the presence of error details in failed responses:
        [Fact]
        public async Task OpenAccount_WithNegativeBalance_ShouldReturnBadRequest()
        {
            var response = await _client.PostAsJsonAsync("/api/accounts/open", new { Balance = -100 });
            Assert.False(response.IsSuccessStatusCode);

            var error = await response.Content.ReadAsStringAsync();
            Assert.Contains("Balance", error, StringComparison.OrdinalIgnoreCase);
        }


        //3. Closed Account Behavior
        ///Once an account is closed, make sure operations like deposit/withdraw fail:
        [Fact]
        public async Task Deposit_ToClosedAccount_ShouldFail()
        {
            var account = new Account { AccountNumber = "X010", Balance = 200, IsClosed = true };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            var response = await _client.PostAsJsonAsync("/api/accounts/deposit", new { AccountNumber = "X010", Amount = 50 });
            Assert.False(response.IsSuccessStatusCode);
        }

        //4. Boundary Values
        //Test minimal and maximal values, e.g., 0, very large amounts, etc.
        [Fact]
        public async Task Deposit_ZeroAmount_ShouldFail()
        {
            var account = new Account { AccountNumber = "X011", Balance = 300 };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            var response = await _client.PostAsJsonAsync("/api/accounts/deposit", new { AccountNumber = "X011", Amount = 0 });
            Assert.False(response.IsSuccessStatusCode);
        }

        //5. Idempotency & Consistency
        //Repeat a successful operation and verify that repeated calls don't cause side effects (when they shouldn’t).
        [Fact]
        public async Task CloseAccount_Twice_ShouldReturnConsistentState()
        {
            var account = new Account { AccountNumber = "X012", Balance = 150 };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            var first = await _client.PostAsync($"/api/accounts/close?accountNumber={account.AccountNumber}", null);
            var second = await _client.PostAsync($"/api/accounts/close?accountNumber={account.AccountNumber}", null);

            using var refreshScope = _scope.ServiceProvider.CreateScope();
            var refreshedDb = refreshScope.ServiceProvider.GetRequiredService<BankDbContext>();
            var closedAccount = await refreshedDb.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == "X012");

            Assert.True(closedAccount.IsClosed);
            Assert.True(first.IsSuccessStatusCode || second.IsSuccessStatusCode);
        }

        // 6. Transaction History – Empty Case
        // Test that an account with no transactions returns an empty list.
        [Fact]
        public async Task TransactionHistory_WithNoTransactions_ShouldReturnEmpty()
        {
            var account = new Account { AccountNumber = "X013", Balance = 50 };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            var response = await _client.GetAsync("/api/accounts/transactions/X013");
            response.EnsureSuccessStatusCode();

            var txns = await response.Content.ReadFromJsonAsync<List<Transaction>>();
            Assert.Empty(txns);
        }


        [Fact]
        public async Task Withdraw_MoreThanBalance_ShouldFail()
        {
            var acc = new Account { AccountNumber = "X100", Balance = 100 };
            _db.Accounts.Add(acc);
            await _db.SaveChangesAsync();

            var response = await _client.PostAsJsonAsync("/api/accounts/withdraw", new
            {
                AccountNumber = "X100",
                Amount = 150
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Deposit_NegativeAmount_ShouldFail()
        {
            var acc = new Account { AccountNumber = "X101", Balance = 200 };
            _db.Accounts.Add(acc);
            await _db.SaveChangesAsync();

            var response = await _client.PostAsJsonAsync("/api/accounts/deposit", new
            {
                AccountNumber = "X101",
                Amount = -50
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ConcurrentDeposits_ShouldMaintainConsistency()
        {
            var acc = new Account { AccountNumber = "X102", Balance = 0 };
            _db.Accounts.Add(acc);
            await _db.SaveChangesAsync();

            var tasks = Enumerable.Range(0, 5).Select(_ => _client.PostAsJsonAsync("/api/accounts/deposit", new
            {
                AccountNumber = "X102",
                Amount = 10
            })).ToArray();

            await Task.WhenAll(tasks);

            using var refreshScope = _scope.ServiceProvider.CreateScope();
            var refreshedDb = refreshScope.ServiceProvider.GetRequiredService<BankDbContext>();
            var updated = await refreshedDb.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == "X102");
            Assert.Equal(50, updated.Balance);
        }

        [Fact]
        public async Task Transfer_FromNonExistentAccount_ShouldFail()
        {
            var response = await _client.PostAsJsonAsync("/api/accounts/transfer", new
            {
                from_account = "INVALID",
                to_account = "X103",
                amount = 50
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task OpenAndLogTransaction_ShouldSucceed()
        {
            var response = await _client.PostAsJsonAsync("/api/accounts/open", new { Balance = 300 });
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<Account>();

            var deposit = await _client.PostAsJsonAsync("/api/accounts/deposit", new { AccountNumber = created!.AccountNumber, Amount = 100 });
            deposit.EnsureSuccessStatusCode();

            var txns = await _client.GetFromJsonAsync<List<Transaction>>($"/api/accounts/transactions/{created.AccountNumber}");
            Assert.NotEmpty(txns);
        }

        [Fact]
        public async Task Transaction_ShouldBeAtomicOnFailure()
        {
            var from = new Account { AccountNumber = "X104", Balance = 100 };
            _db.Accounts.Add(from);
            await _db.SaveChangesAsync();

            var response = await _client.PostAsJsonAsync("/api/accounts/transfer", new
            {
                from_account = "X104",
                to_account = "X_NON_EXISTENT",
                amount = 50
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            using var refreshScope = _scope.ServiceProvider.CreateScope();
            var refreshedDb = refreshScope.ServiceProvider.GetRequiredService<BankDbContext>();
            var check = await refreshedDb.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == "X104");
            Assert.Equal(100, check.Balance);
        }

        [Fact]
        public async Task Transfer_ShouldLogTransaction_BothSides()
        {
            var from = new Account { AccountNumber = "X105", Balance = 500 };
            var to = new Account { AccountNumber = "X106", Balance = 100 };
            _db.Accounts.AddRange(from, to);
            await _db.SaveChangesAsync();

            var result = await _client.PostAsJsonAsync("/api/accounts/transfer", new
            {
                from_account = "X105",
                to_account = "X106",
                amount = 150
            });
            result.EnsureSuccessStatusCode();

            var txnsFrom = await _client.GetFromJsonAsync<List<Transaction>>("/api/accounts/transactions/X105");
            var txnsTo = await _client.GetFromJsonAsync<List<Transaction>>("/api/accounts/transactions/X106");

            //using var refreshScope = _scope.ServiceProvider.CreateScope();
            //var refreshedDb = refreshScope.ServiceProvider.GetRequiredService<BankDbContext>();

            //var txnsFrom = refreshedDb.Transactions.Where(t => t.AccountNumber == from.AccountNumber);
            //var txnsTo = refreshedDb.Transactions.Where(t => t.AccountNumber == to.AccountNumber);

            Assert.Contains(txnsFrom!, t => t.Type == "Transfer Out");
            Assert.Contains(txnsTo!, t => t.Type == "Transfer In");
        }

        [Fact]
        public async Task APIContract_ShouldReturnExpectedStructure()
        {
            var transaction = new Transaction
            {
                AccountNumber = "A001",
                Amount = 500,
                Type = "Deposit",
                Timestamp = DateTime.UtcNow
            };
            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync();

            var response = await _client.GetAsync("/api/accounts/transactions/A001");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            Assert.Contains("Type", json);
            Assert.Contains("Amount", json);
            Assert.Contains("AccountNumber", json);
        }


        [Fact]
        public async Task OpenAccount_DuplicateAccountNumber_ShouldFail()
        {
            var existing = new Account { AccountNumber = "ABC", Balance = 100 };
            _db.Accounts.Add(existing);
            await _db.SaveChangesAsync();

            var response = await _client.PostAsJsonAsync("/api/accounts/open", new { AccountNumber = "ABC", Balance = 200 });

            // Assert: should get a conflict (409) or at least a bad request
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        // 1. **Edge Cases**: Tests for edge cases like zero balance, negative amounts, and invalid account numbers.

        // 2. **Concurrency**: Tests to simulate concurrent access to accounts, ensuring thread safety and data integrity.

        // 3. **Error Handling**: Tests to verify that appropriate errors are returned for invalid operations, such as withdrawing more than the balance or transferring between non-existent accounts.

        // 4. **Performance**: Tests to measure the performance of operations under load, ensuring the system can handle high transaction volumes without degradation.

        // 5. **Security**: Tests to ensure that sensitive operations are protected, such as verifying that only authorized users can perform certain actions.
        // 6. **Integration Tests**: Tests that cover the interaction between multiple components, such as ensuring that account creation and transaction logging work together correctly.
        // 7. **Data Consistency**: Tests to ensure that the database remains consistent after operations, such as verifying that balances are correctly updated after deposits and withdrawals.
        // 8. **Boundary Conditions**: Tests for boundary conditions, such as maximum account balance limits or transaction limits, to ensure the system behaves correctly at the edges of its operational parameters.
        // 9. **Rollback Scenarios**: Tests to ensure that in case of an error during a transaction, the system correctly rolls back to the previous state, maintaining data integrity.
        // 10. **Logging and Monitoring**: Tests to verify that all operations are logged correctly, and that monitoring systems can track account activity effectively.
        // 11. **API Contract Tests**: Tests to ensure that the API adheres to its contract, including response formats, status codes, and error messages.
        // 12. **Data Seeding**: Tests to ensure that the database can be seeded with initial data correctly, and that this data is used in tests to validate functionality.


    }
}

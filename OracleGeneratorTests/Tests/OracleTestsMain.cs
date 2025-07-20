using Microsoft.AspNetCore.Mvc.Testing;
using OracleGeneratorTests.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace OracleGeneratorTests.Tests
{
    public class OracleTestsMain : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public OracleTestsMain(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task OracleTest_OpenAccount()
        {
            var spec = SpecLoader.LoadFromYaml("Specs/open_account.yaml");

            // Sample input: omit accountNumber to test auto-generation
            var input = new Dictionary<string, object>
            {
                { "accountnumber", "X6661" }, 
                { "name", "John Doe" },
                { "balance", 100.00 }
            };

            // Validate preconditions
            var preValid = PreconditionValidator.Validate(spec, input, out var preFailures);
            Assert.True(preValid, $"Precondition failed: {string.Join("; ", preFailures)}");

            var content = JsonContent.Create(input);
            var response = await _client.PostAsync(spec.Endpoint, content);
            var body = await response.Content.ReadAsStringAsync();

            // Output oracle validation
            var validOutput = OutputOracle.Validate(spec, response, body, out var postFailures);
            Assert.True(validOutput, $"Output oracle failed: {string.Join("; ", postFailures)}");
        }


        [Fact]
        public async Task OracleTest_Deposit()
        {
            var spec = SpecLoader.LoadFromYaml("Specs/deposit.yaml");

            // Sample input for deposit
            var input = new Dictionary<string, object>
            {
                { "accountNumber", "A001" },
                { "amount", 50.00m }
            };

            // Validate preconditions
            var preValid = PreconditionValidator.Validate(spec, input, out var preFailures);
            Assert.True(preValid, $"Precondition failed: {string.Join("; ", preFailures)}");

            var content = JsonContent.Create(input);
            var response = await _client.PostAsync(spec.Endpoint, content);
            var body = await response.Content.ReadAsStringAsync();

            // Output oracle validation
            var validOutput = OutputOracle.Validate(spec, response, body, out var postFailures);
            Assert.True(validOutput, $"Output oracle failed: {string.Join("; ", postFailures)}");
        }


        [Fact]
        public async Task OracleTest_Withdraw()
        {
            var spec = SpecLoader.LoadFromYaml("Specs/withdraw.yaml");

            var input = new Dictionary<string, object>
            {
                { "accountNumber", "A001" },
                { "amount", 20.0 }
            };

            var preValid = PreconditionValidator.Validate(spec, input, out var preFailures);
            Assert.True(preValid, $"Precondition failed: {string.Join("; ", preFailures)}");

            var content = JsonContent.Create(input);
            var response = await _client.PostAsync(spec.Endpoint, content);
            var body = await response.Content.ReadAsStringAsync();

            var validOutput = OutputOracle.Validate(spec, response, body, out var postFailures);
            Assert.True(validOutput, $"Output oracle failed: {string.Join("; ", postFailures)}");
        }

        [Fact]
        public async Task OracleTest_GetBalance()
        {
            // Load the specification
            var spec = SpecLoader.LoadFromYaml("Specs/get_balance.yaml");

            // Define input with a valid account number
            var input = new Dictionary<string, object>
            {
                { "accountNumber", "A001" }
            };

            // Validate preconditions
            var preValid = PreconditionValidator.Validate(spec, input, out var preFailures);
            Assert.True(preValid, $"Precondition failed: {string.Join("; ", preFailures)}");

            // Replace route parameters in the endpoint
            var endpoint = spec.Endpoint;
            foreach (var kvp in input)
            {
                var token = $"{{{kvp.Key}}}";
                if (endpoint.Contains(token))
                {
                    endpoint = endpoint.Replace(token, kvp.Value.ToString());
                }
            }

            // Send GET request (no content body for GET)
            var response = await _client.GetAsync(endpoint);
            var body = await response.Content.ReadAsStringAsync();

            // Validate output against oracle
            var validOutput = OutputOracle.Validate(spec, response, body, out var postFailures);
            Assert.True(validOutput, $"Output oracle failed: {string.Join("; ", postFailures)}");
        }

        [Theory]
        [InlineData("Specs/transfer_funds.yaml")]
        public async Task OracleTest_TransferFunds(string specPath)
        {
            var spec = SpecLoader.LoadFromYaml(specPath);

            // Generate sample input based on spec inputs (manual logic for now)
            var input = new Dictionary<string, object>();

            input = new Dictionary<string, object>
            {
                { "from_account", "A001" },
                { "to_account", "B002" },
                { "amount", 20 }
            };

            var preValid = PreconditionValidator.Validate(spec, input, out var preFailures);
            Assert.True(preValid, $"Precondition failed: {string.Join("; ", preFailures)}");

            var content = JsonContent.Create(input);
            var response = await _client.PostAsync(spec.Endpoint, content);
            var body = await response.Content.ReadAsStringAsync();

            var validOutput = OutputOracle.Validate(spec, response, body, out var postFailures);
            Assert.True(validOutput, $"Output oracle failed: {string.Join("; ", postFailures)}");
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftwareVerification_API.Data;
using SoftwareVerification_API.Models;
using System.Security.Principal;

namespace SoftwareVerification_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        public static List<object> Transactions = new();
        private readonly BankDbContext _db;

        public AccountsController(BankDbContext db)
        {
            _db = db;
        }

        [HttpGet("{accountNumber}")]
        public IActionResult GetAccount(string accountNumber)
        {
            var account = _db.Accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);
            if (account == null) return NotFound();
            return Ok(account);
        }

        [HttpGet("getall")]
        public IActionResult GetAllAccounts()
        {
            var account = _db.Accounts;
            return Ok(account);
        }
        [HttpPost("open")]
        public async Task<IActionResult> OpenAccount([FromBody] Account newAccount)
        {
            try
            {
                if (newAccount.Balance < 0)
                    return BadRequest("Initial Balance must be greater than zero.");

                var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == newAccount.AccountNumber);
                if (account != null)
                    return Conflict("Account with this number already exists.");

                // Generate AccountNumber
                var totalAccounts = await _db.Accounts.CountAsync();
                var nextId = totalAccounts + 1;
                if (string.IsNullOrEmpty(newAccount.AccountNumber))
                    newAccount.AccountNumber = $"ACC-{nextId:D4}"; // Format: ACC-0001, ACC-0002, etc.
                                                               //newAccount.AccountNumber = $"ACC-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

                // Initialize balance and timestamps
                //newAccount.Balance = 0;
                newAccount.CreatedAt = DateTime.UtcNow;

                _db.Accounts.Add(newAccount);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAccount), new { accountNumber = newAccount.AccountNumber }, newAccount);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
            {
                return Conflict("Account number already exists.");
            }
        }

        [HttpPost("close")]
        public async Task<IActionResult> CloseAccount(string accountNumber)
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

            if (account == null)
                return NotFound("Account not found.");

            if (account.IsClosed)
                return BadRequest("Account is already closed.");

            account.IsClosed = true;
            await _db.SaveChangesAsync();

            return Ok($"Account {account.AccountNumber} has been closed.");
        }

        //[HttpPost("deposit")]
        //public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
        //{
        //    if (request.Amount <= 0)
        //        return BadRequest("Deposit amount must be greater than zero.");

        //    var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == request.AccountNumber);

        //    if (account == null)
        //        return NotFound("Account not found.");

        //    if (account.IsClosed)
        //        return BadRequest("Cannot deposit to a closed account.");

        //    account.Balance += request.Amount; 

        //    // Log transaction 
        //    _db.Transactions?.Add(new Transaction
        //    {
        //        AccountNumber = request.AccountNumber,
        //        Amount = request.Amount,
        //        Type = "Deposit",
        //        Timestamp = DateTime.UtcNow
        //    });

        //    await _db.SaveChangesAsync();

        //    return Ok(new
        //    {
        //        Message = $"Successfully deposited {request.Amount:C} to {account.AccountNumber}.",
        //        NewBalance = account.Balance
        //    });
        //}

        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest("Deposit amount must be greater than zero.");

            var account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.AccountNumber == request.AccountNumber);

            if (account == null)
                return NotFound("Account not found.");

            if (account.IsClosed)
                return BadRequest("Cannot deposit to a closed account.");

            // Atomic update
            var rowsAffected = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Accounts
            SET Balance = Balance + {request.Amount}
            WHERE AccountNumber = {request.AccountNumber}");

            // Log the transaction (if needed)
            _db.Transactions?.Add(new Transaction
            {
                AccountNumber = request.AccountNumber,
                Amount = request.Amount,
                Type = "Deposit",
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // Retrieve updated balance for response
            var updated = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.AccountNumber == request.AccountNumber);

            return Ok(new
            {
                //Message = $"Successfully deposited {request.Amount:C} to {account.AccountNumber}.",
                Message = $"Deposit successful",
                NewBalance = updated.Balance
            });
        }


        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest("Withdrawal amount must be greater than zero.");

            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == request.AccountNumber);

            if (account == null)
                return NotFound("Account not found.");

            if (account.IsClosed)
                return BadRequest("Cannot withdraw from a closed account.");

            if (account.Balance < request.Amount)
                return BadRequest("Insufficient balance.");

            account.Balance -= request.Amount;

            // Log transaction (optional)
            _db.Transactions?.Add(new Transaction
            {
                AccountNumber = request.AccountNumber,
                Amount = -request.Amount, // Store as negative to indicate withdrawal
                Type = "Withdraw",
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Ok(new
            {
                //Message = $"Successfully withdrew {request.Amount:C} from {account.AccountNumber}.",
                Message = $"Withdrawal successful",
                NewBalance = account.Balance
            });
        }

        [HttpGet("balance/{accountNumber}")]
        public async Task<IActionResult> GetBalance(string accountNumber)
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

            if (account == null)
                return NotFound("Account not found.");

            if (account.IsClosed)
                return BadRequest("Cannot fetch balance of a closed account.");

            return Ok(new
            {
                AccountNumber = account.AccountNumber,
                Balance = account.Balance
            });
        }

        //[HttpPost("transfer")]
        //public IActionResult Transfer([FromBody] TransferRequest request)
        //{
        //    if (request.from_account == request.to_account)
        //        return BadRequest("Accounts must be different.");

        //    if (request.amount <= 0)
        //        return BadRequest("Transfer amount must be greater than zero.");

        //    var transaction = new
        //    {
        //        TransactionId = Guid.NewGuid(),
        //        FromAccount = request.from_account,
        //        ToAccount = request.to_account,
        //        Amount = request.amount
        //    };

        //    Transactions.Add(transaction);

        //    return Ok(new
        //    {
        //        message = "Transfer completed successfully",
        //        transactionId = transaction.TransactionId,
        //        from_account = request.from_account,
        //        to_account = request.to_account,
        //        amount = request.amount
        //    });
        //}
        [HttpPost("transfer")]
        public async Task<IActionResult> TransferFunds([FromBody] TransferRequest request)
        {
            if (request.amount <= 0)
                return BadRequest("Transfer amount must be greater than zero.");

            if (request.from_account == request.to_account)
                return BadRequest("Accounts must be different."); // Cannot transfer to the same account.

            var fromAccount = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == request.from_account);
            var toAccount = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == request.to_account);

            if (fromAccount == null || toAccount == null)
                return NotFound("One or both accounts not found.");

            if (fromAccount.IsClosed || toAccount.IsClosed)
                return BadRequest("One or both accounts are closed.");

            if (fromAccount.Balance < request.amount)
                return BadRequest("Insufficient funds.");

            fromAccount.Balance -= request.amount;
            toAccount.Balance += request.amount;

            _db.Transactions.Add(new Transaction
            {
                AccountNumber = fromAccount.AccountNumber,
                Type = "Transfer Out",
                Amount = request.amount,
                Timestamp = DateTime.UtcNow,
                Description = $"Transfer to {toAccount.AccountNumber}"
            });

            _db.Transactions.Add(new Transaction
            {
                AccountNumber = toAccount.AccountNumber,
                Type = "Transfer In",
                Amount = request.amount,
                Timestamp = DateTime.UtcNow,
                Description = $"Transfer from {fromAccount.AccountNumber}"
            });

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Transfer completed successfully",
                from_account = request.from_account,
                to_account = request.to_account,
                amount = request.amount
            });
        }

        [HttpGet("transactions/{accountNumber}")]
        public async Task<IActionResult> GetTransactionHistory(string accountNumber)
        {
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
            if (account == null)
                return NotFound("Account not found.");

            var transactions = await _db.Transactions
                .Where(t => t.AccountNumber == account.AccountNumber)
                .OrderByDescending(t => t.Timestamp)
                .Select(t => new
                {
                    t.AccountNumber,
                    t.Type,
                    t.Amount,
                    t.Timestamp,
                    t.Description
                })
                .ToListAsync();

            return Ok(transactions);
        }

    }

    //public class TransferRequest
    //{
    //    public string from_account { get; set; }
    //    public string to_account { get; set; }
    //    public decimal amount { get; set; }
    //}
}

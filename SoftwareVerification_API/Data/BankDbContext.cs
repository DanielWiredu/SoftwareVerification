using Microsoft.EntityFrameworkCore;
using SoftwareVerification_API.Models;

namespace SoftwareVerification_API.Data
{
    public class BankDbContext : DbContext
    {
        public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }

        public DbSet<Account> Accounts => Set<Account>();
        //public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Transaction> Transactions => Set<Transaction>();

    }
}

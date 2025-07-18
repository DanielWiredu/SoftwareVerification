using Microsoft.EntityFrameworkCore;
using SoftwareVerification_API.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankApp.UnitTests.TestHelpers
{
    public class InMemoryDbContextFactory
    {
        public static BankDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BankDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new BankDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }
    }
}

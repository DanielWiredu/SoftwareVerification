using AppModels.Account;
using AppModels.Common;
using BusinessLogic.Repository;
using BusinessLogic.Repository.IRepository;
using DataAccess.Data;
using DataAccess.DbAccess;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Serilog;
using SoftwareVerification_API.Data;
using SoftwareVerification_API.Helper;
using SoftwareVerification_API.Models;
using SoftwareVerification_API.Service;
using SoftwareVerification_API.Service.IService;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ENGI9839CourseProject_API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please Bearer and then token in the field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                   {
                     new OpenApiSecurityScheme
                     {
                       Reference = new OpenApiReference
                       {
                         Type = ReferenceType.SecurityScheme,
                         Id = "Bearer"
                       }
                      },
                      new string[] { }
                    }
                });
});
// Configure Serilog from appsettings.json
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration) // Read settings from appsettings.json
                 .Enrich.FromLogContext() // Enrich log context
                 .Enrich.WithMachineName() // Add machine name
                 .Enrich.WithThreadId()   // Add thread id
                 .Enrich.WithEnvironmentUserName() // Add the current user
);

//my services
//builder.Services.AddDbContext<BankDbContext>(options => options.UseSqlite("DataSource=:memory:")); // In-memory SQLite

// Create and open a shared in-memory SQLite connection
var keepAliveConnection = new SqliteConnection("DataSource=:memory:");
keepAliveConnection.Open();

// Register DbContext with this connection
builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseSqlite(keepAliveConnection));


builder.Services.AddCors(o => o.AddPolicy("SoftwareVerification", builder =>
{
    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
}));

builder.Services.AddRouting(option => option.LowercaseUrls = true);
builder.Services.AddControllers().AddJsonOptions(opt => opt.JsonSerializerOptions.PropertyNamingPolicy = null)
                .AddNewtonsoftJson(opt =>
                {
                    opt.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });

//my services

var app = builder.Build();
//app.Services.CreateScope().ServiceProvider.GetService<IDbInitializer>().Initialize();
// Open and seed the in-memory SQLite
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    //db.Database.OpenConnection();  // keep connection open
    //db.Database.EnsureCreated();
    var providerName = db.Database.ProviderName;
    if (providerName != "Microsoft.EntityFrameworkCore.InMemory")
    {
        // Relational-only logic
        var connection = db.Database.GetDbConnection();
        connection.Open();
        db.Database.EnsureCreated(); // or db.Database.Migrate();
    }

    db.Accounts.AddRange(
        new Account { AccountNumber = "A001", Balance = 500 },
        new Account { AccountNumber = "B002", Balance = 1000 }
    );
    db.SaveChanges();
}

//app.UseMiddleware<SwaggerBasicAuthMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSwagger();   //// REMOVE IN PRODUCTION
app.UseSwaggerUI();  /////  REMOVE IN PRODUCTION

app.UseHttpsRedirection();

app.UseCors("SoftwareVerification"); //
app.UseAuthentication(); //

app.UseAuthorization();

// Use Serilog request logging to capture HTTP details
app.UseSerilogRequestLogging(); // This will automatically log HTTP request details

app.MapControllers();

app.Run();

// Expose Program for integration testing   --  The public partial class Program is required so the test project can bootstrap the API.
public partial class Program { }


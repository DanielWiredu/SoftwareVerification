using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NuGet.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Microsoft.AspNetCore.WebUtilities;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using NuGet.Common;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SoftwareVerification_API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    //[Authorize]
    public class XcelAccountController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;
        private readonly ILogger<XcelAccountController> _logger;

        public XcelAccountController(RoleManager<IdentityRole> roleManager, IConfiguration config, ILogger<XcelAccountController> logger)
        {
            _roleManager = roleManager;
            _config = config;
            _logger = logger;
        }

        public class RegisterUserRequest
        {
            public string Username { get; set; }
            public int Age { get; set; }
            public string Email { get; set; }
        }
        public class RegisterUserResponse
        {
            public Guid UserId { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
        }
        public static class InMemoryUserStore
        {
            public static List<RegisterUserResponse> Users = new();
        }

        [HttpPost]
        public IActionResult Register([FromBody] RegisterUserRequest request)
        {
            // Preconditions (duplicated for demo, though oracle also checks)
            if (request.Age < 18)
                return BadRequest("User must be at least 18 years old.");

            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
                return BadRequest("Invalid email address.");

            var newUser = new RegisterUserResponse
            {
                UserId = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email
            };

            InMemoryUserStore.Users.Add(newUser);

            return CreatedAtAction(nameof(GetById), new { id = newUser.UserId }, new
            {
                message = "User registered successfully",
                userId = newUser.UserId,
                username = newUser.Username,
                email = newUser.Email
            });
        }

        [HttpGet("{id}")]
        public IActionResult GetById(Guid id)
        {
            var user = InMemoryUserStore.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null) return NotFound();
            return Ok(user);
        }

    }
}

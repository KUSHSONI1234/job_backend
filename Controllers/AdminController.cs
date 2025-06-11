using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobPortalApi.Data;
using JobPortalApi.Models;

namespace RegisterFormAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Admin Registration
        [HttpPost("admin-register")]
        public async Task<IActionResult> Register(Admin admin)
        {
            if (await _context.Admins.AnyAsync(a => a.Email == admin.Email))
            {
                return BadRequest("Admin already exists.");
            }

            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Admin registered successfully." });
        }

        // Admin Login and Token Generation
        [HttpPost("admin-login")]
        public async Task<IActionResult> Login([FromBody] Admin login)
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Email == login.Email && a.Password == login.Password);

            if (admin == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            // Generate JWT Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["Jwt:Key"];
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, admin.Email),
                    new Claim(ClaimTypes.Role, "Admin")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                Message = "Admin login successful.",
                Token = tokenString,
            });

        }
    }
}

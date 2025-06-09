using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using JobPortalApi.Data;
using JobPortalApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace RegisterFormAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public UserController(AppDbContext context, IWebHostEnvironment env, IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] User user)
        {
            if (string.IsNullOrWhiteSpace(user.FullName) ||
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.Phone) ||
                string.IsNullOrWhiteSpace(user.Password) ||
                string.IsNullOrWhiteSpace(user.Skills) ||
                string.IsNullOrWhiteSpace(user.Bio))
            {
                return BadRequest("All fields are required.");
            }

            if (!user.Email.Contains("@") || !user.Email.Contains("."))
                return BadRequest("Invalid email format.");

            if (!Regex.IsMatch(user.Phone, @"^[6-9]\d{9}$"))
                return BadRequest("Invalid phone number. It should be a 10-digit Indian mobile number.");

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest("Email already registered.");

            if (user.Resume != null)
            {
                var uploads = Path.Combine(_env.ContentRootPath, "Uploads");
                Directory.CreateDirectory(uploads);

                var fileName = Path.GetFileName(user.Resume.FileName);
                var filePath = Path.Combine(uploads, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await user.Resume.CopyToAsync(stream);

                user.ResumeFilePath = filePath;
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully." });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and password are required.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || user.Password != request.Password)
                return Unauthorized("Invalid credentials.");

            var token = GenerateJwtToken(user.Email);

            var userData = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Phone,
                user.Skills,
                user.Bio,
                user.ResumeFilePath
            };

            return Ok(new
            {
                token,
                user = userData,
                message = "Login successful"
            });
        }

        private string GenerateJwtToken(string email)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpireMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound("User not found.");

            var userData = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Phone,
                user.Skills,
                user.Bio,
                user.ResumeFilePath
            };

            return Ok(userData);
        }



    }
}

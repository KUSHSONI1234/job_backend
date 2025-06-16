using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegisterApi.Data;
using RegisterApi.Models;
using System.Text.RegularExpressions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace RegisterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public UserController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (user == null)
                return BadRequest("Invalid user data.");

            if (string.IsNullOrWhiteSpace(user.FirstName))
                return BadRequest("First name is required.");
            if (string.IsNullOrWhiteSpace(user.LastName))
                return BadRequest("Last name is required.");
            if (string.IsNullOrWhiteSpace(user.Email))
                return BadRequest("Email is required.");
            if (string.IsNullOrWhiteSpace(user.Password))
                return BadRequest("Password is required.");

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(user.Email))
                return BadRequest("Invalid email format.");

            if (user.Password.Length < 6)
                return BadRequest("Password must be at least 6 characters.");

            var existingUser = _context.Users.FirstOrDefault(u => u.Email == user.Email);
            if (existingUser != null)
                return BadRequest("Email already registered.");

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully" });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel login)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == login.Email && u.Password == login.Password);
            if (user == null)
                return Unauthorized("Invalid email or password.");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpiresInMinutes"])),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expires = token.ValidTo
            });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetLoggedInUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Invalid token");

            var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);
            if (user == null)
                return NotFound("User not found");

            return Ok(new
            {
                fullName = user.FirstName + " " + user.LastName,
                email = user.Email
            });
        }
    }
}

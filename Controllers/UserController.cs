using Microsoft.AspNetCore.Mvc;
using JobPortalAPI.Data;
using JobPortalAPI.Models;
using System.Text.RegularExpressions;

namespace JobPortalAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.FirstName))
                return BadRequest("First Name is required.");

            if (string.IsNullOrWhiteSpace(user.LastName))
                return BadRequest("Last Name is required.");

            if (string.IsNullOrWhiteSpace(user.Email))
                return BadRequest("Email is required.");

            if (string.IsNullOrWhiteSpace(user.Password))
                return BadRequest("Password is required.");

            if (!IsValidEmail(user.Email))
                return BadRequest("Invalid email format.");



            var existingUser = _context.Users.FirstOrDefault(u => u.Email.ToLower() == user.Email.ToLower());
            if (existingUser != null)
                return BadRequest("User with this email already exists.");

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully" });
        }

        private bool IsValidEmail(string email)
        {
            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
        }


    }
}

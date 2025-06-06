using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegisterFormAPI.Data;

namespace RegisterFormAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UserController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
            {
                return BadRequest("Invalid email format.");
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == user.Email);
            if (existingUser != null)
            {
                return BadRequest("Email already registered.");
            }

            if (user.Resume != null)
            {
                var uploads = Path.Combine(_env.ContentRootPath, "Uploads");
                if (!Directory.Exists(uploads))
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

    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobPortalApi.Data;
using JobPortalApi.Models;

namespace RegisterFormAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
            SeedDefaultAdmin(); // Move logic to a private method
        }

        private void SeedDefaultAdmin()
        {
            var exists = _context.Admins.Any(a => a.Email == "admin@gmail.com");
            if (!exists)
            {
                var defaultAdmin = new Admin
                {
                    Email = "admin@gmail.com",
                    Password = "admin"
                };

                _context.Admins.Add(defaultAdmin);
                _context.SaveChanges();
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(Admin admin)
        {
            if (await _context.Admins.AnyAsync(a => a.Email == admin.Email))
            {
                return BadRequest("Admin already exists.");
            }

            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();
            return Ok("Admin registered successfully.");
        }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

public class User
{
    public int Id { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Bio { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Skills { get; set; } = string.Empty;

    public string? ResumeFilePath { get; set; }

    [NotMapped]
    public IFormFile? Resume { get; set; }
}

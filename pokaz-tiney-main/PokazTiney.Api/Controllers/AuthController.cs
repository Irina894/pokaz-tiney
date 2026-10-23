using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokazTiney.Api.Data;
using PokazTiney.Api.Models;

namespace PokazTiney.Api.Controllers;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest dto, CancellationToken cancellationToken)
    {
        var name = dto.Name?.Trim() ?? "";
        var email = dto.Email?.Trim().ToLowerInvariant() ?? "";
        var password = dto.Password ?? "";

        if (name.Length < 2)
            return BadRequest("Введіть ім'я (мінімум 2 символи).");
        if (!email.Contains('@') || email.Length < 5)
            return BadRequest("Введіть коректний email.");
        if (password.Length < 6)
            return BadRequest("Пароль має містити щонайменше 6 символів.");

        if (await _context.Users.AnyAsync(u => u.Email == email, cancellationToken))
            return Conflict("Користувач з таким email уже існує.");

        var user = new User
        {
            FirstName = name,
            LastName = "",
            Email = email,
            Role = UserRole.Viewer
        };
        user.PasswordHash = _hasher.HashPassword(user, password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return StatusCode(201, ToResponse(user));
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest dto, CancellationToken cancellationToken)
    {
        var email = dto.Email?.Trim().ToLowerInvariant() ?? "";
        var password = dto.Password ?? "";

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
            return Unauthorized("Невірний email або пароль.");

        try
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Невірний email або пароль.");
        }
        catch (FormatException)
        {
            // хеш не у форматі PasswordHasher (наприклад, тестовий користувач із SQL)
            return Unauthorized("Невірний email або пароль.");
        }

        return Ok(ToResponse(user));
    }

    private static object ToResponse(User u) => new
    {
        id = u.Id,
        name = (u.FirstName + " " + u.LastName).Trim(),
        email = u.Email,
        role = u.Role.ToString()
    };
}

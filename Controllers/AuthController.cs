using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TodoApp.Data;
using TodoApp.Models;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
  private readonly AppDbContext _context;
  private readonly IConfiguration _config;

  public AuthController(AppDbContext context, IConfiguration config)
  {
    _context = context;
    _config = config;
  }

  [HttpPost("register")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> Register(User user)
  {
    if (!ModelState.IsValid)
      return BadRequest(ModelState);

    if (await _context.Users.AnyAsync(u => u.Email == user.Email))
      return BadRequest("Email already exists");

    try
    {
      user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
      _context.Users.Add(user);
      await _context.SaveChangesAsync();
      return Ok(new { message = "User registered successfully" });
    }
    catch (Exception)
    {
      return StatusCode(500, "An error occurred while registering the user");
    }
  }

  [HttpPost("login")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> Login(User user)
  {
    if (string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.PasswordHash))
      return BadRequest("Email and password are required");

    try
    {
      var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
      if (dbUser == null || !BCrypt.Net.BCrypt.Verify(user.PasswordHash, dbUser.PasswordHash))
        return Unauthorized("Invalid credentials");

      var token = GenerateJwtToken(dbUser);
      return Ok(new { token, userId = dbUser.Id, email = dbUser.Email });
    }
    catch (Exception)
    {
      return StatusCode(500, "An error occurred while logging in");
    }
  }

  private string GenerateJwtToken(User user)
  {
    var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured");
    var jwtIssuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured");
    var jwtAudience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience is not configured");

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
      new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
      new Claim(ClaimTypes.Email, user.Email),
      new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
      issuer: _config["Jwt:Issuer"],
      audience: _config["Jwt:Audience"],
      claims: claims,
      expires: DateTime.UtcNow.AddHours(1),
      signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}

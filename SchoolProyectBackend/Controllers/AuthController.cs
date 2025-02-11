using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SchoolProyectBackend.Data;
using SchoolProyectBackend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;

using System.Security.Claims;
using System.Text;

namespace SchoolProyectBackend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User userRequest)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userRequest.Email);

            if (user == null || user.PasswordHash != userRequest.PasswordHash)
            {
                return new ObjectResult(new { mensaje = "Usuario o contraseña incorrectos." }) { StatusCode = 401 };

            }

            var token = GenerateJwtToken(user);
            return Ok(new { mensaje = "Inicio de sesión exitoso", token });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key", "La clave JWT no está definida en la configuración.");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim("UserId", user.UserID.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

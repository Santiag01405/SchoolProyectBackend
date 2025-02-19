using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SchoolProyectBackend.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Data;
using Microsoft.AspNetCore.Identity.Data;

namespace SchoolProyectBackend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 🔹 Endpoint de Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                    return BadRequest(new { message = "Email y contraseña requeridos" });

                // Buscar usuario por email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                    return Unauthorized(new { message = "Usuario no encontrado" });

                // 🔹 Comparar contraseña sin encriptación
                if (!string.Equals(user.PasswordHash, request.Password, StringComparison.Ordinal))
                    return Unauthorized(new { message = "Contraseña incorrecta" });

                // Generar el token
                var token = GenerateJwtToken(user);

                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error en el servidor", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // 🔹 Endpoint para registrar usuario (SIN encriptar contraseñas)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            try
            {
                if (string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.PasswordHash))
                    return BadRequest(new { message = "Email y contraseña requeridos" });

                // Validar si ya existe un usuario con el mismo email
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
                if (existingUser != null)
                    return Conflict(new { message = "El usuario ya existe" });

                // 🔹 Guardar la contraseña en texto plano (SIN encriptar)
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(Login), new { email = user.Email }, new { message = "Usuario registrado con éxito" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error en el servidor", error = ex.Message });
            }
        }

        // 🔹 Método para generar el JWT Token
        private string GenerateJwtToken(User user)
        {
            var key = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(issuer))
                throw new InvalidOperationException("JWT Key o Issuer no configurado correctamente en appsettings.json");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserID", user.UserID.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}



/*using Microsoft.AspNetCore.Identity.Data;
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
}*/

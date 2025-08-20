using LaOriginalBackend.Data;
using LaOriginalBackend.Models;
using LaOriginalBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LaOriginalBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Login de usuario: recibe username + password y devuelve JWT
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Buscar usuario en DB
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
                return Unauthorized(new { message = "Usuario no encontrado" });

            // Verificar contraseña
            if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Contraseña incorrecta" });

            // Crear token JWT
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login exitoso",
                token,
                user = new { user.Id, user.Username, user.Email }
            });
        }

        // 🔹 Generar token JWT
        private string GenerateJwtToken(Usuario user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "clave_super_secreta_123456";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "LaOriginalAPI";

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim("id", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtIssuer,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // DTO para login
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}

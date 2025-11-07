// Controllers/AuthController.cs
using LaOriginalBackend.Data;
using LaOriginalBackend.Models;
using LaOriginalBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

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

        // ========== LOGIN ==========
        public class LoginRequest
        {
            public string? UsernameOrEmail { get; set; }
            public string? Username { get; set; }
            public string? Email { get; set; }
            public string Password { get; set; } = default!;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var idOrEmail = request.UsernameOrEmail ?? request.Username ?? request.Email ?? string.Empty;

            var user = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Username == idOrEmail || u.Email == idOrEmail);

            if (user == null)
                return Unauthorized(new { message = "Usuario no encontrado" });

            if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Contraseña incorrecta" });

            if (user.Estado?.Equals("Inactivo", StringComparison.OrdinalIgnoreCase) == true)
                return StatusCode(403, new { message = "Usuario inactivo" });

            if (user.Rol != null && !user.Rol.Activo)
                return StatusCode(403, new { message = "Rol inactivo" });

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login exitoso",
                token,
                mustChangePassword = user.DebeCambiarPassword,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.PrimerNombre,
                    user.PrimerApellido,
                    user.FotoUrl,
                    user.RolId,
                    RolNombre = user.Rol?.Nombre
                }
            });
        }

        // ========== PERFIL ==========
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var idClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            Usuario? user = null;

            if (int.TryParse(idClaim, out var idInt))
            {
                user = await _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.Id == idInt);
            }

            if (user == null)
            {
                var username =
                    User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                    User.FindFirst(ClaimTypes.Name)?.Value ??
                    User.FindFirst("unique_name")?.Value;

                if (!string.IsNullOrWhiteSpace(username))
                {
                    user = await _context.Usuarios
                        .Include(u => u.Rol)
                        .FirstOrDefaultAsync(u => u.Username == username);
                }
            }

            if (user == null) return NotFound(new { message = "Usuario no encontrado" });

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.PrimerNombre,
                user.PrimerApellido,
                user.FotoUrl,
                user.RolId,
                RolNombre = user.Rol?.Nombre,
                mustChangePassword = user.DebeCambiarPassword
            });
        }

        // ========== SUBIR FOTO (AUTENTICADO) ==========
        [Authorize]
        [HttpPost("photo")]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Archivo vacío");

            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(file.ContentType)) return BadRequest("Tipo no permitido");
            if (file.Length > 5_000_000) return BadRequest("Máx 5MB");

            var idClaim = User.FindFirst("id")?.Value;
            if (!int.TryParse(idClaim, out var userId)) return Unauthorized();

            var user = await _context.Usuarios.FindAsync(userId);
            if (user == null) return NotFound("Usuario no encontrado");

            var uploads = GetUserUploadDir(userId);
            Directory.CreateDirectory(uploads);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
            var fileName = $"profile{ext}";
            var fullPath = Path.Combine(uploads, fileName);

            using (var stream = System.IO.File.Create(fullPath))
                await file.CopyToAsync(stream);

            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var publicUrl = $"{baseUrl}/uploads/users/{userId}/{fileName}";

            user.FotoUrl = publicUrl;
            await _context.SaveChangesAsync();

            return Ok(new { fotoUrl = publicUrl });
        }

        // ========== CAMBIO DE CONTRASEÑA ==========
        public class ChangePasswordRequest
        {
            public string CurrentPassword { get; set; } = default!;
            public string NewPassword { get; set; } = default!;
        }

        [Authorize]
        [HttpPost("change-password")]
        [Consumes("application/json")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var idStr = User.FindFirst("id")?.Value;
            if (!int.TryParse(idStr, out var userId)) return Unauthorized();

            var user = await _context.Usuarios.FindAsync(userId);
            if (user == null) return NotFound(new { message = "Usuario no encontrado" });

            if (!PasswordHelper.VerifyPassword(req.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "Contraseña actual incorrecta" });

            var rx = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,64}$");
            if (!rx.IsMatch(req.NewPassword))
                return BadRequest(new { message = "La nueva contraseña no cumple complejidad" });

            user.PasswordHash = PasswordHelper.HashPassword(req.NewPassword);
            user.DebeCambiarPassword = false;
            user.PasswordChangedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reemitir token sin el claim de cambio pendiente
            var token = GenerateJwtToken(user);
            return Ok(new { ok = true, token });
        }

        // ========== JWT ==========
        private string GenerateJwtToken(Usuario user)
        {
            if (user.Rol == null)
                user = _context.Usuarios.Include(u => u.Rol).First(u => u.Id == user.Id);

            var jwtKey = _configuration["Jwt:Key"] ?? "clave-secreta-super-segura-12345";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "LaOriginal";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "LaOriginalUsuarios";

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Username),
        new Claim("id", user.Id.ToString()),
        new Claim("email", user.Email ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.GivenName, user.PrimerNombre ?? string.Empty),
        new Claim("PrimerNombre", user.PrimerNombre ?? string.Empty),
        new Claim(ClaimTypes.GivenName, user.PrimerNombre ?? string.Empty),
        new Claim(ClaimTypes.Name, $"{user.PrimerNombre} {user.PrimerApellido}".Trim()),
        new Claim("picture", user.FotoUrl ?? string.Empty),
        new Claim("rolId", user.RolId.ToString()),
        new Claim("pwd_change_required", user.DebeCambiarPassword ? "1" : "0")
    };

            if (!user.DebeCambiarPassword && !string.IsNullOrWhiteSpace(user.Rol?.Nombre))
            {
                // Token normal: incluye roles
                claims.Add(new Claim(ClaimTypes.Role, user.Rol!.Nombre));
                claims.Add(new Claim("role", user.Rol!.Nombre));
            }
            else
            {
                // Token temporal: sin roles
                claims.Add(new Claim("auth_scope", "password_change"));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = user.DebeCambiarPassword
                ? DateTime.UtcNow.AddMinutes(10)  // corto mientras debe cambiar
                : DateTime.UtcNow.AddHours(3);    // normal

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        // ==== Helpers (uploads root compartido con UsuariosController) ====
        private string GetUploadsRoot()
        {
            var cfg = _configuration["Uploads:Root"];
            if (!string.IsNullOrWhiteSpace(cfg)) return cfg;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LaOriginal", "uploads"
            );
        }

        private string GetUserUploadDir(int userId) =>
            Path.Combine(GetUploadsRoot(), "users", userId.ToString());
    }
}

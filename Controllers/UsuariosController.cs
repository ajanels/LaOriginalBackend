// Controllers/UsuariosController.cs
using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using LaOriginalBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net; // HtmlEncode

namespace LaOriginalBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailService _email;

        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };
        private static readonly string[] AllowedExts = { ".jpg", ".jpeg", ".png", ".webp" };

        public UsuariosController(AppDbContext context, IConfiguration configuration, EmailService email)
        {
            _context = context;
            _configuration = configuration;
            _email = email;
        }

        // ===== Listado =====
        [Authorize(Roles = "Administrador,Admin")]
        [HttpGet]
        public async Task<IActionResult> GetUsuarios()
        {
            var list = await _context.Usuarios
                .AsNoTracking()
                .Include(u => u.Rol)
                .Select(u => new UsuarioListDto
                {
                    Id = u.Id,
                    PrimerNombre = u.PrimerNombre,
                    SegundoNombre = u.SegundoNombre,
                    PrimerApellido = u.PrimerApellido,
                    SegundoApellido = u.SegundoApellido,
                    Username = u.Username,
                    Email = u.Email,
                    Celular = u.Celular,
                    Estado = u.Estado,
                    FotoUrl = u.FotoUrl,
                    RolId = u.RolId,
                    Rol = u.Rol != null ? new RolMiniDto { Id = u.Rol.Id, Nombre = u.Rol.Nombre } : null
                })
                .OrderBy(u => u.PrimerNombre).ThenBy(u => u.PrimerApellido)
                .ToListAsync();

            return Ok(list);
        }

        // ===== Detalle =====
        [Authorize(Roles = "Administrador,Admin")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUsuario(int id)
        {
            var u = await _context.Usuarios
                .AsNoTracking()
                .Include(x => x.Rol)
                .Where(x => x.Id == id)
                .Select(x => new UsuarioDetailDto
                {
                    Id = x.Id,
                    PrimerNombre = x.PrimerNombre,
                    SegundoNombre = x.SegundoNombre,
                    PrimerApellido = x.PrimerApellido,
                    SegundoApellido = x.SegundoApellido,
                    NIT = x.NIT,
                    CUI = x.CUI,
                    FechaNacimiento = x.FechaNacimiento,
                    FechaIngreso = x.FechaIngreso,
                    Celular = x.Celular,
                    Genero = x.Genero,
                    Estado = x.Estado,
                    Direccion = x.Direccion,
                    Email = x.Email,
                    Username = x.Username,
                    FotoUrl = x.FotoUrl,
                    RolId = x.RolId,
                    Rol = x.Rol != null ? new RolMiniDto { Id = x.Rol.Id, Nombre = x.Rol.Nombre } : null
                })
                .FirstOrDefaultAsync();

            if (u == null) return NotFound(new { message = "Usuario no encontrado." });
            return Ok(u);
        }

        // ===== Subir foto (edición) =====
        [Authorize(Roles = "Administrador,Admin")]
        [HttpPost("{id:int}/photo")]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPhoto(int id)
        {
            try
            {
                var uploaded = Request.Form.Files["foto"]
                            ?? Request.Form.Files["file"]
                            ?? (Request.Form.Files.Count > 0 ? Request.Form.Files[0] : null);

                if (uploaded == null || uploaded.Length == 0)
                    return BadRequest(new { message = "Archivo vacío." });

                var ct = uploaded.ContentType?.ToLowerInvariant() ?? "";
                if (!AllowedContentTypes.Contains(ct))
                    return BadRequest(new { message = "Solo se permiten JPG, PNG o WEBP." });

                var user = await _context.Usuarios.FindAsync(id);
                if (user == null) return NotFound(new { message = "Usuario no encontrado." });

                var uploads = GetUserUploadDir(id);
                Directory.CreateDirectory(uploads);

                DeletePreviousPhotoIfAny(user.FotoUrl, uploads);

                var ext = Path.GetExtension(uploaded.FileName).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(ext) || !AllowedExts.Contains(ext))
                {
                    ext = ct switch
                    {
                        "image/jpeg" => ".jpg",
                        "image/png" => ".png",
                        "image/webp" => ".webp",
                        _ => ".jpg"
                    };
                }

                var fileName = $"profile{ext}";
                var fullPath = Path.Combine(uploads, fileName);
                using (var stream = System.IO.File.Create(fullPath))
                    await uploaded.CopyToAsync(stream);

                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                user.FotoUrl = $"{baseUrl}/uploads/users/{user.Id}/{fileName}";
                await _context.SaveChangesAsync();

                return Ok(new { fotoUrl = user.FotoUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UploadPhoto error: {ex}");
                return StatusCode(500, new { message = "Error subiendo la foto." });
            }
        }

        // ===== Crear =====
        [Authorize(Roles = "Administrador,Admin")]
        [HttpPost]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CrearUsuario([FromForm] UsuarioCreateDto dto)
        {
            // Parse seguro de fechas
            DateTime fechaNac = dto.FechaNacimiento;
            DateTime fechaIng = dto.FechaIngreso;

            if (fechaNac == default && Request?.Form != null)
                TryParseFechaFlexible(Request.Form["fechaNacimiento"], out fechaNac);
            if (fechaIng == default && Request?.Form != null)
                TryParseFechaFlexible(Request.Form["fechaIngreso"], out fechaIng);

            if (fechaNac == default || fechaIng == default)
                return BadRequest(new { message = "Fechas inválidas. Usa formato yyyy-MM-dd." });

            var hoy = DateTime.UtcNow.Date;
            if (fechaNac < new DateTime(1900, 1, 1) || fechaNac > hoy.AddYears(-15))
                return BadRequest(new { message = "FechaNacimiento inválida: >= 1900-01-01 y <= hoy - 15 años." });
            if (fechaIng < new DateTime(1990, 1, 1) || fechaIng > hoy)
                return BadRequest(new { message = "FechaIngreso inválida: >= 1990-01-01 y <= hoy." });
            if (fechaIng < fechaNac.AddYears(15))
                return BadRequest(new { message = "FechaIngreso debe ser al menos 15 años después de la FechaNacimiento." });

            var rol = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == dto.RolId && r.Activo);
            if (rol == null) return BadRequest(new { message = "Rol inválido o inactivo." });

            if (await _context.Usuarios.AnyAsync(u => u.NIT == dto.NIT))
                return Conflict(new { message = "El NIT ya existe." });
            if (await _context.Usuarios.AnyAsync(u => u.CUI == dto.CUI))
                return Conflict(new { message = "El CUI ya existe." });
            if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email))
                return Conflict(new { message = "El Email ya existe." });

            // Username único
            string baseUsername =
                (dto.PrimerNombre[..Math.Min(4, dto.PrimerNombre.Length)] +
                 dto.PrimerApellido[..Math.Min(4, dto.PrimerApellido.Length)]).ToLower();
            string finalUsername = baseUsername;
            int contador = 1;
            while (await _context.Usuarios.AnyAsync(u => u.Username == finalUsername))
                finalUsername = $"{baseUsername}{contador++}";

            var user = new Usuario
            {
                PrimerNombre = dto.PrimerNombre,
                SegundoNombre = dto.SegundoNombre,
                PrimerApellido = dto.PrimerApellido,
                SegundoApellido = dto.SegundoApellido,
                NIT = dto.NIT,
                CUI = dto.CUI,
                FechaNacimiento = fechaNac,
                FechaIngreso = fechaIng,
                Celular = dto.Celular,
                Genero = dto.Genero,
                Estado = dto.Estado,
                Direccion = dto.Direccion,
                Email = dto.Email,
                Username = finalUsername,
                PasswordHash = PasswordHelper.HashPassword(dto.Password),
                RolId = dto.RolId,

                // Forzar cambio en primer login
                DebeCambiarPassword = true
            };

            _context.Usuarios.Add(user);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var msg =
                    ex.InnerException?.Message.Contains("IX_Usuarios_NIT") == true ? "El NIT ya existe." :
                    ex.InnerException?.Message.Contains("IX_Usuarios_CUI") == true ? "El CUI ya existe." :
                    ex.InnerException?.Message.Contains("IX_Usuarios_Email") == true ? "El Email ya existe." :
                    ex.InnerException?.Message.Contains("IX_Usuarios_Username") == true ? "El Username ya existe." :
                    "No se pudo guardar.";
                return Conflict(new { message = msg });
            }

            // Foto opcional
            if (dto.Foto != null && dto.Foto.Length > 0)
            {
                try
                {
                    var ct = dto.Foto.ContentType?.ToLowerInvariant() ?? "";
                    if (!AllowedContentTypes.Contains(ct))
                        return BadRequest(new { message = "Solo se permiten JPG, PNG o WEBP." });

                    var uploads = GetUserUploadDir(user.Id);
                    Directory.CreateDirectory(uploads);

                    DeletePreviousPhotoIfAny(user.FotoUrl, uploads);

                    var ext = Path.GetExtension(dto.Foto.FileName).ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(ext) || !AllowedExts.Contains(ext))
                    {
                        ext = ct switch
                        {
                            "image/jpeg" => ".jpg",
                            "image/png" => ".png",
                            "image/webp" => ".webp",
                            _ => ".jpg"
                        };
                    }

                    var fileName = $"profile{ext}";
                    var fullPath = Path.Combine(uploads, fileName);
                    using (var stream = System.IO.File.Create(fullPath))
                        await dto.Foto.CopyToAsync(stream);

                    var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                    user.FotoUrl = $"{baseUrl}/uploads/users/{user.Id}/{fileName}";
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error guardando foto en CrearUsuario: {ex}");
                }
            }

            // ===== Envío de correo (bonito con logo) =====
            var (webUrl, logoUrl) = GetBranding();

            var bodyHtml = $@"
  <p>Se creó una cuenta para ti en <b>La Original</b>. Estas son tus credenciales temporales:</p>
  <table role='presentation' cellspacing='0' cellpadding='0' border='0' style='width:100%; max-width:520px; margin:6px 0 10px;'>
    <tr>
      <td style='padding:6px 0; color:#667085; width:160px;'>Usuario:</td>
      <td style='padding:6px 0; color:#101828; font-weight:600;'>{WebUtility.HtmlEncode(finalUsername)}</td>
    </tr>
    <tr>
      <td style='padding:6px 0; color:#667085;'>Contraseña temporal:</td>
      <td style='padding:6px 0;'>
        <code style='background:#F2F4F7; padding:2px 6px; border-radius:6px; color:#101828;'>{WebUtility.HtmlEncode(dto.Password)}</code>
      </td>
    </tr>
  </table>
  <p style='margin-top:8px; color:#667085;'>Por seguridad, se te pedirá <b>cambiarla</b> al iniciar sesión.</p>";

            var htmlWelcome = BuildEmailHtml(
                subjectTitle: "¡Bienvenido a La Original!",
                greetingName: user.PrimerNombre ?? user.Username,
                bodyHtml: bodyHtml,
                actionText: "Iniciar sesión",
                actionUrl: $"{webUrl}/login",
                logoUrl: logoUrl
            );

            await _email.SendEmailAsync(
                dto.Email,
                "Credenciales de acceso - La Original",
                htmlWelcome
            );

            var created = new UsuarioDetailDto
            {
                Id = user.Id,
                PrimerNombre = user.PrimerNombre,
                SegundoNombre = user.SegundoNombre,
                PrimerApellido = user.PrimerApellido,
                SegundoApellido = user.SegundoApellido,
                NIT = user.NIT,
                CUI = user.CUI,
                FechaNacimiento = user.FechaNacimiento,
                FechaIngreso = user.FechaIngreso,
                Celular = user.Celular,
                Genero = user.Genero,
                Estado = user.Estado,
                Direccion = user.Direccion,
                Email = user.Email,
                Username = user.Username,
                FotoUrl = user.FotoUrl,
                RolId = user.RolId,
                Rol = new RolMiniDto { Id = user.RolId, Nombre = rol.Nombre }
            };

            return CreatedAtAction(nameof(GetUsuario), new { id = user.Id }, created);
        }

        // ===== Editar =====
        [Authorize(Roles = "Administrador,Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> EditarUsuario(int id, [FromBody] UsuarioUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest(new { message = "Id inválido." });

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null) return NotFound(new { message = "Usuario no encontrado." });

            var hoy = DateTime.UtcNow.Date;
            var fn = dto.FechaNacimiento; var fi = dto.FechaIngreso;
            if (fn < new DateTime(1900, 1, 1) || fn > hoy.AddYears(-15))
                return BadRequest(new { message = "FechaNacimiento inválida: >= 1900-01-01 y <= hoy - 15 años." });
            if (fi < new DateTime(1990, 1, 1) || fi > hoy)
                return BadRequest(new { message = "FechaIngreso inválida: >= 1990-01-01 y <= hoy." });
            if (fi < fn.AddYears(15))
                return BadRequest(new { message = "FechaIngreso debe ser al menos 15 años después de la FechaNacimiento." });

            var rol = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == dto.RolId && r.Activo);
            if (rol == null) return BadRequest(new { message = "Rol inválido o inactivo." });

            if (await _context.Usuarios.AnyAsync(u => u.NIT == dto.NIT && u.Id != id))
                return Conflict(new { message = "El NIT ya existe." });
            if (await _context.Usuarios.AnyAsync(u => u.CUI == dto.CUI && u.Id != id))
                return Conflict(new { message = "El CUI ya existe." });
            if (await _context.Usuarios.AnyAsync(u => u.Email == dto.Email && u.Id != id))
                return Conflict(new { message = "El Email ya existe." });

            // --- Asignaciones base ---
            usuario.PrimerNombre = dto.PrimerNombre;
            usuario.SegundoNombre = dto.SegundoNombre;
            usuario.PrimerApellido = dto.PrimerApellido;
            usuario.SegundoApellido = dto.SegundoApellido;
            usuario.NIT = dto.NIT;
            usuario.CUI = dto.CUI;
            usuario.FechaNacimiento = fn;
            usuario.FechaIngreso = fi;
            usuario.Celular = dto.Celular;
            usuario.Genero = dto.Genero;
            usuario.Estado = dto.Estado;
            usuario.Direccion = dto.Direccion;
            usuario.Email = dto.Email;
            usuario.RolId = dto.RolId;

            // --- Reset por ADMIN ---
            bool adminResetPwd = false;
            string? plainTempPwd = null;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                usuario.PasswordHash = PasswordHelper.HashPassword(dto.Password);

                // Fuerza cambio en el siguiente login
                usuario.DebeCambiarPassword = true;

                adminResetPwd = true;
                plainTempPwd = dto.Password;
            }

            await _context.SaveChangesAsync();

            // Notificación por email si el admin cambió la contraseña (bonito con logo)
            if (adminResetPwd && !string.IsNullOrWhiteSpace(usuario.Email))
            {
                try
                {
                    var (webUrl, logoUrl) = GetBranding();

                    var bodyHtml = $@"
          <p>Un administrador restableció tu contraseña en <b>La Original</b>.</p>
          <table role='presentation' cellspacing='0' cellpadding='0' border='0' style='width:100%; max-width:520px; margin:6px 0 10px;'>
            <tr>
              <td style='padding:6px 0; color:#667085; width:160px;'>Contraseña temporal:</td>
              <td style='padding:6px 0;'>
                <code style='background:#F2F4F7; padding:2px 6px; border-radius:6px; color:#101828;'>{WebUtility.HtmlEncode(plainTempPwd)}</code>
              </td>
            </tr>
          </table>
          <p style='margin-top:8px; color:#667085;'>Por seguridad, al iniciar sesión se te pedirá <b>cambiarla inmediatamente</b>.</p>";

                    var html = BuildEmailHtml(
                        subjectTitle: "Contraseña temporal asignada",
                        greetingName: usuario.PrimerNombre ?? usuario.Username,
                        bodyHtml: bodyHtml,
                        actionText: "Cambiar contraseña",
                        actionUrl: $"{webUrl}/login",
                        logoUrl: logoUrl
                    );

                    await _email.SendEmailAsync(
                        usuario.Email!,
                        "Contraseña restablecida - La Original",
                        html
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error enviando email de reset a {usuario.Email}: {ex}");
                }
            }

            return NoContent();
        }

        // ===== Eliminar =====
        [Authorize(Roles = "Administrador,Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound(new { message = "Usuario no encontrado." });

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ===== Debug SendGrid =====
        [Authorize(Roles = "Administrador,Admin")]
        [HttpPost("debug/sendgrid")]
        public async Task<IActionResult> DebugSendGrid([FromQuery] string to)
        {
            await _email.SendEmailAsync(to, "Prueba SendGrid", "<b>Hola!</b> Esto es una prueba.");
            return Ok(new { ok = true });
        }

        // ==== Helpers ruta uploads ====
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

        private void DeletePreviousPhotoIfAny(string? fotoUrl, string uploadsDir)
        {
            if (string.IsNullOrWhiteSpace(fotoUrl)) return;
            try
            {
                var uri = new Uri(fotoUrl, UriKind.RelativeOrAbsolute);
                var fileName = uri.IsAbsoluteUri ? Path.GetFileName(uri.LocalPath) : Path.GetFileName(fotoUrl);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    var prev = Path.Combine(uploadsDir, fileName);
                    if (System.IO.File.Exists(prev)) System.IO.File.Delete(prev);
                }
            }
            catch { /* no-op */ }
        }

        private static bool TryParseFechaFlexible(string? raw, out DateTime value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy", "yyyy/MM/dd" };
            foreach (var f in formats)
            {
                if (DateTime.TryParseExact(raw, f, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value)) return true;
                if (DateTime.TryParseExact(raw, f, new CultureInfo("es-GT"),
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value)) return true;
            }

            return DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value);
        }

        // ======== Branding / Email Template ========
        private (string WebUrl, string LogoUrl) GetBranding()
        {
            var webUrl = _configuration["App:WebUrl"] ?? "https://laoriginal.example.com";
            var logoUrl = _configuration["Branding:LogoUrl"] ?? $"{webUrl}/assets/logo-laoriginal.png";
            return (webUrl, logoUrl);
        }

        private string BuildEmailHtml(
            string subjectTitle,
            string greetingName,
            string bodyHtml,
            string? actionText = null,
            string? actionUrl = null,
            string? logoUrl = null
        )
        {
            const string primary = "#6b46c1";
            const string text = "#101828";
            const string muted = "#667085";
            const string bg = "#F2F4F7";

            var safeGreeting = WebUtility.HtmlEncode(greetingName ?? "");
            var preheader = "Notificación de La Original";

            return $@"
<!doctype html>
<html lang=""es"">
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>{WebUtility.HtmlEncode(subjectTitle)}</title>
<style>
@media (max-width: 620px) {{
  .container {{ width: 100% !important; }}
  .p-sm {{ padding: 16px !important; }}
}}
</style>
</head>
<body style=""margin:0; padding:0; background:{bg}; font-family:Arial, Helvetica, sans-serif;"">
  <div style=""display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;"">{preheader}</div>

  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background:{bg}; padding:24px 12px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" class=""container"" width=""600"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""width:600px; max-width:100%;"">
          <tr>
            <td align=""left"" style=""padding:0 8px 12px 8px;"">
              {(string.IsNullOrWhiteSpace(logoUrl) ? "" : $@"<img src=""{logoUrl}"" alt=""La Original"" height=""44"" style=""display:block; height:44px;"">")}
            </td>
          </tr>

          <tr>
            <td style=""background:#ffffff; border-radius:14px; box-shadow:0 8px 28px rgba(0,0,0,.06);"">
              <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                <tr>
                  <td class=""p-sm"" style=""padding:24px 28px;"">
                    <h1 style=""margin:0 0 8px 0; font-size:22px; line-height:28px; color:{text};"">{WebUtility.HtmlEncode(subjectTitle)}</h1>
                    <p style=""margin:0 0 16px 0; color:{muted}; font-size:14px;"">Hola <b style=""color:{text}"">{safeGreeting}</b>,</p>

                    <div style=""color:{text}; font-size:14px; line-height:20px;"">
                      {bodyHtml}
                    </div>

                    {(string.IsNullOrWhiteSpace(actionText) || string.IsNullOrWhiteSpace(actionUrl) ? "" : $@"
                    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top:18px;"">
                      <tr>
                        <td align=""left"">
                          <a href=""{actionUrl}"" target=""_blank""
                             style=""display:inline-block; background:{primary}; color:#fff; text-decoration:none;
                                    padding:10px 16px; border-radius:10px; font-weight:600; font-size:14px;"">
                             {WebUtility.HtmlEncode(actionText)}
                          </a>
                        </td>
                      </tr>
                    </table>
                    ")}

                    <p style=""margin:24px 0 0 0; color:{muted}; font-size:12px;"">
                      Si no reconoces esta acción, ponte en contacto con el administrador de <b>La Original</b>.
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <tr>
            <td align=""center"" style=""padding:14px 8px; color:{muted}; font-size:12px;"">
              © {DateTime.Now.Year} La Original — Todos los derechos reservados.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }
    }
}

using LaOriginalBackend.Data;
using LaOriginalBackend.Models;
using LaOriginalBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace TuProyecto.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ GET: api/usuarios
        [HttpGet]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            return Ok(usuarios);
        }

        // ✅ GET: api/usuarios/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            return Ok(usuario);
        }

        // ✅ POST: api/usuarios
        [HttpPost]
        public async Task<IActionResult> CrearUsuario([FromBody] Usuario usuario)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1️⃣ Generar Username automático (ej: carllope)
            string baseUsername = (
                usuario.PrimerNombre.Substring(0, Math.Min(4, usuario.PrimerNombre.Length)) +
                usuario.PrimerApellido.Substring(0, Math.Min(4, usuario.PrimerApellido.Length))
            ).ToLower();

            string finalUsername = baseUsername;
            int contador = 1;

            while (await _context.Usuarios.AnyAsync(u => u.Username == finalUsername))
            {
                finalUsername = $"{baseUsername}{contador}";
                contador++;
            }
            usuario.Username = finalUsername;

            // 2️⃣ Hashear contraseña antes de guardar
            usuario.PasswordHash = PasswordHelper.HashPassword(usuario.PasswordHash);

            // 3️⃣ Guardar usuario
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // 4️⃣ Enviar credenciales al correo
            await EnviarCorreo(usuario.Email, usuario.Username, usuario.PasswordHash);

            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, usuario);
        }

        // ✅ PUT: api/usuarios/5
        [HttpPut("{id}")]
        public async Task<IActionResult> EditarUsuario(int id, [FromBody] Usuario usuarioActualizado)
        {
            if (id != usuarioActualizado.Id) return BadRequest();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.PrimerNombre = usuarioActualizado.PrimerNombre;
            usuario.SegundoNombre = usuarioActualizado.SegundoNombre;
            usuario.PrimerApellido = usuarioActualizado.PrimerApellido;
            usuario.SegundoApellido = usuarioActualizado.SegundoApellido;
            usuario.NIT = usuarioActualizado.NIT;
            usuario.CUI = usuarioActualizado.CUI;
            usuario.FechaNacimiento = usuarioActualizado.FechaNacimiento;
            usuario.FechaIngreso = usuarioActualizado.FechaIngreso;
            usuario.Celular = usuarioActualizado.Celular;
            usuario.Genero = usuarioActualizado.Genero;
            usuario.Estado = usuarioActualizado.Estado;
            usuario.Direccion = usuarioActualizado.Direccion;
            usuario.Cargo = usuarioActualizado.Cargo;
            usuario.Email = usuarioActualizado.Email;

            if (!string.IsNullOrEmpty(usuarioActualizado.PasswordHash))
            {
                usuario.PasswordHash = PasswordHelper.HashPassword(usuarioActualizado.PasswordHash);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ✅ DELETE: api/usuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 📧 Método auxiliar para enviar correo con credenciales
        private async Task EnviarCorreo(string emailDestino, string username, string passwordPlano)
        {
            try
            {
                using (var smtp = new SmtpClient("smtp.tuservidor.com", 587))
                {
                    smtp.Credentials = new NetworkCredential("tu_correo@dominio.com", "tu_password");
                    smtp.EnableSsl = true;

                    var mail = new MailMessage("tu_correo@dominio.com", emailDestino)
                    {
                        Subject = "Credenciales de acceso - La Original",
                        Body = $"Bienvenido a La Original!\n\nUsuario: {username}\nContraseña: {passwordPlano}\n\nPor favor cambie su contraseña después de iniciar sesión."
                    };

                    await smtp.SendMailAsync(mail);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo: {ex.Message}");
            }
        }
    }
}

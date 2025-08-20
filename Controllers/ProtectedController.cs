using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaOriginalBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProtectedController : ControllerBase
    {
        /// <summary>
        /// Este endpoint requiere token válido para poder acceder
        /// </summary>
        [HttpGet("datos-seguros")]
        [Authorize] // 🔹 Esto protege el endpoint con JWT
        public IActionResult GetSecureData()
        {
            return Ok(new
            {
                message = "Acceso concedido: tienes un token válido 🎉",
                fecha = DateTime.UtcNow,
                datosConfidenciales = "Aquí solo entran usuarios logueados"
            });
        }
    }
}

using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Controllers.Mantenimientos
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ClientesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClienteListDto>>> Get(
            [FromQuery] bool soloActivos = true,
            [FromQuery] string? term = null,
            [FromQuery] int take = 100)
        {
            var q = _db.Clientes.AsNoTracking();

            if (soloActivos) q = q.Where(c => c.Activo);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim().ToLower();
                q = q.Where(c =>
                    c.Nombre.ToLower().Contains(t) ||
                    (c.NIT != null && c.NIT.ToLower().Contains(t)) ||
                    (c.Telefono != null && c.Telefono.ToLower().Contains(t)));
            }

            var list = await q.OrderBy(c => c.Nombre)
                .Take(Math.Clamp(take, 1, 500))
                .Select(c => new ClienteListDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    NIT = c.NIT,
                    Telefono = c.Telefono,
                    Email = c.Email,
                    Direccion = c.Direccion,
                    Activo = c.Activo
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/clientes/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ClienteListDto>> GetById(int id)
        {
            var c = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return NotFound();

            return Ok(new ClienteListDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                NIT = c.NIT,
                Telefono = c.Telefono,
                Email = c.Email,
                Direccion = c.Direccion,
                Activo = c.Activo
            });
        }

        // POST: api/clientes
        [HttpPost]
        public async Task<ActionResult<ClienteListDto>> Create([FromBody] ClienteCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!string.IsNullOrWhiteSpace(dto.NIT) &&
                await _db.Clientes.AnyAsync(x => x.NIT == dto.NIT))
                return Conflict(new { message = "Ya existe un cliente con ese NIT." });

            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                await _db.Clientes.AnyAsync(x => x.Email == dto.Email))
                return Conflict(new { message = "Ya existe un cliente con ese Email." });

            var entity = new Cliente
            {
                Nombre = dto.Nombre.Trim(),
                NIT = dto.NIT?.Trim(),
                Telefono = dto.Telefono?.Trim(),
                Email = dto.Email?.Trim(),
                Direccion = dto.Direccion?.Trim(),
                Notas = dto.Notas?.Trim(),
                Activo = dto.Activo
            };

            _db.Clientes.Add(entity);
            await _db.SaveChangesAsync();

            var result = new ClienteListDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                NIT = entity.NIT,
                Telefono = entity.Telefono,
                Email = entity.Email,
                Direccion = entity.Direccion,
                Activo = entity.Activo
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        // PUT: api/clientes/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClienteUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var entity = await _db.Clientes.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.NIT) &&
                await _db.Clientes.AnyAsync(x => x.Id != id && x.NIT == dto.NIT))
                return Conflict(new { message = "Ya existe otro cliente con ese NIT." });

            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                await _db.Clientes.AnyAsync(x => x.Id != id && x.Email == dto.Email))
                return Conflict(new { message = "Ya existe otro cliente con ese Email." });

            entity.Nombre = dto.Nombre.Trim();
            entity.NIT = dto.NIT?.Trim();
            entity.Telefono = dto.Telefono?.Trim();
            entity.Email = dto.Email?.Trim();
            entity.Direccion = dto.Direccion?.Trim();
            entity.Notas = dto.Notas?.Trim();
            entity.Activo = dto.Activo;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> Toggle(int id, [FromBody] ClienteToggleDto dto)
        {
            var entity = await _db.Clientes.FindAsync(id);
            if (entity is null) return NotFound();

            entity.Activo = dto.Activo;
            await _db.SaveChangesAsync();

            return Ok(new { entity.Id, entity.Nombre, entity.Activo });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Clientes.FindAsync(id);
            if (entity is null) return NotFound();

            _db.Clientes.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

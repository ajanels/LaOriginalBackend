using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Controllers.Mantenimientos
{
    [Authorize(Roles = "Administrador,Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class EstadosController : ControllerBase
    {
        private readonly AppDbContext _db;
        public EstadosController(AppDbContext db) { _db = db; }

        // GET: api/estados?tipo=Pedido
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? tipo)
        {
            var query = _db.Estados.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(tipo))
                query = query.Where(e => e.Tipo == tipo);

            var list = await query
                .OrderBy(e => e.Tipo).ThenBy(e => e.Nombre)
                .Select(e => new EstadoListDto
                {
                    Id = e.Id,
                    Tipo = e.Tipo,
                    Nombre = e.Nombre,
                    Activo = e.Activo
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/estados/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var e = await _db.Estados.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (e == null) return NotFound();

            return Ok(new EstadoUpsertDto
            {
                Tipo = e.Tipo,
                Nombre = e.Nombre,
                Activo = e.Activo,
                Notas = e.Notas
            });
        }

        // POST: api/estados
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EstadoUpsertDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var exists = await _db.Estados.AnyAsync(x => x.Tipo == dto.Tipo && x.Nombre == dto.Nombre);
            if (exists) return Conflict("Ya existe un estado con ese Tipo y Nombre.");

            var e = new Estado
            {
                Tipo = dto.Tipo.Trim(),
                Nombre = dto.Nombre.Trim(),
                Activo = dto.Activo,
                Notas = dto.Notas
            };

            _db.Estados.Add(e);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = e.Id }, new { e.Id });
        }

        // PUT: api/estados/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] EstadoUpsertDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var e = await _db.Estados.FirstOrDefaultAsync(x => x.Id == id);
            if (e == null) return NotFound();

            var duplicate = await _db.Estados.AnyAsync(x =>
                x.Id != id && x.Tipo == dto.Tipo && x.Nombre == dto.Nombre);
            if (duplicate) return Conflict("Ya existe otro estado con ese Tipo y Nombre.");

            e.Tipo = dto.Tipo.Trim();
            e.Nombre = dto.Nombre.Trim();
            e.Activo = dto.Activo;
            e.Notas = dto.Notas;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/estados/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _db.Estados.FirstOrDefaultAsync(x => x.Id == id);
            if (e == null) return NotFound();

            _db.Estados.Remove(e);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("No se puede eliminar: está referenciado por otros registros.");
            }

            return NoContent();
        }
    }
}

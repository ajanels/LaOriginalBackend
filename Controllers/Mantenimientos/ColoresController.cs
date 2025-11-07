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
    public class ColoresController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ColoresController(AppDbContext db) => _db = db;

        // GET: api/colores?soloActivos=true&term=rojo
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ColorListDto>>> Get(
            [FromQuery] bool soloActivos = true, [FromQuery] string? term = null)
        {
            var q = _db.Colores.AsNoTracking();
            if (soloActivos) q = q.Where(x => x.Activo);
            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term.Trim().ToLower();
                q = q.Where(x => x.Nombre.ToLower().Contains(term));
            }

            var list = await q
                .OrderBy(x => x.Nombre)
                .Select(x => new ColorListDto
                {
                    Id = x.Id,
                    Nombre = x.Nombre,
                    Hex = x.Hex,
                    Activo = x.Activo,
                    Notas = x.Notas
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/colores/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ColorListDto>> GetById(int id)
        {
            var c = await _db.Colores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return NotFound();

            return Ok(new ColorListDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Hex = c.Hex,
                Activo = c.Activo,
                Notas = c.Notas
            });
        }

        // POST: api/colores
        [HttpPost]
        public async Task<ActionResult<ColorListDto>> Create([FromBody] ColorCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var name = dto.Nombre.Trim();
            var exists = await _db.Colores.AnyAsync(x => x.Nombre.ToLower() == name.ToLower());
            if (exists) return Conflict(new { message = "Ya existe un color con ese nombre." });

            var entity = new Color
            {
                Nombre = name,
                Hex = string.IsNullOrWhiteSpace(dto.Hex) ? null : (dto.Hex.StartsWith("#") ? dto.Hex : $"#{dto.Hex}"),
                Activo = dto.Activo,
                Notas = dto.Notas?.Trim()
            };

            _db.Colores.Add(entity);
            await _db.SaveChangesAsync();

            var result = new ColorListDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Hex = entity.Hex,
                Activo = entity.Activo,
                Notas = entity.Notas
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        // PUT: api/colores/5
        [Authorize(Roles = "Administrador,Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ColorUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var entity = await _db.Colores.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return NotFound();

            var exists = await _db.Colores.AnyAsync(x =>
                x.Id != id && x.Nombre.ToLower() == dto.Nombre.Trim().ToLower());
            if (exists) return Conflict(new { message = "Ya existe otro color con ese nombre." });

            entity.Nombre = dto.Nombre.Trim();
            entity.Hex = string.IsNullOrWhiteSpace(dto.Hex) ? null : (dto.Hex.StartsWith("#") ? dto.Hex : $"#{dto.Hex}");
            entity.Activo = dto.Activo;
            entity.Notas = dto.Notas?.Trim();

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // PATCH: api/colores/5/estado
        [Authorize(Roles = "Administrador,Admin")]
        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> Toggle(int id, [FromBody] ColorToggleDto dto)
        {
            var entity = await _db.Colores.FindAsync(id);
            if (entity is null) return NotFound();

            entity.Activo = dto.Activo;
            await _db.SaveChangesAsync();

            return Ok(new { entity.Id, entity.Nombre, entity.Activo });
        }

        // DELETE: api/colores/5
        [Authorize(Roles = "Administrador,Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Colores.FindAsync(id);
            if (entity is null) return NotFound();

            _db.Colores.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

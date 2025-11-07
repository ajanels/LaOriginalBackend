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
    public class UnidadesMedidaController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UnidadesMedidaController(AppDbContext db) => _db = db;

        // GET: api/unidadesmedida?soloActivos=true&term=kg
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UnidadMedidaListDto>>> Get([FromQuery] bool soloActivos = true, [FromQuery] string? term = null)
        {
            var q = _db.Unidades.AsNoTracking();

            if (soloActivos) q = q.Where(u => u.Activo);
            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term.Trim().ToLower();
                q = q.Where(u =>
                    u.Nombre.ToLower().Contains(term) ||
                    u.Simbolo.ToLower().Contains(term));
            }

            var list = await q
                .OrderBy(u => u.Nombre)
                .Select(u => new UnidadMedidaListDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Simbolo = u.Simbolo,
                    Descripcion = u.Descripcion,
                    Activo = u.Activo
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/unidadesmedida/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UnidadMedidaListDto>> GetById(int id)
        {
            var e = await _db.Unidades.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (e is null) return NotFound();

            return Ok(new UnidadMedidaListDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Simbolo = e.Simbolo,
                Descripcion = e.Descripcion,
                Activo = e.Activo
            });
        }

        // POST: api/unidadesmedida
        [HttpPost]
        public async Task<ActionResult<UnidadMedidaListDto>> Create([FromBody] UnidadMedidaCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var nombre = dto.Nombre.Trim();
            var simbolo = dto.Simbolo.Trim();

            // Unicidad por símbolo (ya hay índice único en DB)
            var existsSimbolo = await _db.Unidades.AnyAsync(x => x.Simbolo.ToLower() == simbolo.ToLower());
            if (existsSimbolo) return Conflict(new { message = "Ya existe una unidad con ese símbolo." });

            var entity = new UnidadMedida
            {
                Nombre = nombre,
                Simbolo = simbolo,
                Descripcion = dto.Descripcion?.Trim(),
                Activo = dto.Activo
            };

            _db.Unidades.Add(entity);
            await _db.SaveChangesAsync();

            var result = new UnidadMedidaListDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Simbolo = entity.Simbolo,
                Descripcion = entity.Descripcion,
                Activo = entity.Activo
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        // PUT: api/unidadesmedida/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UnidadMedidaUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var entity = await _db.Unidades.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return NotFound();

            var nombre = dto.Nombre.Trim();
            var simbolo = dto.Simbolo.Trim();

            // Evitar duplicados de símbolo en otros registros
            var existsSimbolo = await _db.Unidades.AnyAsync(x =>
                x.Id != id && x.Simbolo.ToLower() == simbolo.ToLower());
            if (existsSimbolo) return Conflict(new { message = "Ya existe otra unidad con ese símbolo." });

            entity.Nombre = nombre;
            entity.Simbolo = simbolo;
            entity.Descripcion = dto.Descripcion?.Trim();
            entity.Activo = dto.Activo;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // PATCH: api/unidadesmedida/5/estado
        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> Toggle(int id, [FromBody] UnidadMedidaToggleDto dto)
        {
            var entity = await _db.Unidades.FindAsync(id);
            if (entity is null) return NotFound();

            entity.Activo = dto.Activo;
            await _db.SaveChangesAsync();

            return Ok(new { entity.Id, entity.Nombre, entity.Activo });
        }

        // DELETE: api/unidadesmedida/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Unidades.FindAsync(id);
            if (entity is null) return NotFound();

            // TODO: cuando exista Productos, validar referencia antes de eliminar.
            _db.Unidades.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

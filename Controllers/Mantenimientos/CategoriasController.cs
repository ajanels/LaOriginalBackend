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
    public class CategoriasController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CategoriasController(AppDbContext db) => _db = db;

        // GET: api/categorias?soloActivos=true&term=hi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaListDto>>> Get([FromQuery] bool soloActivos = true, [FromQuery] string? term = null)
        {
            var q = _db.Categorias.AsNoTracking();

            if (soloActivos) q = q.Where(c => c.Activo);
            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term.Trim().ToLower();
                q = q.Where(c => c.Nombre.ToLower().Contains(term));
            }

            var list = await q
                .OrderBy(c => c.Nombre)
                .Select(c => new CategoriaListDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    Activo = c.Activo
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/categorias/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoriaListDto>> GetById(int id)
        {
            var c = await _db.Categorias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return NotFound();

            return Ok(new CategoriaListDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Descripcion = c.Descripcion,
                Activo = c.Activo
            });
        }

        // POST: api/categorias
        [HttpPost]
        public async Task<ActionResult<CategoriaListDto>> Create([FromBody] CategoriaCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var name = dto.Nombre.Trim();
            var exists = await _db.Categorias.AnyAsync(x => x.Nombre.ToLower() == name.ToLower());
            if (exists) return Conflict(new { message = "Ya existe una categoría con ese nombre." });

            var entity = new Categoria
            {
                Nombre = name,
                Descripcion = dto.Descripcion?.Trim(),
                Activo = dto.Activo
            };

            _db.Categorias.Add(entity);
            await _db.SaveChangesAsync();

            var result = new CategoriaListDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion,
                Activo = entity.Activo
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        // PUT: api/categorias/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoriaUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var entity = await _db.Categorias.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return NotFound();

            var exists = await _db.Categorias.AnyAsync(x =>
                x.Id != id && x.Nombre.ToLower() == dto.Nombre.Trim().ToLower());
            if (exists) return Conflict(new { message = "Ya existe otra categoría con ese nombre." });

            entity.Nombre = dto.Nombre.Trim();
            entity.Descripcion = dto.Descripcion?.Trim();
            entity.Activo = dto.Activo;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // PATCH: api/categorias/5/estado
        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> Toggle(int id, [FromBody] CategoriaToggleDto dto)
        {
            var entity = await _db.Categorias.FindAsync(id);
            if (entity is null) return NotFound();

            entity.Activo = dto.Activo;
            await _db.SaveChangesAsync();

            return Ok(new { entity.Id, entity.Nombre, entity.Activo });
        }

        // DELETE: api/categorias/5
        [Authorize(Roles = "Administrador,Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Categorias.FindAsync(id);
            if (entity is null) return NotFound();

            _db.Categorias.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

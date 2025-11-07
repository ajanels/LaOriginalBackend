using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Controllers.Mantenimientos;

[ApiController]
[Route("api/[controller]")]
public class MarcasController : ControllerBase
{
    private readonly AppDbContext _db;
    public MarcasController(AppDbContext db) => _db = db;

    // GET: api/marcas?soloActivos=true&term=tex
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MarcaListDto>>> Get([FromQuery] bool soloActivos = true, [FromQuery] string? term = null)
    {
        var q = _db.Marcas.AsNoTracking();

        if (soloActivos) q = q.Where(m => m.Activo);
        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim().ToLower();
            q = q.Where(m => m.Nombre.ToLower().Contains(term));
        }

        var list = await q
            .OrderBy(m => m.Nombre)
            .Select(m => new MarcaListDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Descripcion = m.Descripcion,
                Activo = m.Activo
            })
            .ToListAsync();

        return Ok(list);
    }

    // GET: api/marcas/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MarcaListDto>> GetById(int id)
    {
        var m = await _db.Marcas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return NotFound();

        return Ok(new MarcaListDto
        {
            Id = m.Id,
            Nombre = m.Nombre,
            Descripcion = m.Descripcion,
            Activo = m.Activo
        });
    }

    // POST: api/marcas
    [HttpPost]
    public async Task<ActionResult<MarcaListDto>> Create([FromBody] MarcaCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var name = dto.Nombre.Trim();
        var exists = await _db.Marcas.AnyAsync(x => x.Nombre.ToLower() == name.ToLower());
        if (exists) return Conflict(new { message = "Ya existe una marca con ese nombre." });

        var entity = new Marca
        {
            Nombre = name,
            Descripcion = dto.Descripcion?.Trim(),
            Activo = dto.Activo
        };

        _db.Marcas.Add(entity);
        await _db.SaveChangesAsync();

        var result = new MarcaListDto
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            Descripcion = entity.Descripcion,
            Activo = entity.Activo
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
    }

    // PUT: api/marcas/5
    [Authorize(Roles = "Administrador,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] MarcaUpdateDto dto)
    {
        if (id != dto.Id) return BadRequest();

        var entity = await _db.Marcas.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return NotFound();

        var exists = await _db.Marcas.AnyAsync(x =>
            x.Id != id && x.Nombre.ToLower() == dto.Nombre.Trim().ToLower());
        if (exists) return Conflict(new { message = "Ya existe otra marca con ese nombre." });

        entity.Nombre = dto.Nombre.Trim();
        entity.Descripcion = dto.Descripcion?.Trim();
        entity.Activo = dto.Activo;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PATCH: api/marcas/5/estado
    [Authorize(Roles = "Administrador,Admin")]
    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> Toggle(int id, [FromBody] MarcaToggleDto dto)
    {
        var entity = await _db.Marcas.FindAsync(id);
        if (entity is null) return NotFound();

        entity.Activo = dto.Activo;
        await _db.SaveChangesAsync();

        return Ok(new { entity.Id, entity.Nombre, entity.Activo });
    }

    // DELETE: api/marcas/5
    [Authorize(Roles = "Administrador,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Cuando existan Productos, valida referencias aquí (FK).
        var entity = await _db.Marcas.FindAsync(id);
        if (entity is null) return NotFound();

        _db.Marcas.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

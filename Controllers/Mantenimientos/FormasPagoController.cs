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
    public class FormasPagoController : ControllerBase
    {
        private readonly AppDbContext _db;
        public FormasPagoController(AppDbContext db) => _db = db;

        // GET: api/formaspago?soloActivos=true&term=tar
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FormaPagoListDto>>> Get(
            [FromQuery] bool soloActivos = true,
            [FromQuery] string? term = null)
        {
            var q = _db.FormasPago.AsNoTracking();

            if (soloActivos) q = q.Where(f => f.Activo);
            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term.Trim().ToLower();
                q = q.Where(f => f.Nombre.ToLower().Contains(term));
            }

            var list = await q
                .OrderBy(f => f.Nombre)
                .Select(f => new FormaPagoListDto
                {
                    Id = f.Id,
                    Nombre = f.Nombre,
                    Descripcion = f.Descripcion,
                    Activo = f.Activo,
                    RequiereReferencia = f.RequiereReferencia,
                    AfectaCaja = f.AfectaCaja,
                    AfectaBanco = f.AfectaBanco,
                    EsCredito = f.EsCredito
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/formaspago/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<FormaPagoListDto>> GetById(int id)
        {
            var f = await _db.FormasPago.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (f is null) return NotFound();

            return Ok(new FormaPagoListDto
            {
                Id = f.Id,
                Nombre = f.Nombre,
                Descripcion = f.Descripcion,
                Activo = f.Activo,
                RequiereReferencia = f.RequiereReferencia,
                AfectaCaja = f.AfectaCaja,
                AfectaBanco = f.AfectaBanco,
                EsCredito = f.EsCredito
            });
        }

        // POST: api/formaspago
        [Authorize(Roles = "Administrador,Admin")]
        [HttpPost]
        public async Task<ActionResult<FormaPagoListDto>> Create([FromBody] FormaPagoCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var name = dto.Nombre.Trim();
            var exists = await _db.FormasPago.AnyAsync(x => x.Nombre.ToLower() == name.ToLower());
            if (exists) return Conflict(new { message = "Ya existe una forma de pago con ese nombre." });

            var entity = new FormaPago
            {
                Nombre = name,
                Descripcion = dto.Descripcion?.Trim(),
                Activo = dto.Activo,
                RequiereReferencia = dto.RequiereReferencia,
                AfectaCaja = dto.AfectaCaja,
                AfectaBanco = dto.AfectaBanco,
                EsCredito = dto.EsCredito
            };

            _db.FormasPago.Add(entity);
            await _db.SaveChangesAsync();

            var result = new FormaPagoListDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion,
                Activo = entity.Activo,
                RequiereReferencia = entity.RequiereReferencia,
                AfectaCaja = entity.AfectaCaja,
                AfectaBanco = entity.AfectaBanco,
                EsCredito = entity.EsCredito
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        // PUT: api/formaspago/5
        [Authorize(Roles = "Administrador,Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] FormaPagoUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var entity = await _db.FormasPago.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return NotFound();

            var exists = await _db.FormasPago.AnyAsync(x =>
                x.Id != id && x.Nombre.ToLower() == dto.Nombre.Trim().ToLower());
            if (exists) return Conflict(new { message = "Ya existe otra forma de pago con ese nombre." });

            entity.Nombre = dto.Nombre.Trim();
            entity.Descripcion = dto.Descripcion?.Trim();
            entity.Activo = dto.Activo;
            entity.RequiereReferencia = dto.RequiereReferencia;
            entity.AfectaCaja = dto.AfectaCaja;
            entity.AfectaBanco = dto.AfectaBanco;
            entity.EsCredito = dto.EsCredito;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // PATCH: api/formaspago/5/estado
        [Authorize(Roles = "Administrador,Admin")]
        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> Toggle(int id, [FromBody] FormaPagoToggleDto dto)
        {
            var entity = await _db.FormasPago.FindAsync(id);
            if (entity is null) return NotFound();

            entity.Activo = dto.Activo;
            await _db.SaveChangesAsync();

            return Ok(new { entity.Id, entity.Nombre, entity.Activo });
        }

        // DELETE: api/formaspago/5
        [Authorize(Roles = "Administrador,Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Nota: validar referencias antes de eliminar en producción.
            var entity = await _db.FormasPago.FindAsync(id);
            if (entity is null) return NotFound();

            _db.FormasPago.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

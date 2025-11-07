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
    public class ProveedoresController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ProveedoresController(AppDbContext db) => _db = db;

        // GET: api/proveedores?soloActivos=true&term=tex
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProveedorListDto>>> Get(
            [FromQuery] bool soloActivos = true,
            [FromQuery] string? term = null)
        {
            var q = _db.Proveedores.AsNoTracking();

            if (soloActivos) q = q.Where(p => p.Activo);

            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term.Trim().ToLower();
                q = q.Where(p =>
                    p.Nombre.ToLower().Contains(term) ||
                    (p.NIT != null && p.NIT.ToLower().Contains(term)));
            }

            var list = await q
                .OrderBy(p => p.Nombre)
                .Select(p => new ProveedorListDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    NIT = p.NIT,
                    Contacto = p.Contacto,
                    Telefono = p.Telefono,
                    Email = p.Email,
                    Activo = p.Activo
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/proveedores/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProveedorDetailDto>> GetById(int id)
        {
            var p = await _db.Proveedores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound();

            return Ok(new ProveedorDetailDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                NIT = p.NIT,
                Contacto = p.Contacto,
                Telefono = p.Telefono,
                Email = p.Email,
                Direccion = p.Direccion,
                Notas = p.Notas,
                Activo = p.Activo
            });
        }

        // POST: api/proveedores
        [HttpPost]
        public async Task<ActionResult<ProveedorDetailDto>> Create([FromBody] ProveedorCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var name = dto.Nombre.Trim();
            var nit = dto.NIT.Trim().ToUpper();

            var dupNombre = await _db.Proveedores.AnyAsync(x => x.Nombre.ToLower() == name.ToLower());
            if (dupNombre) return Conflict(new { message = "Ya existe un proveedor con ese nombre." });

            var dupNit = await _db.Proveedores.AnyAsync(x => x.NIT.ToUpper() == nit);
            if (dupNit) return Conflict(new { message = "Ya existe un proveedor con ese NIT." });

            var entity = new Proveedor
            {
                Nombre = name,
                NIT = nit,
                Contacto = dto.Contacto?.Trim(),
                Telefono = dto.Telefono?.Trim(),
                Email = dto.Email?.Trim(),
                Direccion = dto.Direccion?.Trim(),
                Notas = dto.Notas?.Trim(),
                Activo = dto.Activo
            };

            _db.Proveedores.Add(entity);
            await _db.SaveChangesAsync();

            var result = new ProveedorDetailDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                NIT = entity.NIT,
                Contacto = entity.Contacto,
                Telefono = entity.Telefono,
                Email = entity.Email,
                Direccion = entity.Direccion,
                Notas = entity.Notas,
                Activo = entity.Activo
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        // PUT: api/proveedores/5
        [Authorize(Roles = "Administrador,Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProveedorUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var entity = await _db.Proveedores.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return NotFound();

            var name = dto.Nombre.Trim();
            var nit = dto.NIT.Trim().ToUpper();

            var dupNombre = await _db.Proveedores.AnyAsync(x =>
                x.Id != id && x.Nombre.ToLower() == name.ToLower());
            if (dupNombre) return Conflict(new { message = "Ya existe otro proveedor con ese nombre." });

            var dupNit = await _db.Proveedores.AnyAsync(x =>
                x.Id != id && x.NIT.ToUpper() == nit);
            if (dupNit) return Conflict(new { message = "Ya existe otro proveedor con ese NIT." });

            entity.Nombre = name;
            entity.NIT = nit;
            entity.Contacto = dto.Contacto?.Trim();
            entity.Telefono = dto.Telefono?.Trim();
            entity.Email = dto.Email?.Trim();
            entity.Direccion = dto.Direccion?.Trim();
            entity.Notas = dto.Notas?.Trim();
            entity.Activo = dto.Activo;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // PATCH: api/proveedores/5/estado
        [Authorize(Roles = "Administrador,Admin")]
        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> Toggle(int id, [FromBody] ProveedorToggleDto dto)
        {
            var entity = await _db.Proveedores.FindAsync(id);
            if (entity is null) return NotFound();

            entity.Activo = dto.Activo;
            await _db.SaveChangesAsync();

            return Ok(new { entity.Id, entity.Nombre, entity.Activo });
        }

        // DELETE: api/proveedores/5
        [Authorize(Roles = "Administrador,Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            // TODO: cuando existan compras/pedidos, validar referencias antes de eliminar.
            var entity = await _db.Proveedores.FindAsync(id);
            if (entity is null) return NotFound();

            _db.Proveedores.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

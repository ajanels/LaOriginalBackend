// Controllers/RolesController.cs
using LaOriginalBackend.Data;
using LaOriginalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Controllers;

[Authorize(Roles = "Administrador,Admin")]
[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _context;
    public RolesController(AppDbContext context) => _context = context;

    // GET /api/roles?soloActivos=true
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool soloActivos = true)
    {
        var q = _context.Roles.AsNoTracking();
        if (soloActivos) q = q.Where(r => r.Activo);

        var list = await q
            .OrderBy(r => r.Nombre)
            .Select(r => new RolDto
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                Activo = r.Activo
            })
            .ToListAsync();

        return Ok(list);
    }

    // GET /api/roles/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var rol = await _context.Roles.AsNoTracking()
            .Select(r => new RolDto
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                Activo = r.Activo
            })
            .FirstOrDefaultAsync(r => r.Id == id);

        return rol is null ? NotFound() : Ok(rol);
    }

    // POST /api/roles
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RolCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var name = dto.Nombre.Trim();
        var exists = await _context.Roles.AnyAsync(r => r.Nombre.ToLower() == name.ToLower());
        if (exists) return Conflict(new { message = "Ya existe un rol con ese nombre." });

        var rol = new Rol
        {
            Nombre = name,
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            Activo = dto.Activo
        };

        _context.Roles.Add(rol);
        await _context.SaveChangesAsync();

        var result = new RolDto
        {
            Id = rol.Id,
            Nombre = rol.Nombre,
            Descripcion = rol.Descripcion,
            Activo = rol.Activo
        };

        return CreatedAtAction(nameof(GetById), new { id = rol.Id }, result);
    }

    // PUT /api/roles/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] RolUpdateDto dto)
    {
        if (id != dto.Id) return BadRequest();

        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);
        if (rol is null) return NotFound();

        var exists = await _context.Roles.AnyAsync(r =>
            r.Id != id && r.Nombre.ToLower() == dto.Nombre.Trim().ToLower());
        if (exists) return Conflict(new { message = "Ya existe otro rol con ese nombre." });

        rol.Nombre = dto.Nombre.Trim();
        rol.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        rol.Activo = dto.Activo;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PATCH /api/roles/5/estado   (ahora cambia el bool Activo)
    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> ChangeEstado(int id, [FromBody] ChangeEstadoDto dto)
    {
        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);
        if (rol is null) return NotFound();

        rol.Activo = dto.Activo;
        await _context.SaveChangesAsync();

        return Ok(new { rol.Id, rol.Activo });
    }

    // DELETE /api/roles/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        // seguridad: no permitir eliminar si hay usuarios con este rol
        var hasUsers = await _context.Usuarios.AnyAsync(u => u.RolId == id);
        if (hasUsers)
            return Conflict(new { message = "No se puede eliminar: hay usuarios asociados. Marque el rol como inactivo." });

        var rol = await _context.Roles.FindAsync(id);
        if (rol is null) return NotFound();

        _context.Roles.Remove(rol);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

/* ===== DTOs ===== */

public class RolDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public string Estado => Activo ? "Activo" : "Inactivo"; // útil para UI
}

public class RolCreateDto
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Nombre { get; set; } = null!;

    [StringLength(200)]
    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}

public class RolUpdateDto
{
    [Required] public int Id { get; set; }

    [Required, StringLength(50, MinimumLength = 3)]
    public string Nombre { get; set; } = null!;

    [StringLength(200)]
    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}

public class ChangeEstadoDto
{
    public bool Activo { get; set; }
}

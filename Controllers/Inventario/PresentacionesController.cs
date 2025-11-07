using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Controllers.Inventario;

[ApiController]
[Route("api")]
public class PresentacionesController : ControllerBase
{
    private readonly AppDbContext _db;
    public PresentacionesController(AppDbContext db) => _db = db;

    // GET: api/productos/5/presentaciones
    [HttpGet("productos/{productoId:int}/presentaciones")]
    public async Task<ActionResult<IEnumerable<PresentacionListDto>>> ListByProducto(int productoId)
    {
        var exists = await _db.Productos.AsNoTracking().AnyAsync(p => p.Id == productoId);
        if (!exists) return NotFound(new { message = "Producto no encontrado" });

        var list = await _db.Presentaciones
            .AsNoTracking()
            .Where(pr => pr.ProductoId == productoId)
            .Include(pr => pr.Unidad)
            .Include(pr => pr.Color)
            .OrderByDescending(pr => pr.EsPrincipal).ThenBy(pr => pr.Nombre)
            .Select(pr => new PresentacionListDto
            {
                Id = pr.Id,
                ProductoId = pr.ProductoId,
                Nombre = pr.Nombre,
                UnidadMedidaId = pr.UnidadMedidaId,
                Unidad = pr.Unidad.Nombre,
                Factor = pr.Factor,
                SKU = pr.SKU,
                CodigoBarras = pr.CodigoBarras,
                PrecioCompraDefault = pr.PrecioCompraDefault,
                PrecioVentaDefault = pr.PrecioVentaDefault,
                ColorId = pr.ColorId,
                Color = pr.Color != null ? pr.Color.Nombre : null,
                Activo = pr.Activo,
                EsPrincipal = pr.EsPrincipal
            })
            .ToListAsync();

        return Ok(list);
    }

    // POST: api/productos/5/presentaciones
    [Authorize(Roles = "Administrador,Admin")]
    [HttpPost("productos/{productoId:int}/presentaciones")]
    public async Task<ActionResult<PresentacionListDto>> Create(int productoId, [FromBody] PresentacionCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (productoId != dto.ProductoId) return BadRequest(new { message = "ProductoId no coincide con la ruta." });

        var prodExists = await _db.Productos.AnyAsync(p => p.Id == productoId);
        if (!prodExists) return NotFound(new { message = "Producto no encontrado" });

        // Validaciones de unicidad
        var nameExists = await _db.Presentaciones.AnyAsync(pr =>
            pr.ProductoId == productoId && pr.Nombre.ToLower() == dto.Nombre.Trim().ToLower());
        if (nameExists) return Conflict(new { message = "Ya existe una presentación con ese nombre para este producto." });

        if (!string.IsNullOrWhiteSpace(dto.SKU))
        {
            var skuExists = await _db.Presentaciones.AnyAsync(pr => pr.SKU == dto.SKU);
            if (skuExists) return Conflict(new { message = "El SKU ya está en uso." });
        }
        if (!string.IsNullOrWhiteSpace(dto.CodigoBarras))
        {
            var cbExists = await _db.Presentaciones.AnyAsync(pr => pr.CodigoBarras == dto.CodigoBarras);
            if (cbExists) return Conflict(new { message = "El código de barras ya está en uso." });
        }

        var entity = new Presentacion
        {
            ProductoId = productoId,
            Nombre = dto.Nombre.Trim(),
            UnidadMedidaId = dto.UnidadMedidaId,
            Factor = dto.Factor <= 0 ? 1m : dto.Factor,
            SKU = dto.SKU?.Trim(),
            CodigoBarras = dto.CodigoBarras?.Trim(),
            PrecioCompraDefault = dto.PrecioCompraDefault,
            PrecioVentaDefault = dto.PrecioVentaDefault,
            ColorId = dto.ColorId,
            Activo = dto.Activo,
            EsPrincipal = dto.EsPrincipal
        };

        // Garantizar única principal
        if (entity.EsPrincipal)
        {
            var otrosPrincipales = await _db.Presentaciones
                .Where(x => x.ProductoId == productoId && x.EsPrincipal)
                .ToListAsync();
            if (otrosPrincipales.Any())
            {
                foreach (var pr in otrosPrincipales) pr.EsPrincipal = false;
                await _db.SaveChangesAsync(); // desmarca primero
            }
        }

        _db.Presentaciones.Add(entity);
        await _db.SaveChangesAsync();

        // Proyección de respuesta
        var dtoOut = await _db.Presentaciones
            .AsNoTracking()
            .Include(x => x.Unidad)
            .Include(x => x.Color)
            .Where(x => x.Id == entity.Id)
            .Select(pr => new PresentacionListDto
            {
                Id = pr.Id,
                ProductoId = pr.ProductoId,
                Nombre = pr.Nombre,
                UnidadMedidaId = pr.UnidadMedidaId,
                Unidad = pr.Unidad.Nombre,
                Factor = pr.Factor,
                SKU = pr.SKU,
                CodigoBarras = pr.CodigoBarras,
                PrecioCompraDefault = pr.PrecioCompraDefault,
                PrecioVentaDefault = pr.PrecioVentaDefault,
                ColorId = pr.ColorId,
                Color = pr.Color != null ? pr.Color.Nombre : null,
                Activo = pr.Activo,
                EsPrincipal = pr.EsPrincipal
            })
            .SingleAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = entity.Id },
            dtoOut
        );
    }

    // GET: api/presentaciones/123
    [HttpGet("presentaciones/{id:int}")]
    public async Task<ActionResult<PresentacionListDto>> GetById(int id)
    {
        var pr = await _db.Presentaciones
            .AsNoTracking()
            .Include(x => x.Unidad)
            .Include(x => x.Color)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (pr is null) return NotFound();

        return Ok(new PresentacionListDto
        {
            Id = pr.Id,
            ProductoId = pr.ProductoId,
            Nombre = pr.Nombre,
            UnidadMedidaId = pr.UnidadMedidaId,
            Unidad = pr.Unidad.Nombre,
            Factor = pr.Factor,
            SKU = pr.SKU,
            CodigoBarras = pr.CodigoBarras,
            PrecioCompraDefault = pr.PrecioCompraDefault,
            PrecioVentaDefault = pr.PrecioVentaDefault,
            ColorId = pr.ColorId,
            Color = pr.Color != null ? pr.Color.Nombre : null,
            Activo = pr.Activo,
            EsPrincipal = pr.EsPrincipal
        });
    }

    // PUT: api/presentaciones/123
    [Authorize(Roles = "Administrador,Admin")]
    [HttpPut("presentaciones/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PresentacionUpdateDto dto)
    {
        if (id != dto.Id) return BadRequest();

        var pr = await _db.Presentaciones.FirstOrDefaultAsync(x => x.Id == id);
        if (pr is null) return NotFound();

        // Validar nombre único por producto
        var nameExists = await _db.Presentaciones.AnyAsync(x =>
            x.ProductoId == pr.ProductoId &&
            x.Id != id &&
            x.Nombre.ToLower() == dto.Nombre.Trim().ToLower());
        if (nameExists) return Conflict(new { message = "Ya existe otra presentación con ese nombre para este producto." });

        // SKU / Código de barras únicos globalmente
        if (!string.IsNullOrWhiteSpace(dto.SKU))
        {
            var skuExists = await _db.Presentaciones.AnyAsync(x => x.Id != id && x.SKU == dto.SKU);
            if (skuExists) return Conflict(new { message = "El SKU ya está en uso." });
        }
        if (!string.IsNullOrWhiteSpace(dto.CodigoBarras))
        {
            var cbExists = await _db.Presentaciones.AnyAsync(x => x.Id != id && x.CodigoBarras == dto.CodigoBarras);
            if (cbExists) return Conflict(new { message = "El código de barras ya está en uso." });
        }

        // Si se volverá principal, primero desmarcar otras principales
        if (dto.EsPrincipal && !pr.EsPrincipal)
        {
            var otrosPrincipales = await _db.Presentaciones
                .Where(x => x.ProductoId == pr.ProductoId && x.EsPrincipal && x.Id != pr.Id)
                .ToListAsync();
            if (otrosPrincipales.Any())
            {
                foreach (var x in otrosPrincipales) x.EsPrincipal = false;
                await _db.SaveChangesAsync();
            }
        }

        pr.Nombre = dto.Nombre.Trim();
        pr.UnidadMedidaId = dto.UnidadMedidaId;
        pr.Factor = dto.Factor <= 0 ? 1m : dto.Factor;
        pr.SKU = dto.SKU?.Trim();
        pr.CodigoBarras = dto.CodigoBarras?.Trim();
        pr.PrecioCompraDefault = dto.PrecioCompraDefault;
        pr.PrecioVentaDefault = dto.PrecioVentaDefault;
        pr.ColorId = dto.ColorId;
        pr.Activo = dto.Activo;
        pr.EsPrincipal = dto.EsPrincipal;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PATCH: api/presentaciones/123/estado
    [Authorize(Roles = "Administrador,Admin")]
    [HttpPatch("presentaciones/{id:int}/estado")]
    public async Task<IActionResult> Toggle(int id, [FromBody] PresentacionToggleDto dto)
    {
        var pr = await _db.Presentaciones.FindAsync(id);
        if (pr is null) return NotFound();

        pr.Activo = dto.Activo;
        await _db.SaveChangesAsync();

        return Ok(new { pr.Id, pr.Activo });
    }

    // PATCH: api/presentaciones/123/principal
    [Authorize(Roles = "Administrador,Admin")]
    [HttpPatch("presentaciones/{id:int}/principal")]
    public async Task<IActionResult> SetPrincipal(int id)
    {
        var pr = await _db.Presentaciones.FindAsync(id);
        if (pr is null) return NotFound();

        // Desmarcar otros primero, guardar, luego marcar este (evitar índice único)
        var otros = await _db.Presentaciones
            .Where(x => x.ProductoId == pr.ProductoId && x.EsPrincipal && x.Id != pr.Id)
            .ToListAsync();

        if (otros.Any())
        {
            foreach (var o in otros) o.EsPrincipal = false;
            await _db.SaveChangesAsync();
        }

        pr.EsPrincipal = true;
        await _db.SaveChangesAsync();

        return Ok(new { pr.Id, pr.ProductoId, pr.EsPrincipal });
    }

    // DELETE: api/presentaciones/123
    [Authorize(Roles = "Administrador,Admin")]
    [HttpDelete("presentaciones/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var pr = await _db.Presentaciones.FindAsync(id);
        if (pr is null) return NotFound();

        // Aquí más adelante validar si la presentación participa en documentos (compras/ventas) antes de eliminar.
        _db.Presentaciones.Remove(pr);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

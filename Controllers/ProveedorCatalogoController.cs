using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Controllers;

[ApiController]
[Route("api/Proveedores/{proveedorId:int}/Catalogo")]
public class ProveedorCatalogoController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProveedorCatalogoController(AppDbContext db) => _db = db;

    /// <summary>
    /// Lista el catálogo del proveedor con búsqueda por término, filtro por activos,
    /// filtro por categoría y límite (take). Devuelve los campos requeridos por el front.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProveedorCatalogoItemDto>>> List(
        int proveedorId,
        [FromQuery] string? term = null,
        [FromQuery] bool soloActivos = true,
        [FromQuery] int? categoriaId = null,
        [FromQuery] int take = 200)
    {
        var q = _db.ProveedoresPresentaciones
            .Where(pp => pp.ProveedorId == proveedorId)
            .Include(pp => pp.Presentacion).ThenInclude(pr => pr.Producto).ThenInclude(p => p.Categoria)
            .Include(pp => pp.Presentacion).ThenInclude(pr => pr.Unidad)
            .Include(pp => pp.Presentacion).ThenInclude(pr => pr.Color)
            .AsNoTracking();

        if (soloActivos)
        {
            q = q.Where(pp =>
                pp.Activo &&
                pp.Presentacion.Activo &&
                pp.Presentacion.Producto.Activo);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim().ToLower();
            q = q.Where(pp =>
                pp.Presentacion.Producto.Nombre.ToLower().Contains(t) ||
                (pp.Presentacion.Producto.Codigo ?? "").ToLower().Contains(t) ||
                pp.Presentacion.Nombre.ToLower().Contains(t) ||
                (pp.Presentacion.SKU ?? "").ToLower().Contains(t) ||
                (pp.Presentacion.CodigoBarras ?? "").ToLower().Contains(t) ||
                (pp.CodigoProveedor ?? "").ToLower().Contains(t));
        }

        if (categoriaId.HasValue)
        {
            q = q.Where(pp => pp.Presentacion.Producto.CategoriaId == categoriaId.Value);
        }

        var items = await q
            .OrderBy(pp => pp.Presentacion.Producto.Nombre)
            .ThenBy(pp => pp.Presentacion.Nombre)
            .Select(pp => new ProveedorCatalogoItemDto
            {
                PresentacionId = pp.PresentacionId,
                ProductoId = pp.Presentacion.ProductoId,
                ProductoCodigo = pp.Presentacion.Producto.Codigo,
                ProductoNombre = pp.Presentacion.Producto.Nombre,
                ProductoCategoriaId = pp.Presentacion.Producto.CategoriaId,
                ProductoCategoria = pp.Presentacion.Producto.Categoria != null
                    ? pp.Presentacion.Producto.Categoria.Nombre
                    : null,

                PresentacionNombre = pp.Presentacion.Nombre,
                Unidad = pp.Presentacion.Unidad.Simbolo,
                Color = pp.Presentacion.Color != null ? pp.Presentacion.Color.Nombre : null,
                SKU = pp.Presentacion.SKU,
                CodigoBarras = pp.Presentacion.CodigoBarras,
                CodigoProveedor = pp.CodigoProveedor,

                FotoUrl = pp.Presentacion.Producto.FotoUrl,

                // Disponible por presentación (suma de stock)
                Disponible = _db.ProductoStocks
                    .Where(s => s.PresentacionId == pp.PresentacionId)
                    .Sum(s => (decimal?)s.Cantidad) ?? 0m,

                // ✅ Fallback completo: Ultimo > Lista > PrecioCompraDefault de Presentación > PrecioCompraDefault de Producto > 0
                PrecioSugerido = pp.PrecioUltimo
                               ?? pp.PrecioLista
                               ?? pp.Presentacion.PrecioCompraDefault
                               ?? pp.Presentacion.Producto.PrecioCompraDefault
                               ?? 0m,

                Activo = pp.Activo
            })
            .Take(take)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult> Add(int proveedorId, ProveedorCatalogoCreateDto body)
    {
        if (!await _db.Proveedores.AnyAsync(x => x.Id == proveedorId))
            return NotFound("Proveedor no existe.");
        if (!await _db.Presentaciones.AnyAsync(x => x.Id == body.PresentacionId))
            return BadRequest("Presentación no existe.");

        var exists = await _db.ProveedoresPresentaciones
            .AnyAsync(x => x.ProveedorId == proveedorId && x.PresentacionId == body.PresentacionId);
        if (exists) return Conflict("Ya está en el catálogo del proveedor.");

        var row = new ProveedorPresentacion
        {
            ProveedorId = proveedorId,
            PresentacionId = body.PresentacionId,
            CodigoProveedor = body.CodigoProveedor,
            PrecioLista = body.PrecioLista,
            Activo = true,
            Notas = body.Notas
        };
        _db.ProveedoresPresentaciones.Add(row);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { proveedorId }, new { row.Id });
    }

    // PATCH: api/Proveedores/{proveedorId}/Catalogo/{presentacionId}
    [HttpPatch("{presentacionId:int}")]
    public async Task<IActionResult> Update(int proveedorId, int presentacionId, ProveedorCatalogoUpdateDto body)
    {
        var row = await _db.ProveedoresPresentaciones
            .FirstOrDefaultAsync(x => x.ProveedorId == proveedorId && x.PresentacionId == presentacionId);
        if (row is null)
            return NotFound(new { message = "La presentación no está en el catálogo del proveedor." });

        if (body.CodigoProveedor is not null)
            row.CodigoProveedor = string.IsNullOrWhiteSpace(body.CodigoProveedor)
                ? null
                : body.CodigoProveedor.Trim();

        if (body.PrecioLista is not null)
            row.PrecioLista = body.PrecioLista;

        if (body.PrecioUltimo is not null)
            row.PrecioUltimo = body.PrecioUltimo;

        if (body.Activo is not null)
            row.Activo = body.Activo.Value;

        if (body.Notas is not null)
            row.Notas = string.IsNullOrWhiteSpace(body.Notas) ? null : body.Notas.Trim();

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{presentacionId:int}")]
    public async Task<IActionResult> Delete(int proveedorId, int presentacionId)
    {
        var row = await _db.ProveedoresPresentaciones
            .FirstOrDefaultAsync(x => x.ProveedorId == proveedorId && x.PresentacionId == presentacionId);
        if (row is null) return NotFound();

        _db.ProveedoresPresentaciones.Remove(row);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

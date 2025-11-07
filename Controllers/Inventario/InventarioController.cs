using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaOriginalBackend.Controllers.Inventario;

[ApiController]
[Route("api/[controller]")]
public class InventarioController : ControllerBase
{
    private readonly AppDbContext _db;
    public InventarioController(AppDbContext db) => _db = db;

    // ===================== STOCK =====================

    [HttpGet("stock")]
    public async Task<ActionResult<IEnumerable<StockListDto>>> GetStock(
        [FromQuery] string? term = null,
        [FromQuery] bool soloActivos = true,
        [FromQuery] bool soloBajos = false)
    {
        var q = _db.ProductoStocks
            .AsNoTracking()
            .Include(s => s.Presentacion).ThenInclude(p => p.Unidad)
            .Include(s => s.Presentacion).ThenInclude(p => p.Producto)
            .Include(s => s.Presentacion).ThenInclude(p => p.Color)
            .AsQueryable();

        if (soloActivos)
            q = q.Where(s => s.Presentacion.Activo && s.Presentacion.Producto.Activo);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim().ToLower();
            q = q.Where(s =>
                s.Presentacion.Producto.Nombre.ToLower().Contains(t) ||
                (s.Presentacion.Producto.Codigo != null && s.Presentacion.Producto.Codigo.ToLower().Contains(t)) ||
                (s.Presentacion.SKU ?? "").ToLower().Contains(t) ||
                (s.Presentacion.CodigoBarras ?? "").ToLower().Contains(t)
            );
        }

        if (soloBajos)
            q = q.Where(s => s.Minimo != null && s.Cantidad < s.Minimo);

        var list = await q
            .OrderBy(s => s.Presentacion.Producto.Nombre)
            .ThenBy(s => s.Presentacion.Nombre)
            .Select(s => new StockListDto
            {
                PresentacionId = s.PresentacionId,
                ProductoId = s.Presentacion.ProductoId,
                Producto = s.Presentacion.Producto.Nombre,
                ProductoCodigo = s.Presentacion.Producto.Codigo,
                FotoUrl = s.Presentacion.Producto.FotoUrl,
                Cantidad = s.Cantidad,
                Minimo = s.Minimo,
                PrecioVenta = s.Presentacion.PrecioVentaDefault
            })
            .ToListAsync();

        return Ok(list);
    }

    // ===================== STOCK (detalle) =====================

    [HttpGet("stock/{presentacionId:int}")]
    public async Task<ActionResult<StockDetailDto>> GetStockByPresentacion(int presentacionId)
    {
        var s = await _db.ProductoStocks
            .AsNoTracking()
            .Include(x => x.Presentacion).ThenInclude(p => p.Unidad)
            .Include(x => x.Presentacion).ThenInclude(p => p.Producto)
            .Include(x => x.Presentacion).ThenInclude(p => p.Color)
            .FirstOrDefaultAsync(x => x.PresentacionId == presentacionId);

        if (s is null)
        {
            var pr = await _db.Presentaciones
                .AsNoTracking()
                .Include(p => p.Producto)
                .Include(p => p.Unidad)
                .Include(p => p.Color)
                .FirstOrDefaultAsync(p => p.Id == presentacionId);

            if (pr is null) return NotFound(new { message = "Presentación no encontrada." });

            return Ok(new StockDetailDto
            {
                PresentacionId = pr.Id,
                ProductoId = pr.ProductoId,
                Producto = pr.Producto.Nombre,
                ProductoCodigo = pr.Producto.Codigo,
                FotoUrl = pr.Producto.FotoUrl,
                Cantidad = 0m,
                Minimo = null
            });
        }

        return Ok(new StockDetailDto
        {
            PresentacionId = s.PresentacionId,
            ProductoId = s.Presentacion.ProductoId,
            Producto = s.Presentacion.Producto.Nombre,
            ProductoCodigo = s.Presentacion.Producto.Codigo,
            FotoUrl = s.Presentacion.Producto.FotoUrl,
            Cantidad = s.Cantidad,
            Minimo = s.Minimo
        });
    }

    // ===================== KARDEX =====================

    [HttpGet("kardex")]
    public async Task<ActionResult<IEnumerable<KardexItemDto>>> GetKardex(
        [FromQuery] int presentacionId,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null)
    {
        var prExists = await _db.Presentaciones.AsNoTracking().AnyAsync(p => p.Id == presentacionId);
        if (!prExists) return NotFound(new { message = "Presentación no encontrada." });

        var q = _db.MovimientosInventario
            .AsNoTracking()
            .Where(m => m.PresentacionId == presentacionId);

        if (desde.HasValue) q = q.Where(m => m.FechaUtc >= desde.Value.ToUniversalTime());
        if (hasta.HasValue) q = q.Where(m => m.FechaUtc <= hasta.Value.ToUniversalTime());

        var list = await q
            .OrderBy(m => m.FechaUtc)
            .Select(m => new KardexItemDto
            {
                Id = m.Id,
                FechaUtc = DateTime.SpecifyKind(m.FechaUtc, DateTimeKind.Utc),
                Tipo = m.Tipo.ToString(),
                Cantidad = m.Cantidad,
                CostoUnitario = m.CostoUnitario,
                PrecioUnitario = m.PrecioUnitario,
                Documento = m.Documento,
                DocumentoId = m.DocumentoId,
                Notas = m.Notas
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{presentacionId:int}/Movimientos")]
    public Task<ActionResult<IEnumerable<KardexItemDto>>> MovimientosCompat(int presentacionId)
        => GetKardex(presentacionId, null, null);

    // ===================== DEFINIR MÍNIMO DE STOCK =====================

    [HttpPut("stock/{presentacionId:int}/minimo")]
    public async Task<ActionResult> DefinirMinimo(int presentacionId, [FromBody] MinimoDto dto)
    {
        if (dto.Minimo < 0) return BadRequest(new { message = "El mínimo no puede ser negativo." });

        var stock = await _db.ProductoStocks.FirstOrDefaultAsync(s => s.PresentacionId == presentacionId);
        if (stock == null)
        {
            stock = new ProductoStock { PresentacionId = presentacionId, Cantidad = 0m, Minimo = dto.Minimo };
            _db.ProductoStocks.Add(stock);
        }
        else
        {
            stock.Minimo = dto.Minimo;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Mínimo actualizado",
            presentacionId,
            minimo = dto.Minimo
        });
    }

    // ===================== AJUSTE =====================
    [HttpPost("ajuste")]
    public async Task<ActionResult> Ajuste([FromBody] AjusteInventarioDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var pr = await _db.Presentaciones
            .Include(p => p.Producto)
            .FirstOrDefaultAsync(p => p.Id == dto.PresentacionId);

        if (pr is null) return NotFound(new { message = "Presentación no encontrada." });
        if (dto.Cantidad <= 0) return BadRequest(new { message = "La cantidad debe ser mayor a 0." });

        int? userId = null;
        var idClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idClaim, out var parsed)) userId = parsed;

        using var tx = await _db.Database.BeginTransactionAsync();

        var stock = await _db.ProductoStocks.FirstOrDefaultAsync(s => s.PresentacionId == dto.PresentacionId);
        if (stock == null)
        {
            stock = new ProductoStock { PresentacionId = dto.PresentacionId, Cantidad = 0m, CostoPromedio = 0m };
            _db.ProductoStocks.Add(stock);
            await _db.SaveChangesAsync();
        }

        // === Reglas ===
        decimal costoAplicado = 0;

        if (dto.Tipo == "entrada")
        {
            if (!dto.CostoUnitario.HasValue || dto.CostoUnitario.Value <= 0)
                return BadRequest(new { message = "El costo unitario es obligatorio para las entradas." });

            costoAplicado = dto.CostoUnitario.Value;
        }
        else if (dto.Tipo == "salida")
        {
            if (stock.Cantidad < dto.Cantidad)
                return Conflict(new { message = "No hay stock suficiente para realizar la salida.", stockActual = stock.Cantidad });

            // usar costo promedio
            costoAplicado = stock.CostoPromedio;
        }

        var mov = new MovimientoInventario
        {
            FechaUtc = DateTime.UtcNow,
            PresentacionId = dto.PresentacionId,
            Tipo = dto.Tipo == "entrada" ? TipoMovimiento.Entrada : TipoMovimiento.Salida,
            Cantidad = dto.Tipo == "entrada" ? dto.Cantidad : -dto.Cantidad, // salida en negativo
            CostoUnitario = costoAplicado,
            Documento = "Ajuste",
            DocumentoId = null,
            Notas = dto.Motivo,
            UsuarioId = userId
        };

        _db.MovimientosInventario.Add(mov);

        // === Actualizar stock y costo promedio ===
        if (dto.Tipo == "entrada")
        {
            var totalAnterior = stock.CostoPromedio * stock.Cantidad;
            var totalNuevo = costoAplicado * dto.Cantidad;
            stock.CostoPromedio = (totalAnterior + totalNuevo) / (stock.Cantidad + dto.Cantidad);
            stock.Cantidad += dto.Cantidad;
        }
        else // salida
        {
            stock.Cantidad -= dto.Cantidad;
            // costo promedio NO se recalcula en salidas
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            message = "Ajuste registrado",
            presentacionId = dto.PresentacionId,
            tipo = dto.Tipo,
            cantidad = dto.Cantidad,
            stockActual = stock.Cantidad,
            costoAplicado,
            costoPromedio = stock.CostoPromedio
        });
    }
}

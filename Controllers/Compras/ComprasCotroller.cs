using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using LaOriginalBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaOriginalBackend.Controllers.Compras
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ComprasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICajaDomainService _caja;

        public ComprasController(AppDbContext db, ICajaDomainService caja)
        {
            _db = db;
            _caja = caja;
        }

        private static decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        private int? GetUserId()
        {
            var idClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : (int?)null;
        }

        // ===== LISTAR =====
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompraListDto>>> Get(
            [FromQuery] string? term = null,
            [FromQuery] int? proveedorId = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            CancellationToken ct = default)
        {
            var q = _db.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim().ToLower();
                q = q.Where(c =>
                    (c.Numero != null && EF.Functions.Like(c.Numero.ToLower(), $"%{t}%")) ||
                    (c.Proveedor != null && EF.Functions.Like(c.Proveedor.Nombre.ToLower(), $"%{t}%")));
            }

            if (proveedorId.HasValue) q = q.Where(c => c.ProveedorId == proveedorId.Value);
            if (desde.HasValue) q = q.Where(c => c.Fecha >= desde.Value.ToUniversalTime());
            if (hasta.HasValue) q = q.Where(c => c.Fecha <= hasta.Value.ToUniversalTime());

            var list = await q
                .OrderByDescending(c => c.Fecha)
                .Select(c => new CompraListDto
                {
                    Id = c.Id,
                    Fecha = DateTime.SpecifyKind(c.Fecha, DateTimeKind.Utc),
                    Proveedor = c.Proveedor != null ? c.Proveedor.Nombre : "(Sin proveedor)",
                    Numero = c.Numero,
                    Total = c.Total,
                    Estado = c.Estado,
                    Anulada = c.Anulada
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        // ===== OBTENER =====
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CompraDetailDto>> GetById(int id, CancellationToken ct = default)
        {
            var c = await _db.Compras.AsNoTracking()
                .Include(x => x.Proveedor)
                .Include(x => x.FormaPago)
                .Include(x => x.Detalles).ThenInclude(d => d.Presentacion).ThenInclude(p => p.Producto)
                .Include(x => x.Detalles).ThenInclude(d => d.Presentacion).ThenInclude(p => p.Unidad)
                .Include(x => x.Detalles).ThenInclude(d => d.Presentacion).ThenInclude(p => p.Color)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (c is null) return NotFound();

            var dto = new CompraDetailDto
            {
                Id = c.Id,
                Fecha = DateTime.SpecifyKind(c.Fecha, DateTimeKind.Utc),
                ProveedorId = c.ProveedorId,
                Proveedor = c.Proveedor?.Nombre ?? "(Sin proveedor)",
                Numero = c.Numero,
                Observaciones = c.Observaciones,
                FormaPagoId = c.FormaPagoId,
                FormaPago = c.FormaPago?.Nombre,
                Subtotal = c.Subtotal,
                Descuento = c.Descuento,
                Total = c.Total,
                Estado = c.Estado,
                Anulada = c.Anulada,
                Detalles = c.Detalles.Select(d => new CompraDetalleDto
                {
                    Id = d.Id,
                    PresentacionId = d.PresentacionId,
                    Producto = d.Presentacion.Producto.Nombre,
                    Presentacion = d.Presentacion.Nombre,
                    Unidad = d.Presentacion.Unidad.Nombre,
                    Color = d.Presentacion.Color?.Nombre,
                    Cantidad = d.Cantidad,
                    CostoUnitario = d.CostoUnitario,
                    TotalLinea = d.TotalLinea,
                    Notas = d.Notas ?? d.Notas 
                }).ToList()
            };

            return Ok(dto);
        }

        // ===== CREAR =====
        [Authorize(Roles = "Administrador,Admin,Compras")]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CompraCreateDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dto is null) return BadRequest(new { message = "Solicitud inválida." });
            if (dto.Detalles == null || dto.Detalles.Count == 0)
                return BadRequest(new { message = "La compra debe tener al menos un detalle." });

            var prov = await _db.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == dto.ProveedorId, ct);
            if (prov is null) return BadRequest(new { message = "Proveedor no existe." });

            if (dto.Detalles.Any(d => d.Cantidad <= 0))
                return BadRequest(new { message = "Todas las cantidades deben ser mayores a cero." });

            if (dto.Detalles.Any(d => d.CostoUnitario < 0))
                return BadRequest(new { message = "El costo unitario no puede ser negativo." });

            // Forma de pago
            bool afectaCaja = false;
            bool requiereRef = false;
            string? fpNombre = null;

            if (dto.FormaPagoId.HasValue)
            {
                var fp = await _db.FormasPago.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == dto.FormaPagoId.Value, ct);

                if (fp is not null)
                {
                    fpNombre = fp.Nombre;
                    afectaCaja = fp.AfectaCaja;
                    requiereRef = fp.RequiereReferencia;
                }
            }

            if (requiereRef && string.IsNullOrWhiteSpace(dto.Referencia))
                return BadRequest(new { message = "Esta forma de pago requiere número de referencia." });

            if (afectaCaja)
            {
                var est = await _caja.EstadoAsync(ct);
                if (!est.Abierta)
                    return BadRequest(new { message = "No puedes registrar la compra: la caja está cerrada." });
            }

            var userId = GetUserId();

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            // Validar presentaciones
            var presentacionIds = dto.Detalles.Select(d => d.PresentacionId).Distinct().ToList();
            var prs = await _db.Presentaciones
                .Where(p => presentacionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(ct);

            if (prs.Count != presentacionIds.Count)
                return BadRequest(new { message = "Una o más presentaciones no existen." });

            var now = DateTime.UtcNow;

            var compra = new Compra
            {
                Fecha = now,
                ProveedorId = dto.ProveedorId,
                Numero = string.IsNullOrWhiteSpace(dto.Numero) ? null : dto.Numero.Trim(),
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),
                FormaPagoId = dto.FormaPagoId,
                Estado = "Registrada",
                Anulada = false,
                UsuarioId = userId
            };

            decimal subtotal = 0m;

            foreach (var d in dto.Detalles)
            {
                var costo = RoundMoney(d.CostoUnitario);
                var cant = d.Cantidad;
                var totalLinea = RoundMoney(cant * costo);
                subtotal += totalLinea;

                compra.Detalles.Add(new CompraDetalle
                {
                    PresentacionId = d.PresentacionId,
                    Cantidad = cant,
                    CostoUnitario = costo,
                    TotalLinea = totalLinea,
                    Notas = string.IsNullOrWhiteSpace(d.Notas) ? null : d.Notas.Trim()
                });
            }

            compra.Subtotal = RoundMoney(subtotal);
            compra.Descuento = 0m;
            compra.Total = compra.Subtotal;

            _db.Compras.Add(compra);
            await _db.SaveChangesAsync(ct);

            // Inventario (actualiza stock y costo promedio ponderado)
            var stockDict = await _db.ProductoStocks
                .Where(s => presentacionIds.Contains(s.PresentacionId))
                .ToDictionaryAsync(s => s.PresentacionId, ct);

            foreach (var d in compra.Detalles)
            {
                if (!stockDict.TryGetValue(d.PresentacionId, out var stock))
                {
                    stock = new ProductoStock { PresentacionId = d.PresentacionId, Cantidad = 0m, CostoPromedio = 0m };
                    _db.ProductoStocks.Add(stock);
                    stockDict[d.PresentacionId] = stock;
                }

                // 🆕 promedio ponderado de costo
                var cantAntes = stock.Cantidad;
                var totalAntes = stock.CostoPromedio * cantAntes;
                var totalNuevo = d.CostoUnitario * d.Cantidad;
                var cantDesp = cantAntes + d.Cantidad;

                stock.CostoPromedio = cantDesp <= 0 ? stock.CostoPromedio : (totalAntes + totalNuevo) / cantDesp;
                stock.Cantidad = cantDesp;

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    FechaUtc = now,
                    PresentacionId = d.PresentacionId,
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = d.Cantidad,
                    CostoUnitario = d.CostoUnitario,
                    Documento = "Compra",
                    DocumentoId = compra.Id,
                    Notas = d.Notas,
                    UsuarioId = userId
                });
            }


            await _db.SaveChangesAsync(ct);

            // Caja
            if (afectaCaja)
            {
                var fpNom = (fpNombre ?? "").Trim();
                var esDeposito = fpNom.ToLowerInvariant().Contains("deposit");
                var documento = esDeposito ? "Deposito" : "Compra";
                var sufijo = string.IsNullOrWhiteSpace(fpNom) ? "" : $" ({fpNom})";
                var concepto = $"Pago compra {(compra.Numero ?? compra.Id.ToString())}{sufijo}";

                await _caja.AddMovimientoEnCajaAbiertaAsync(new CajaMovimientoCreateDto
                {
                    Tipo = (int)TipoMovimientoCaja.PagoProveedor,
                    Monto = compra.Total,
                    Concepto = concepto,
                    Documento = documento,
                    DocumentoId = compra.Id,
                    Observaciones = string.IsNullOrWhiteSpace(dto.Referencia) ? null : $"Ref: {dto.Referencia!.Trim()}"
                }, userId, ct);
            }

            await tx.CommitAsync(ct);

            return CreatedAtAction(nameof(GetById), new { id = compra.Id },
                new { compra.Id, message = "Compra registrada y stock actualizado." });
        }

        // ===== ANULAR =====
        [Authorize(Roles = "Administrador,Admin,Compras")]
        [HttpPost("{id:int}/anular")]
        public async Task<ActionResult> Anular(int id, [FromBody] CompraAnularDto dto, CancellationToken ct = default)
        {
            var c = await _db.Compras
                .Include(x => x.Detalles)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (c is null) return NotFound(new { message = "Compra no encontrada." });
            if (c.Anulada) return Conflict(new { message = "La compra ya está anulada." });

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var detIds = c.Detalles.Select(d => d.PresentacionId).Distinct().ToList();
            var stockMap = await _db.ProductoStocks
                .Where(s => detIds.Contains(s.PresentacionId))
                .ToDictionaryAsync(s => s.PresentacionId, ct);

            foreach (var d in c.Detalles)
            {
                var disponible = stockMap.TryGetValue(d.PresentacionId, out var st) ? st.Cantidad : 0m;
                if (disponible < d.Cantidad)
                {
                    return Conflict(new
                    {
                        message = "No hay stock suficiente para anular la compra.",
                        presentacionId = d.PresentacionId,
                        requerido = d.Cantidad,
                        disponible
                    });
                }
            }

            var userId = GetUserId();
            var now = DateTime.UtcNow;
            var motivo = string.IsNullOrWhiteSpace(dto?.Motivo) ? "Anulación de compra" : dto!.Motivo!.Trim();

            foreach (var d in c.Detalles)
            {
                var stock = stockMap[d.PresentacionId];
                stock.Cantidad -= d.Cantidad;

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    FechaUtc = now,
                    PresentacionId = d.PresentacionId,
                    Tipo = TipoMovimiento.Salida,
                    Cantidad = -d.Cantidad,
                    CostoUnitario = d.CostoUnitario,
                    Documento = "AnulacionCompra",
                    DocumentoId = c.Id,
                    Notas = motivo,
                    UsuarioId = userId
                });
            }

            c.Anulada = true;
            c.Estado = "Anulada";

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Ok(new { message = "Compra anulada y stock revertido.", id = c.Id });
        }
    }
}

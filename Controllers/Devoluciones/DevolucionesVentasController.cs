// Controllers/Devoluciones/DevolucionesVentasController.cs
using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos.Devoluciones;
using LaOriginalBackend.Models;
using LaOriginalBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaOriginalBackend.Controllers.Devoluciones
{
    [ApiController]
    [Route("api/devoluciones/ventas")]
    public class DevolucionesVentasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICajaDomainService _caja;
        public DevolucionesVentasController(AppDbContext db, ICajaDomainService caja) { _db = db; _caja = caja; }

        private int? GetUserId()
        {
            var idClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : (int?)null;
        }

        // Listado / Detalle igual que lo tenías...
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DevolucionVentaListDto>>> Get(
            [FromQuery] string? term = null, [FromQuery] int? clienteId = null,
            [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
        {
            var q = _db.Set<DevolucionVenta>().AsNoTracking().Include(d => d.Cliente).AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim().ToLower();
                q = q.Where(d => (d.Numero != null && d.Numero.ToLower().Contains(t)) ||
                                 (d.Cliente != null && d.Cliente.Nombre.ToLower().Contains(t)));
            }
            if (clienteId.HasValue) q = q.Where(d => d.ClienteId == clienteId.Value);
            if (desde.HasValue) q = q.Where(d => d.Fecha >= DateTime.SpecifyKind(desde.Value, DateTimeKind.Utc));
            if (hasta.HasValue) q = q.Where(d => d.Fecha <= DateTime.SpecifyKind(hasta.Value, DateTimeKind.Utc));

            var list = await q.OrderByDescending(d => d.Fecha).Select(d => new DevolucionVentaListDto
            {
                Id = d.Id,
                Fecha = d.Fecha,
                Numero = d.Numero,
                Cliente = d.Cliente != null ? d.Cliente.Nombre : "Consumidor Final",
                Total = d.Total,
                Estado = d.Estado,
                Anulada = d.Anulada
            }).ToListAsync();

            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DevolucionVentaDetailDto>> GetById(int id)
        {
            var d = await _db.Set<DevolucionVenta>().AsNoTracking()
                .Include(x => x.Cliente)
                .Include(x => x.FormaPago)
                .Include(x => x.Detalles).ThenInclude(it => it.Presentacion).ThenInclude(p => p.Producto)
                .Include(x => x.Detalles).ThenInclude(it => it.Presentacion).ThenInclude(p => p.Unidad)
                .Include(x => x.Detalles).ThenInclude(it => it.Presentacion).ThenInclude(p => p.Color)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (d is null) return NotFound();

            var dto = new DevolucionVentaDetailDto
            {
                Id = d.Id,
                Fecha = d.Fecha,
                VentaId = d.VentaId,
                ClienteId = d.ClienteId,
                Cliente = d.Cliente?.Nombre ?? "Consumidor Final",
                Numero = d.Numero,
                Observaciones = d.Observaciones,
                FormaPagoId = d.FormaPagoId,
                FormaPago = d.FormaPago?.Nombre,
                Subtotal = d.Subtotal,
                Descuento = d.Descuento,
                Total = d.Total,
                Estado = d.Estado,
                Anulada = d.Anulada,
                Detalles = d.Detalles.Select(it => new DVItemDto
                {
                    Id = it.Id,
                    PresentacionId = it.PresentacionId,
                    Producto = it.Presentacion.Producto.Nombre,
                    Presentacion = it.Presentacion.Nombre,
                    Unidad = it.Presentacion.Unidad.Nombre,
                    Color = it.Presentacion.Color?.Nombre,
                    Cantidad = it.Cantidad,
                    PrecioUnitario = it.PrecioUnitario,
                    DescuentoUnitario = it.DescuentoUnitario,
                    TotalLinea = it.TotalLinea,
                    Notas = it.Notas
                }).ToList()
            };
            return Ok(dto);
        }

        // Crear (ENTRADA inventario + 💵 Egreso caja por reembolso)
        [Authorize(Roles = "Administrador,Admin,Vendedor,Ventas,Caja")]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] DevolucionVentaCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.Detalles.Any(i => i.Cantidad <= 0)) return BadRequest(new { message = "Todas las cantidades deben ser > 0." });
            if (dto.Detalles.Any(i => i.PrecioUnitario < 0 || i.DescuentoUnitario < 0)) return BadRequest(new { message = "Precios/Descuentos inválidos." });

            var prsIds = dto.Detalles.Select(i => i.PresentacionId).Distinct().ToList();
            var prs = await _db.Presentaciones.Where(p => prsIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();
            if (prs.Count != prsIds.Count) return BadRequest(new { message = "Una o más presentaciones no existen." });

            var userId = GetUserId();
            using var tx = await _db.Database.BeginTransactionAsync();

            var dev = new DevolucionVenta
            {
                Fecha = DateTime.UtcNow,
                VentaId = dto.VentaId,
                ClienteId = dto.ClienteId,
                Numero = string.IsNullOrWhiteSpace(dto.Numero) ? null : dto.Numero.Trim(),
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),
                FormaPagoId = dto.FormaPagoId,
                Estado = "Registrada",
                Anulada = false,
                UsuarioId = userId
            };

            decimal subtotal = 0m, descuento = 0m;
            foreach (var i in dto.Detalles)
            {
                var totalLinea = Math.Round(i.Cantidad * (i.PrecioUnitario - i.DescuentoUnitario), 2, MidpointRounding.AwayFromZero);
                subtotal += Math.Round(i.Cantidad * i.PrecioUnitario, 2, MidpointRounding.AwayFromZero);
                descuento += Math.Round(i.Cantidad * i.DescuentoUnitario, 2, MidpointRounding.AwayFromZero);

                dev.Detalles.Add(new DevolucionVentaDetalle
                {
                    PresentacionId = i.PresentacionId,
                    Cantidad = i.Cantidad,
                    PrecioUnitario = i.PrecioUnitario,
                    DescuentoUnitario = i.DescuentoUnitario,
                    TotalLinea = totalLinea,
                    Notas = string.IsNullOrWhiteSpace(i.Notas) ? null : i.Notas.Trim()
                });
            }
            dev.Subtotal = subtotal;
            dev.Descuento = descuento;
            dev.Total = subtotal - descuento;

            _db.Set<DevolucionVenta>().Add(dev);
            await _db.SaveChangesAsync();

            // Inventario (ENTRADA)
            var stockMap = await _db.ProductoStocks.Where(s => prsIds.Contains(s.PresentacionId)).ToDictionaryAsync(s => s.PresentacionId);
            foreach (var d in dev.Detalles)
            {
                if (!stockMap.TryGetValue(d.PresentacionId, out var stock))
                {
                    stock = new ProductoStock { PresentacionId = d.PresentacionId, Cantidad = 0m };
                    _db.ProductoStocks.Add(stock);
                    stockMap[d.PresentacionId] = stock;
                }
                stock.Cantidad += d.Cantidad;

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    FechaUtc = DateTime.UtcNow,
                    PresentacionId = d.PresentacionId,
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = d.Cantidad,
                    CostoUnitario = d.PrecioUnitario,
                    Documento = "DevolucionVenta",
                    DocumentoId = dev.Id,
                    Notas = d.Notas,
                    UsuarioId = userId
                });
            }
            await _db.SaveChangesAsync();

            // 💵 Caja (si abierta): Egreso por reembolso al cliente
            var estadoCaja = await _caja.EstadoAsync();
            if (estadoCaja.Abierta)
            {
                await _caja.AddMovimientoEnCajaAbiertaAsync(new LaOriginalBackend.Dtos.CajaMovimientoCreateDto
                {
                    Tipo = (int)TipoMovimientoCaja.Egreso,
                    Monto = dev.Total,
                    Concepto = $"Reembolso por devolución de venta #{dev.Id}",
                    Documento = "DevolucionVenta",
                    DocumentoId = dev.Id
                }, userId);
            }

            await tx.CommitAsync();
            return CreatedAtAction(nameof(GetById), new { id = dev.Id }, new { dev.Id, message = "Devolución de venta registrada." });
        }

        // Anular (SALIDA inventario + 💵 Ingreso caja si abierta)
        [Authorize(Roles = "Administrador,Admin,Vendedor,Ventas,Caja")]
        [HttpPost("{id:int}/anular")]
        public async Task<ActionResult> Anular(int id, [FromBody] DVAnularDto dto)
        {
            var d = await _db.Set<DevolucionVenta>().Include(x => x.Detalles).FirstOrDefaultAsync(x => x.Id == id);
            if (d is null) return NotFound(new { message = "Devolución no encontrada." });
            if (d.Anulada) return Conflict(new { message = "La devolución ya está anulada." });

            using var tx = await _db.Database.BeginTransactionAsync();
            var prsIds = d.Detalles.Select(it => it.PresentacionId).Distinct().ToList();
            var stockMap = await _db.ProductoStocks.Where(s => prsIds.Contains(s.PresentacionId)).ToDictionaryAsync(s => s.PresentacionId);

            foreach (var it in d.Detalles)
            {
                var disponible = stockMap.TryGetValue(it.PresentacionId, out var st) ? st.Cantidad : 0m;
                if (disponible < it.Cantidad)
                    return Conflict(new { message = "No hay stock suficiente para anular la devolución.", presentacionId = it.PresentacionId, requerido = it.Cantidad, disponible });
            }

            var userId = GetUserId();

            foreach (var it in d.Detalles)
            {
                var stock = stockMap[it.PresentacionId];
                stock.Cantidad -= it.Cantidad; // SALIDA

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    FechaUtc = DateTime.UtcNow,
                    PresentacionId = it.PresentacionId,
                    Tipo = TipoMovimiento.Salida,
                    Cantidad = -it.Cantidad,
                    CostoUnitario = it.PrecioUnitario,
                    Documento = "AnulacionDevolucionVenta",
                    DocumentoId = d.Id,
                    Notas = string.IsNullOrWhiteSpace(dto.Motivo) ? "Anulación de devolución de venta" : dto.Motivo,
                    UsuarioId = userId
                });
            }

            d.Anulada = true;
            d.Estado = "Anulada";
            await _db.SaveChangesAsync();

            // 💵 Caja (si abierta): Ingreso para revertir el reembolso
            var estadoCaja = await _caja.EstadoAsync();
            if (estadoCaja.Abierta)
            {
                await _caja.AddMovimientoEnCajaAbiertaAsync(new LaOriginalBackend.Dtos.CajaMovimientoCreateDto
                {
                    Tipo = (int)TipoMovimientoCaja.Ingreso,
                    Monto = d.Total,
                    Concepto = $"Reversa de reembolso por anulación de devolución de venta #{d.Id}",
                    Documento = "AnulacionDevolucionVenta",
                    DocumentoId = d.Id
                }, userId);
            }

            await tx.CommitAsync();
            return Ok(new { message = "Devolución de venta anulada y stock revertido.", id = d.Id });
        }
    }
}

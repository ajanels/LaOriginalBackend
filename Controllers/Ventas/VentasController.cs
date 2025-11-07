// Controllers/Ventas/VentasController.cs
using LaOriginalBackend.Data;
using LaOriginalBackend.Models;
using LaOriginalBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Data; // IsolationLevel

// Alias para los DTOs de Ventas (tu archivo Dtos/Ventas/VentasDtos.cs)
using DV = LaOriginalBackend.Dtos.Ventas;
// Alias para DTOs de Caja (CajaMovimientoCreateDto) que están en la raíz
using DCaja = LaOriginalBackend.Dtos;

namespace LaOriginalBackend.Controllers.Ventas
{
    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICajaDomainService _caja;

        public VentasController(AppDbContext db, ICajaDomainService caja)
        {
            _db = db;
            _caja = caja;
        }

        private int? GetUserId()
        {
            var idClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : (int?)null;
        }

        private static bool EsPagoEfectivo(string? formaPagoNombre)
        {
            if (string.IsNullOrWhiteSpace(formaPagoNombre)) return false;
            var n = formaPagoNombre.Trim().ToLower();
            return n is "efectivo" or "cash" or "contado";
        }

        // ===== LISTAR =====
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DV.VentaListDto>>> Get(
            [FromQuery] string? term = null,
            [FromQuery] int? clienteId = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            var q = _db.Ventas.AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.FormaPago)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim().ToLower();
                q = q.Where(v =>
                    (v.Numero != null && v.Numero.ToLower().Contains(t)) ||
                    (v.Serie != null && v.Serie.ToLower().Contains(t)) ||
                    (v.Cliente != null && v.Cliente.Nombre.ToLower().Contains(t)));
            }

            if (clienteId.HasValue) q = q.Where(v => v.ClienteId == clienteId.Value);
            if (desde.HasValue) q = q.Where(v => v.Fecha >= DateTime.SpecifyKind(desde.Value, DateTimeKind.Utc));
            if (hasta.HasValue) q = q.Where(v => v.Fecha <= DateTime.SpecifyKind(hasta.Value, DateTimeKind.Utc));

            var list = await q.OrderByDescending(v => v.Fecha).Select(v => new DV.VentaListDto
            {
                Id = v.Id,
                Fecha = v.Fecha,
                Serie = v.Serie,
                Numero = v.Numero,
                ClienteNombre = v.Cliente != null ? v.Cliente.Nombre : "Consumidor Final",
                Total = v.Total,
                Estado = v.Estado,
                Anulada = v.Anulada,
                FormaPagoNombre = v.FormaPago != null ? v.FormaPago.Nombre : null
            }).ToListAsync();

            return Ok(list);
        }

        // ===== DETALLE =====
        [HttpGet("{id:int}")]
        public async Task<ActionResult<DV.VentaDetailDto>> GetById(int id)
        {
            var v = await _db.Ventas.AsNoTracking()
                .Include(x => x.Cliente)
                .Include(x => x.FormaPago)
                .Include(x => x.Detalles).ThenInclude(d => d.Presentacion).ThenInclude(p => p.Producto)
                .Include(x => x.Detalles).ThenInclude(d => d.Presentacion).ThenInclude(p => p.Unidad)
                .Include(x => x.Detalles).ThenInclude(d => d.Presentacion).ThenInclude(p => p.Color)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (v is null) return NotFound();

            string? usuarioNombre = null;
            if (v.UsuarioId.HasValue)
            {
                usuarioNombre = await _db.Usuarios
                    .Where(u => u.Id == v.UsuarioId.Value)
                    .Select(u => (u.PrimerNombre + " " + u.PrimerApellido).Trim())
                    .FirstOrDefaultAsync();
            }

            var dto = new DV.VentaDetailDto
            {
                Id = v.Id,
                Fecha = v.Fecha,
                ClienteId = v.ClienteId,
                ClienteNombre = v.Cliente?.Nombre,
                Serie = v.Serie,
                Numero = v.Numero,
                Observaciones = v.Observaciones,
                Subtotal = v.Subtotal,
                Descuento = v.Descuento,
                Total = v.Total,
                Estado = v.Estado,
                Anulada = v.Anulada,
                FormaPagoId = v.FormaPagoId,
                FormaPagoNombre = v.FormaPago?.Nombre,
                UsuarioId = v.UsuarioId,
                UsuarioNombre = usuarioNombre,
                Items = v.Detalles.Select(d => new DV.VentaItemDto
                {
                    Id = d.Id,
                    PresentacionId = d.PresentacionId,
                    PresentacionNombre = d.Presentacion.Nombre,
                    ProductoNombre = d.Presentacion.Producto.Nombre,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    DescuentoUnitario = d.DescuentoUnitario,
                    TotalLinea = d.TotalLinea,
                    Notas = d.Notas
                }).ToList()
            };

            return Ok(dto);
        }

        // ===== CREAR =====
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] DV.VentaCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new { message = "Debe incluir al menos 1 ítem." });

            if (dto.Items.Any(i => i.Cantidad <= 0))
                return BadRequest(new { message = "Todas las cantidades deben ser > 0." });

            if (dto.Items.Any(i => i.PrecioUnitario < 0 || i.DescuentoUnitario < 0))
                return BadRequest(new { message = "Precios/Descuentos inválidos." });

            if (dto.ClienteId.HasValue)
            {
                var cliExists = await _db.Clientes.AsNoTracking().AnyAsync(c => c.Id == dto.ClienteId.Value);
                if (!cliExists) return BadRequest(new { message = "Cliente no existe." });
            }

            var estadoCaja = await _caja.EstadoAsync();
            if (!estadoCaja.Abierta)
                return BadRequest(new { message = "No puedes registrar ventas: la caja está cerrada." });

            var userId = GetUserId();

            using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            // IDs de presentaciones involucradas
            var presentacionIds = dto.Items.Select(i => i.PresentacionId).Distinct().ToArray();

            // Mapa de stock (cantidad + costo promedio actual)
            var stockMap = await _db.ProductoStocks
                .Where(s => presentacionIds.Contains(s.PresentacionId))
                .ToDictionaryAsync(s => s.PresentacionId, s => s);

            // Mapa de reservas activas
            var estadosActivos = new[]
            {
                EstadoPedidoCliente.Confirmado,
                EstadoPedidoCliente.EnPreparacion,
                EstadoPedidoCliente.Listo
            };

            var reservasPorPid = await _db.PedidosClientesReservas
                .Where(r => presentacionIds.Contains(r.PresentacionId))
                .Join(_db.PedidosClientes, r => r.PedidoClienteId, p => p.Id, (r, p) => new { r.PresentacionId, p.Estado, r.Cantidad })
                .Where(x => estadosActivos.Contains(x.Estado))
                .GroupBy(x => x.PresentacionId)
                .Select(g => new { PresentacionId = g.Key, Cant = g.Sum(x => x.Cantidad) })
                .ToDictionaryAsync(x => x.PresentacionId, x => x.Cant);

            // Fallback de costos por si el stock no tiene costo promedio
            var presMap = await _db.Presentaciones
                .Where(p => presentacionIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            // Validar disponible y armar venta con costo “congelado”
            var venta = new Venta
            {
                Fecha = DateTime.UtcNow,
                ClienteId = dto.ClienteId,
                Serie = string.IsNullOrWhiteSpace(dto.Serie) ? null : dto.Serie.Trim(),
                Numero = string.IsNullOrWhiteSpace(dto.Numero) ? null : dto.Numero.Trim(),
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),
                FormaPagoId = dto.FormaPagoId,
                Estado = "Registrada",
                Anulada = false,
                UsuarioId = userId
            };

            decimal subtotal = 0m, descuento = 0m;

            foreach (var i in dto.Items)
            {
                var stock = stockMap.GetValueOrDefault(i.PresentacionId);
                var reservado = reservasPorPid.GetValueOrDefault(i.PresentacionId, 0m);
                var disponible = Math.Round((stock?.Cantidad ?? 0m) - reservado, 2, MidpointRounding.AwayFromZero);

                if (disponible < i.Cantidad)
                {
                    return Conflict(new
                    {
                        message = "Stock insuficiente: parte del stock está reservado por pedidos.",
                        presentacionId = i.PresentacionId,
                        requerido = i.Cantidad,
                        stock = stock?.Cantidad ?? 0m,
                        reservado,
                        disponible
                    });
                }

                // === Costo a "congelar" en el detalle ===
                var costoProm = stock?.CostoPromedio ?? 0m;
                decimal costoSnap = (costoProm > 0m)
                    ? costoProm
                    : (presMap.TryGetValue(i.PresentacionId, out var pres)
                        ? (pres.PrecioCompraDefault ?? 0m)
                        : 0m);

                var totalLinea = Math.Round(i.Cantidad * (i.PrecioUnitario - i.DescuentoUnitario), 2, MidpointRounding.AwayFromZero);
                subtotal += Math.Round(i.Cantidad * i.PrecioUnitario, 2, MidpointRounding.AwayFromZero);
                descuento += Math.Round(i.Cantidad * i.DescuentoUnitario, 2, MidpointRounding.AwayFromZero);

                venta.Detalles.Add(new VentaDetalle
                {
                    PresentacionId = i.PresentacionId,
                    Cantidad = i.Cantidad,
                    PrecioUnitario = i.PrecioUnitario,
                    DescuentoUnitario = i.DescuentoUnitario,
                    TotalLinea = totalLinea,
                    CostoUnitario = costoSnap, // snapshot
                    Notas = string.IsNullOrWhiteSpace(i.Notas) ? null : i.Notas.Trim()
                });
            }

            venta.Subtotal = subtotal;
            venta.Descuento = descuento;
            venta.Total = subtotal - descuento;

            _db.Ventas.Add(venta);
            await _db.SaveChangesAsync();

            // Movimientos de inventario (registramos costo aplicado)
            foreach (var d in venta.Detalles)
            {
                var stock = await _db.ProductoStocks.FirstOrDefaultAsync(s => s.PresentacionId == d.PresentacionId)
                            ?? new ProductoStock { PresentacionId = d.PresentacionId, Cantidad = 0m, CostoPromedio = 0m };

                if (stock.Id == 0) _db.ProductoStocks.Add(stock);

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    FechaUtc = DateTime.UtcNow,
                    PresentacionId = d.PresentacionId,
                    Tipo = TipoMovimiento.Salida,
                    Cantidad = -d.Cantidad,              // (mantenido como en tu código)
                    CostoUnitario = d.CostoUnitario,
                    Documento = "Venta",
                    DocumentoId = venta.Id,
                    Notas = d.Notas,
                    UsuarioId = userId
                });

                stock.Cantidad -= d.Cantidad;
                await _db.SaveChangesAsync();
            }

            // Movimiento en caja si es efectivo
            string? fpNombre = null;
            if (venta.FormaPagoId.HasValue)
                fpNombre = await _db.FormasPago.AsNoTracking()
                    .Where(f => f.Id == venta.FormaPagoId.Value)
                    .Select(f => f.Nombre)
                    .FirstOrDefaultAsync();

            if (EsPagoEfectivo(fpNombre))
            {
                await _caja.AddMovimientoEnCajaAbiertaAsync(new DCaja.CajaMovimientoCreateDto
                {
                    Tipo = (int)TipoMovimientoCaja.CobroVenta,
                    Monto = venta.Total,
                    Concepto = $"Cobro venta {(venta.Serie ?? "").Trim()}-{(venta.Numero ?? venta.Id.ToString()).Trim()}".Trim('-'),
                    Documento = "Venta",
                    DocumentoId = venta.Id
                }, userId);
            }

            await tx.CommitAsync();
            return CreatedAtAction(nameof(GetById), new { id = venta.Id },
                new { venta.Id, message = "Venta registrada y stock actualizado." });
        }

        // ===== ANULAR =====
        [HttpPost("{id:int}/anular")]
        public async Task<ActionResult> Anular(int id, [FromBody] DV.VentaAnularDto dto)
        {
            var v = await _db.Ventas.Include(x => x.Detalles).FirstOrDefaultAsync(x => x.Id == id);
            if (v is null) return NotFound(new { message = "Venta no encontrada." });
            if (v.Anulada) return Conflict(new { message = "La venta ya está anulada." });

            using var tx = await _db.Database.BeginTransactionAsync();
            var userId = GetUserId();

            foreach (var d in v.Detalles)
            {
                var stock = await _db.ProductoStocks.FirstOrDefaultAsync(s => s.PresentacionId == d.PresentacionId)
                            ?? new ProductoStock { PresentacionId = d.PresentacionId, Cantidad = 0m, CostoPromedio = 0m };

                if (stock.Id == 0) _db.ProductoStocks.Add(stock);

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    FechaUtc = DateTime.UtcNow,
                    PresentacionId = d.PresentacionId,
                    Tipo = TipoMovimiento.Entrada,
                    Cantidad = d.Cantidad,
                    CostoUnitario = d.CostoUnitario,   // devolvemos usando el costo de la venta
                    Documento = "AnulacionVenta",
                    DocumentoId = v.Id,
                    Notas = string.IsNullOrWhiteSpace(dto.Motivo) ? "Anulación de venta" : dto.Motivo,
                    UsuarioId = userId
                });

                stock.Cantidad += d.Cantidad;
                await _db.SaveChangesAsync();
            }

            string? fpNombre = null;
            if (v.FormaPagoId.HasValue)
                fpNombre = await _db.FormasPago.AsNoTracking()
                    .Where(f => f.Id == v.FormaPagoId.Value)
                    .Select(f => f.Nombre)
                    .FirstOrDefaultAsync();

            if (EsPagoEfectivo(fpNombre))
            {
                var estado = await _caja.EstadoAsync();
                if (estado.Abierta)
                {
                    await _caja.AddMovimientoEnCajaAbiertaAsync(new DCaja.CajaMovimientoCreateDto
                    {
                        Tipo = (int)TipoMovimientoCaja.Egreso,
                        Monto = v.Total,
                        Concepto = $"Devolución por anulación venta {(v.Serie ?? "").Trim()}-{(v.Numero ?? v.Id.ToString()).Trim()}".Trim('-'),
                        Observaciones = dto.Motivo,
                        Documento = "AnulacionVenta",
                        DocumentoId = v.Id
                    }, userId);
                }
            }

            v.Anulada = true;
            v.Estado = "Anulada";
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { message = "Venta anulada, stock revertido.", id = v.Id });
        }
    }
}

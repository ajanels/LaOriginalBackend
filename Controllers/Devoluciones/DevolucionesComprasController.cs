// Controllers/Devoluciones/DevolucionesComprasController.cs
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
    [Route("api/devoluciones/compras")]
    public class DevolucionesComprasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICajaDomainService _caja;
        public DevolucionesComprasController(AppDbContext db, ICajaDomainService caja) { _db = db; _caja = caja; }

        private int? GetUserId()
        {
            var idClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : (int?)null;
        }

        // Listado / Detalle igual que lo tenías...

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DevolucionCompraListDto>>> Get(
            [FromQuery] string? term = null, [FromQuery] int? proveedorId = null,
            [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
        {
            var q = _db.Set<DevolucionCompra>().AsNoTracking().Include(d => d.Proveedor).AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim().ToLower();
                q = q.Where(d => (d.Numero != null && d.Numero.ToLower().Contains(t)) ||
                                 (d.Proveedor != null && d.Proveedor.Nombre.ToLower().Contains(t)));
            }
            if (proveedorId.HasValue) q = q.Where(d => d.ProveedorId == proveedorId.Value);
            if (desde.HasValue) q = q.Where(d => d.Fecha >= DateTime.SpecifyKind(desde.Value, DateTimeKind.Utc));
            if (hasta.HasValue) q = q.Where(d => d.Fecha <= DateTime.SpecifyKind(hasta.Value, DateTimeKind.Utc));

            var list = await q.OrderByDescending(d => d.Fecha).Select(d => new DevolucionCompraListDto
            {
                Id = d.Id, Fecha = d.Fecha, Proveedor = d.Proveedor != null ? d.Proveedor.Nombre : "(Sin proveedor)",
                Numero = d.Numero, Total = d.Total, Estado = d.Estado, Anulada = d.Anulada
            }).ToListAsync();

            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DevolucionCompraDetailDto>> GetById(int id)
        {
            var d = await _db.Set<DevolucionCompra>().AsNoTracking()
                .Include(x => x.Proveedor)
                .Include(x => x.FormaPago)
                .Include(x => x.Detalles).ThenInclude(it => it.Presentacion).ThenInclude(p => p.Producto)
                .Include(x => x.Detalles).ThenInclude(it => it.Presentacion).ThenInclude(p => p.Unidad)
                .Include(x => x.Detalles).ThenInclude(it => it.Presentacion).ThenInclude(p => p.Color)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (d is null) return NotFound();

            var dto = new DevolucionCompraDetailDto
            {
                Id = d.Id, Fecha = d.Fecha, CompraId = d.CompraId, ProveedorId = d.ProveedorId,
                Proveedor = d.Proveedor?.Nombre ?? "(Sin proveedor)", Numero = d.Numero, Observaciones = d.Observaciones,
                FormaPagoId = d.FormaPagoId, FormaPago = d.FormaPago?.Nombre,
                Subtotal = d.Subtotal, Descuento = d.Descuento, Total = d.Total,
                Estado = d.Estado, Anulada = d.Anulada,
                Detalles = d.Detalles.Select(it => new DCItemDto
                {
                    Id = it.Id, PresentacionId = it.PresentacionId, Producto = it.Presentacion.Producto.Nombre,
                    Presentacion = it.Presentacion.Nombre, Unidad = it.Presentacion.Unidad.Nombre, Color = it.Presentacion.Color?.Nombre,
                    Cantidad = it.Cantidad, CostoUnitario = it.CostoUnitario, TotalLinea = it.TotalLinea, Notas = it.Notas
                }).ToList()
            };
            return Ok(dto);
        }

        // Crear (SALIDA inventario + 💵 Ingreso en caja si abierta)
        [Authorize(Roles = "Administrador,Admin,Compras")]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] DevolucionCompraCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var prov = await _db.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == dto.ProveedorId);
            if (prov is null) return BadRequest(new { message = "Proveedor no existe." });
            if (dto.Detalles.Any(i => i.Cantidad <= 0)) return BadRequest(new { message = "Todas las cantidades deben ser > 0." });
            if (dto.Detalles.Any(i => i.CostoUnitario < 0)) return BadRequest(new { message = "Costo unitario inválido." });

            var prsIds = dto.Detalles.Select(i => i.PresentacionId).Distinct().ToList();
            var prs = await _db.Presentaciones.Where(p => prsIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();
            if (prs.Count != prsIds.Count) return BadRequest(new { message = "Una o más presentaciones no existen." });

            var userId = GetUserId();
            using var tx = await _db.Database.BeginTransactionAsync();

            var stockMap = await _db.ProductoStocks.Where(s => prsIds.Contains(s.PresentacionId)).ToDictionaryAsync(s => s.PresentacionId);
            foreach (var i in dto.Detalles)
            {
                var disponible = stockMap.TryGetValue(i.PresentacionId, out var st) ? st.Cantidad : 0m;
                if (disponible < i.Cantidad)
                    return Conflict(new { message = "Stock insuficiente para devolver al proveedor.", presentacionId = i.PresentacionId, requerido = i.Cantidad, disponible });
            }

            var dev = new DevolucionCompra
            {
                Fecha = DateTime.UtcNow, CompraId = dto.CompraId, ProveedorId = dto.ProveedorId,
                Numero = string.IsNullOrWhiteSpace(dto.Numero) ? null : dto.Numero.Trim(),
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),
                FormaPagoId = dto.FormaPagoId, Estado = "Registrada", Anulada = false, UsuarioId = userId
            };

            decimal subtotal = 0m;
            foreach (var i in dto.Detalles)
            {
                var totalLinea = Math.Round(i.Cantidad * i.CostoUnitario, 2, MidpointRounding.AwayFromZero);
                subtotal += totalLinea;

                dev.Detalles.Add(new DevolucionCompraDetalle
                {
                    PresentacionId = i.PresentacionId, Cantidad = i.Cantidad,
                    CostoUnitario = i.CostoUnitario, TotalLinea = totalLinea,
                    Notas = string.IsNullOrWhiteSpace(i. Notas) ? null : i.Notas.Trim()
                });
            }
            dev.Subtotal = subtotal;
            dev.Descuento = 0m;
            dev.Total = subtotal;

            _db.Set<DevolucionCompra>().Add(dev);
            await _db.SaveChangesAsync();

            // Inventario (SALIDA)
            foreach (var d in dev.Detalles)
            {
                var stock = stockMap[d.PresentacionId];
                stock.Cantidad -= d.Cantidad;

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    FechaUtc = DateTime.UtcNow, PresentacionId = d.PresentacionId,
                    Tipo = TipoMovimiento.Salida, Cantidad = -d.Cantidad, CostoUnitario = d.CostoUnitario,
                    Documento = "DevolucionCompra", DocumentoId = dev.Id, Notas = d.Notas, UsuarioId = userId
                });
            }
            await _db.SaveChangesAsync();

            // 💵 Caja (si abierta): Ingreso (reintegro del proveedor)
            var estadoCaja = await _caja.EstadoAsync();
            if (estadoCaja.Abierta)
            {
                await _caja.AddMovimientoEnCajaAbiertaAsync(new LaOriginalBackend.Dtos.CajaMovimientoCreateDto
                {
                    Tipo = (int)TipoMovimientoCaja.Ingreso,
                    Monto = dev.Total,
                    Concepto = $"Reintegro por devolución a proveedor #{dev.Id}",
                    Documento = "DevolucionCompra",
                    DocumentoId = dev.Id
                }, userId);
            }

            await tx.CommitAsync();
            return CreatedAtAction(nameof(GetById), new { id = dev.Id }, new { dev.Id, message = "Devolución registrada y stock decrementado." });
        }

        // Anular (ENTRADA inventario + 💵 Egreso en caja si abierta)
        [Authorize(Roles = "Administrador,Admin,Compras")]
        [HttpPost("{id:int}/anular")]
        public async Task<ActionResult> Anular(int id, [FromBody] DCAnularDto dto)
        {
            var d = await _db.Set<DevolucionCompra>().Include(x => x.Detalles).FirstOrDefaultAsync(x => x.Id == id);
            if (d is null) return NotFound(new { message = "Devolución no encontrada." });
            if (d.Anulada) return Conflict(new { message = "La devolución ya está anulada." });

            using var tx = await _db.Database.BeginTransactionAsync();
            var userId = GetUserId();

            var prsIds = d.Detalles.Select(it => it.PresentacionId).Distinct().ToList();
            var stockMap = await _db.ProductoStocks.Where(s => prsIds.Contains(s.PresentacionId)).ToDictionaryAsync(s => s.PresentacionId);

            foreach (var it in d.Detalles)
            {
                if (!stockMap.TryGetValue(it.PresentacionId, out var stock))
                {
                    stock = new ProductoStock { PresentacionId = it.PresentacionId, Cantidad = 0m };
                    _db.ProductoStocks.Add(stock);
                    stockMap[it.PresentacionId] = stock;
                }
                stock.Cantidad += it.Cantidad;

                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    FechaUtc = DateTime.UtcNow, PresentacionId = it.PresentacionId,
                    Tipo = TipoMovimiento.Entrada, Cantidad = it.Cantidad, CostoUnitario = it.CostoUnitario,
                    Documento = "AnulacionDevolucionCompra", DocumentoId = d.Id,
                    Notas = string.IsNullOrWhiteSpace(dto.Motivo) ? "Anulación de devolución a proveedor" : dto.Motivo,
                    UsuarioId = userId
                });
            }

            d.Anulada = true;
            d.Estado = "Anulada";
            await _db.SaveChangesAsync();

            var estadoCaja = await _caja.EstadoAsync();
            if (estadoCaja.Abierta)
            {
                await _caja.AddMovimientoEnCajaAbiertaAsync(new LaOriginalBackend.Dtos.CajaMovimientoCreateDto
                {
                    Tipo = (int)TipoMovimientoCaja.Egreso,
                    Monto = d.Total,
                    Concepto = $"Salida por anulación de devolución a proveedor #{d.Id}",
                    Documento = "AnulacionDevolucionCompra",
                    DocumentoId = d.Id
                }, userId);
            }

            await tx.CommitAsync();
            return Ok(new { message = "Devolución a proveedor anulada y stock revertido.", id = d.Id });
        }
    }
}

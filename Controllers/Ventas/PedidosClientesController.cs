// Controllers/Ventas/PedidosClientesController.cs
using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using LaOriginalBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Data;
using System.Linq;

// Alias para movimientos de caja
using DCaja = LaOriginalBackend.Dtos;

namespace LaOriginalBackend.Controllers.Ventas
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PedidosClientesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICajaDomainService _caja;

        public PedidosClientesController(AppDbContext db, ICajaDomainService caja)
        {
            _db = db;
            _caja = caja;
        }

        // ========= Helpers =========

        private int? GetUserId()
        {
            var idClaim = User.FindFirst("id")?.Value
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : (int?)null;
        }

        private async Task<decimal> StockActualAsync(int presentacionId)
        {
            var st = await _db.ProductoStocks.AsNoTracking()
                .FirstOrDefaultAsync(s => s.PresentacionId == presentacionId);
            return st?.Cantidad ?? 0m;
        }

        private async Task<decimal> ReservadoAsync(int presentacionId, int? excluirPedidoId = null)
        {
            var q = _db.PedidosClientesReservas.AsNoTracking()
                .Where(r => r.PresentacionId == presentacionId);
            if (excluirPedidoId.HasValue) q = q.Where(r => r.PedidoClienteId != excluirPedidoId.Value);
            return await q.SumAsync(r => (decimal?)r.Cantidad) ?? 0m;
        }

        private async Task<decimal> DisponibleAsync(int presentacionId, int? excluirPedidoId = null)
        {
            var stock = await StockActualAsync(presentacionId);
            var reservado = await ReservadoAsync(presentacionId, excluirPedidoId);
            return stock - reservado;
        }

        private static decimal Redondear2(decimal v)
            => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        private static (bool ok, string? error) ValidarDetalles(IEnumerable<PedidoClienteDetalleDto>? detalles)
        {
            if (detalles == null) return (true, null);
            foreach (var d in detalles)
            {
                if (d.Cantidad <= 0) return (false, "La cantidad de cada ítem debe ser > 0.");
                if (d.PrecioUnitario < 0) return (false, "El precio unitario no puede ser negativo.");
                if (d.DescuentoUnitario < 0) return (false, "El descuento unitario no puede ser negativo.");
                if (d.DescuentoUnitario > d.PrecioUnitario) return (false, "El descuento unitario no puede ser mayor al precio unitario.");
            }
            return (true, null);
        }

        // Pago neto (cobros - devoluciones)
        private static decimal Neto(IEnumerable<PedidoClientePago> pagos)
        {
            var cobros = pagos.Where(x => !x.EsDevolucion).Sum(x => x.Monto);
            var devol = pagos.Where(x => x.EsDevolucion).Sum(x => x.Monto);
            return Math.Round(cobros - devol, 2, MidpointRounding.AwayFromZero);
        }

        // 📄 Texto legible desde diseño + extra (solo si hay datos reales)
        private static string? ComponerObservaciones(DisenoPedidoCliente? d, string? extra)
        {
            static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

            bool hayDatos =
                d != null && (
                    d.Lienzos > 0 ||
                    !string.IsNullOrWhiteSpace(d.Color) ||
                    !string.IsNullOrWhiteSpace(d.Otros) ||
                    d.Brich ||
                    d.Reportado is not null
                );

            var parts = new List<string>();
            if (hayDatos && d != null)
            {
                if (d.Lienzos > 0) parts.Add($"Lienzos: {d.Lienzos}");
                var color = TrimOrNull(d.Color);
                if (color != null) parts.Add($"Color: {color}");
                if (d.Brich) parts.Add("Brich: sí");
                var otros = TrimOrNull(d.Otros);
                if (otros != null) parts.Add(otros);
            }

            var cuerpo = string.Join(" · ", parts);

            var sb = new List<string>();
            if (!string.IsNullOrEmpty(cuerpo)) sb.Add(cuerpo);

            if (d?.Reportado is bool rep)
                sb.Add($"Reportado: {(rep ? "sí" : "no")}");

            var ext = TrimOrNull(extra);
            if (!string.IsNullOrEmpty(ext)) sb.Add(ext!);

            if (sb.Count == 0) return null;
            var txt = string.Join(" | ", sb);
            return txt.Length > 400 ? txt[..400] : txt;
        }

        // ===== Reservas / Inventario =====

        private async Task<IActionResult> SincronizarReservasAsync(PedidoCliente pedido)
        {
            if (pedido.Tipo != TipoPedidoCliente.Personalizado) return Ok();

            var existentes = await _db.PedidosClientesReservas
                .Where(r => r.PedidoClienteId == pedido.Id)
                .ToDictionaryAsync(r => r.PresentacionId, r => r);

            foreach (var d in pedido.Detalles)
            {
                var disponible = await DisponibleAsync(d.PresentacionId, pedido.Id);
                var cantNecesaria = d.Cantidad;

                if (existentes.TryGetValue(d.PresentacionId, out var resExist))
                    disponible += resExist.Cantidad;

                if (cantNecesaria > disponible)
                {
                    return Conflict(new
                    {
                        message = "Stock insuficiente para reservar.",
                        presentacionId = d.PresentacionId,
                        requerido = cantNecesaria,
                        disponible
                    });
                }

                if (existentes.TryGetValue(d.PresentacionId, out var r))
                    r.Cantidad = cantNecesaria;
                else
                    _db.PedidosClientesReservas.Add(new PedidoClienteReserva
                    {
                        PedidoClienteId = pedido.Id,
                        PresentacionId = d.PresentacionId,
                        Cantidad = cantNecesaria
                    });
            }

            var idsDetalle = pedido.Detalles.Select(x => x.PresentacionId).ToHashSet();
            var eliminar = existentes.Values.Where(r => !idsDetalle.Contains(r.PresentacionId)).ToList();
            if (eliminar.Count > 0) _db.PedidosClientesReservas.RemoveRange(eliminar);

            await _db.SaveChangesAsync();
            return Ok();
        }

        private async Task<IActionResult> DescargarInventarioAsync(PedidoCliente pedido)
        {
            if (pedido.Detalles.Count == 0) return Ok();

            var pids = pedido.Detalles.Select(d => d.PresentacionId).Distinct().ToArray();
            var stocks = await _db.ProductoStocks
                .Where(s => pids.Contains(s.PresentacionId))
                .ToDictionaryAsync(s => s.PresentacionId);

            foreach (var d in pedido.Detalles)
            {
                stocks.TryGetValue(d.PresentacionId, out var stock);
                var actual = stock?.Cantidad ?? 0m;

                if (d.Cantidad > actual)
                {
                    return Conflict(new
                    {
                        message = "Stock insuficiente para entregar.",
                        presentacionId = d.PresentacionId,
                        requerido = d.Cantidad,
                        stockActual = actual
                    });
                }

                var costoAplicado = stock?.CostoPromedio ?? 0m;

                // Salida (cantidad positiva)
                _db.MovimientosInventario.Add(new MovimientoInventario
                {
                    FechaUtc = DateTime.UtcNow,
                    PresentacionId = d.PresentacionId,
                    Tipo = TipoMovimiento.Salida,
                    Cantidad = d.Cantidad,
                    CostoUnitario = costoAplicado,
                    Documento = "PedidoCliente",
                    DocumentoId = pedido.Id,
                    Notas = "Descarga por entrega de pedido"
                });

                if (stock == null)
                {
                    stock = new ProductoStock { PresentacionId = d.PresentacionId, Cantidad = 0m, CostoPromedio = 0m };
                    _db.ProductoStocks.Add(stock);
                    stocks[d.PresentacionId] = stock;
                }
                stock.Cantidad -= d.Cantidad;
            }

            var reservas = await _db.PedidosClientesReservas
                .Where(r => r.PedidoClienteId == pedido.Id).ToListAsync();
            if (reservas.Count > 0) _db.PedidosClientesReservas.RemoveRange(reservas);

            await _db.SaveChangesAsync();
            return Ok();
        }

        // ========= Endpoints =========

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PedidoClienteListDto>>> Get(
            [FromQuery] string? term = null,
            [FromQuery] int? clienteId = null,
            [FromQuery] EstadoPedidoCliente? estado = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] int take = 100)
        {
            var q = _db.PedidosClientes.AsNoTracking().AsQueryable();

            if (clienteId.HasValue)
                q = q.Where(p => p.ClienteId == clienteId.Value);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                q = q.Where(p =>
                    EF.Functions.Like(p.ClienteNombre, $"%{t}%") ||
                    EF.Functions.Like(p.Observaciones ?? string.Empty, $"%{t}%"));
            }

            if (estado.HasValue)
                q = q.Where(p => p.Estado == estado.Value);

            if (desde.HasValue)
            {
                var desdeUtc = DateTime.SpecifyKind(desde.Value, DateTimeKind.Utc);
                q = q.Where(p => p.FechaCreacionUtc >= desdeUtc);
            }
            if (hasta.HasValue)
            {
                var hastaUtc = DateTime.SpecifyKind(hasta.Value.Date.AddDays(1), DateTimeKind.Utc);
                q = q.Where(p => p.FechaCreacionUtc < hastaUtc);
            }

            var total = await q.CountAsync();

            // PAGO NETO en la grilla
            var baseQuery =
                from p in q
                join pago in _db.PedidosClientesPagos.AsNoTracking()
                    on p.Id equals pago.PedidoClienteId into pagosGrp
                select new
                {
                    p.Id,
                    p.FechaCreacionUtc,
                    p.ClienteNombre,
                    p.Observaciones,
                    p.Estado,
                    p.Tipo,
                    p.Total,
                    PagadoNeto = pagosGrp.Sum(x => (decimal?)(x.EsDevolucion ? -x.Monto : x.Monto)) ?? 0m
                };

            var ordered = baseQuery.OrderByDescending(x => x.FechaCreacionUtc);

            List<PedidoClienteListDto> list;

            if (page.HasValue || pageSize.HasValue)
            {
                var ps = Math.Clamp(pageSize ?? 50, 1, 500);
                var pg = Math.Max(1, page ?? 1);
                var skip = (pg - 1) * ps;

                var rows = await ordered
                    .Skip(skip)
                    .Take(ps)
                    .ToListAsync();

                list = rows.Select(x => new PedidoClienteListDto
                {
                    Id = x.Id,
                    FechaCreacionUtc = DateTime.SpecifyKind(x.FechaCreacionUtc, DateTimeKind.Utc),
                    Cliente = x.ClienteNombre,
                    Descripcion = x.Observaciones,
                    Estado = x.Estado,
                    Tipo = x.Tipo,
                    Total = x.Total,
                    CuentaAlDia = x.PagadoNeto >= x.Total
                }).ToList();

                Response.Headers["X-Total-Count"] = total.ToString();
                return Ok(list);
            }
            else
            {
                var rows = await ordered
                    .Take(Math.Clamp(take, 1, 500))
                    .ToListAsync();

                list = rows.Select(x => new PedidoClienteListDto
                {
                    Id = x.Id,
                    FechaCreacionUtc = DateTime.SpecifyKind(x.FechaCreacionUtc, DateTimeKind.Utc),
                    Cliente = x.ClienteNombre,
                    Descripcion = x.Observaciones,
                    Estado = x.Estado,
                    Tipo = x.Tipo,
                    Total = x.Total,
                    CuentaAlDia = x.PagadoNeto >= x.Total
                }).ToList();

                Response.Headers["X-Total-Count"] = total.ToString();
                return Ok(list);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PedidoClienteDetailDto>> GetById(int id)
        {
            var p = await _db.PedidosClientes
                .AsNoTracking()
                .Include(x => x.Detalles)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p is null) return NotFound();

            var pagosEnt = await _db.PedidosClientesPagos
                .AsNoTracking()
                .Where(x => x.PedidoClienteId == id)
                .OrderBy(x => x.FechaUtc)
                .ToListAsync();

            var cobrado = pagosEnt.Where(x => !x.EsDevolucion).Sum(x => x.Monto);
            var devuelto = pagosEnt.Where(x => x.EsDevolucion).Sum(x => x.Monto);
            var pagadoNeto = Redondear2(cobrado - devuelto);
            var saldo = Redondear2(p.Total - pagadoNeto);

            PedidoClienteDisenoDto? disenoDto = null;
            if (p.Diseno != null)
            {
                disenoDto = new PedidoClienteDisenoDto
                {
                    Lienzos = p.Diseno.Lienzos,
                    Color = p.Diseno.Color,
                    Brich = p.Diseno.Brich,
                    Otros = p.Diseno.Otros,
                    Reportado = p.Diseno.Reportado,
                    Extra = p.Diseno.Extra
                };
            }

            DateTime? fechaComp = p.FechaEntregaCompromisoUtc.HasValue
                ? DateTime.SpecifyKind(p.FechaEntregaCompromisoUtc.Value, DateTimeKind.Utc)
                : (DateTime?)null;

            return Ok(new PedidoClienteDetailDto
            {
                Id = p.Id,
                ClienteId = p.ClienteId,
                ClienteNombre = p.ClienteNombre,
                Telefono = p.Telefono,
                DireccionEntrega = p.DireccionEntrega,
                FechaEntregaCompromisoUtc = fechaComp,
                // agregado para el detalle:
                FechaCreacionUtc = DateTime.SpecifyKind(p.FechaCreacionUtc, DateTimeKind.Utc),

                Estado = p.Estado,
                Tipo = p.Tipo,
                Observaciones = p.Observaciones,
                Diseno = disenoDto,
                Subtotal = p.Subtotal,
                Descuento = p.Descuento,
                Total = p.Total,
                Detalles = p.Detalles.Select(d => new PedidoClienteDetalleDto
                {
                    Id = d.Id,
                    PresentacionId = d.PresentacionId,
                    PresentacionNombre = d.PresentacionNombre,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    DescuentoUnitario = d.DescuentoUnitario,
                    TotalLinea = d.TotalLinea,
                    Notas = d.Notas
                }).ToList(),
                Pagos = pagosEnt.Select(x => new PedidoClientePagoDto
                {
                    Id = x.Id,
                    FechaUtc = DateTime.SpecifyKind(x.FechaUtc, DateTimeKind.Utc),
                    FormaPagoId = x.FormaPagoId,
                    FormaPagoNombre = x.FormaPagoNombre,
                    Monto = x.Monto,
                    Referencia = x.Referencia,
                    Notas = x.Notas,
                    EsDevolucion = x.EsDevolucion,
                    PagoOriginalId = x.PagoOriginalId
                }).ToList(),
                MontoPagado = pagadoNeto,
                Saldo = saldo,
                TotalCobrado = cobrado,
                TotalDevuelto = devuelto
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PedidoClienteCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (okDetalles, errDetalles) = ValidarDetalles(dto.Detalles);
            if (!okDetalles) return BadRequest(new { message = errDetalles });

            var tieneItems = (dto.Detalles?.Count ?? 0) > 0;

            if (dto.Tipo == TipoPedidoCliente.Personalizado && !tieneItems)
                return BadRequest(new { message = "El pedido Personalizado debe tener al menos un ítem." });

            if (dto.Tipo == TipoPedidoCliente.Completo && !tieneItems && dto.Total < 0)
                return BadRequest(new { message = "El total no puede ser negativo." });

            if (dto.Total < 0)
                return BadRequest(new { message = "El total no puede ser negativo." });

            if (dto.FechaEntregaCompromisoUtc.HasValue &&
                dto.FechaEntregaCompromisoUtc.Value.Date < DateTime.UtcNow.Date)
                return BadRequest(new { message = "La fecha de entrega no puede ser en el pasado." });

            DateTime? fechaComp = dto.FechaEntregaCompromisoUtc.HasValue
                ? DateTime.SpecifyKind(dto.FechaEntregaCompromisoUtc.Value, DateTimeKind.Utc)
                : (DateTime?)null;

            var entity = new PedidoCliente
            {
                ClienteId = dto.ClienteId,
                ClienteNombre = dto.ClienteNombre.Trim(),
                Telefono = dto.Telefono?.Trim(),
                DireccionEntrega = dto.DireccionEntrega?.Trim(),
                FechaEntregaCompromisoUtc = fechaComp,
                Estado = dto.Estado,
                Tipo = dto.Tipo,
                Subtotal = dto.Subtotal,
                Descuento = dto.Descuento,
                Total = dto.Total
            };

            if (dto.Diseno != null)
            {
                entity.Diseno = new DisenoPedidoCliente
                {
                    Lienzos = dto.Diseno.Lienzos,
                    Color = string.IsNullOrWhiteSpace(dto.Diseno.Color) ? null : dto.Diseno.Color!.Trim(),
                    Brich = dto.Diseno.Brich,
                    Otros = string.IsNullOrWhiteSpace(dto.Diseno.Otros) ? null : dto.Diseno.Otros!.Trim(),
                    Reportado = dto.Diseno.Reportado,
                    Extra = string.IsNullOrWhiteSpace(dto.Diseno.Extra) ? null : dto.Diseno.Extra!.Trim()
                };
            }

            entity.Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones)
                ? ComponerObservaciones(dto.Diseno != null ? new DisenoPedidoCliente
                {
                    Lienzos = dto.Diseno.Lienzos,
                    Color = dto.Diseno.Color,
                    Brich = dto.Diseno.Brich,
                    Otros = dto.Diseno.Otros,
                    Reportado = dto.Diseno.Reportado,
                    Extra = dto.Diseno.Extra
                }
                    : null,
                    dto.Diseno?.Extra)
                : dto.Observaciones!.Trim();

            foreach (var d in (dto.Detalles ?? Enumerable.Empty<PedidoClienteDetalleDto>()))
            {
                entity.Detalles.Add(new PedidoClienteDetalle
                {
                    PresentacionId = d.PresentacionId,
                    PresentacionNombre = d.PresentacionNombre?.Trim(),
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    DescuentoUnitario = d.DescuentoUnitario,
                    TotalLinea = d.TotalLinea,
                    Notas = d.Notas?.Trim()
                });
            }

            _db.PedidosClientes.Add(entity);
            await _db.SaveChangesAsync();

            if (entity.Estado == EstadoPedidoCliente.Confirmado &&
                entity.Tipo == TipoPedidoCliente.Personalizado &&
                entity.Detalles.Count > 0)
            {
                var sync = await SincronizarReservasAsync(entity);
                if (sync is ConflictObjectResult) return sync;
            }

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new { entity.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] PedidoClienteUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var entity = await _db.PedidosClientes
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity is null) return NotFound();

            if (entity.Estado != EstadoPedidoCliente.Borrador &&
                entity.Estado != EstadoPedidoCliente.Confirmado)
                return Conflict(new { message = "Solo se puede editar en Borrador o Confirmado." });

            var (okDetalles, errDetalles) = ValidarDetalles(dto.Detalles);
            if (!okDetalles) return BadRequest(new { message = errDetalles });

            var tieneItems = (dto.Detalles?.Count ?? 0) > 0;

            if (dto.Tipo == TipoPedidoCliente.Personalizado && !tieneItems)
                return BadRequest(new { message = "El pedido Personalizado debe tener al menos un ítem." });

            if (dto.Total < 0)
                return BadRequest(new { message = "El total no puede ser negativo." });

            if (dto.FechaEntregaCompromisoUtc.HasValue &&
                dto.FechaEntregaCompromisoUtc.Value.Date < DateTime.UtcNow.Date)
                return BadRequest(new { message = "La fecha de entrega no puede ser en el pasado." });

            DateTime? fechaComp = dto.FechaEntregaCompromisoUtc.HasValue
                ? DateTime.SpecifyKind(dto.FechaEntregaCompromisoUtc.Value, DateTimeKind.Utc)
                : (DateTime?)null;

            entity.ClienteId = dto.ClienteId;
            entity.ClienteNombre = dto.ClienteNombre.Trim();
            entity.Telefono = dto.Telefono?.Trim();
            entity.DireccionEntrega = dto.DireccionEntrega?.Trim();
            entity.FechaEntregaCompromisoUtc = fechaComp;
            entity.Subtotal = dto.Subtotal;
            entity.Descuento = dto.Descuento;
            entity.Total = dto.Total;
            entity.Estado = dto.Estado;
            entity.Tipo = dto.Tipo;

            if (dto.Diseno == null)
            {
                entity.Diseno ??= new DisenoPedidoCliente();
                entity.Diseno.Lienzos = 0;
                entity.Diseno.Color = null;
                entity.Diseno.Brich = false;
                entity.Diseno.Otros = null;
                entity.Diseno.Reportado = null;
                entity.Diseno.Extra = null;
            }
            else
            {
                entity.Diseno ??= new DisenoPedidoCliente();
                entity.Diseno.Lienzos = dto.Diseno.Lienzos;
                entity.Diseno.Color = string.IsNullOrWhiteSpace(dto.Diseno.Color) ? null : dto.Diseno.Color!.Trim();
                entity.Diseno.Brich = dto.Diseno.Brich;
                entity.Diseno.Otros = string.IsNullOrWhiteSpace(dto.Diseno.Otros) ? null : dto.Diseno.Otros!.Trim();
                entity.Diseno.Reportado = dto.Diseno.Reportado;
                entity.Diseno.Extra = string.IsNullOrWhiteSpace(dto.Diseno.Extra) ? null : dto.Diseno.Extra!.Trim();
            }

            entity.Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones)
                ? ComponerObservaciones(dto.Diseno != null ? new DisenoPedidoCliente
                {
                    Lienzos = dto.Diseno.Lienzos,
                    Color = dto.Diseno.Color,
                    Brich = dto.Diseno.Brich,
                    Otros = dto.Diseno.Otros,
                    Reportado = dto.Diseno.Reportado,
                    Extra = dto.Diseno.Extra
                }
                    : null,
                    dto.Diseno?.Extra)
                : dto.Observaciones!.Trim();

            _db.PedidosClientesDetalles.RemoveRange(entity.Detalles);
            entity.Detalles.Clear();

            foreach (var d in (dto.Detalles ?? Enumerable.Empty<PedidoClienteDetalleDto>()))
            {
                entity.Detalles.Add(new PedidoClienteDetalle
                {
                    PresentacionId = d.PresentacionId,
                    PresentacionNombre = d.PresentacionNombre?.Trim(),
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    DescuentoUnitario = d.DescuentoUnitario,
                    TotalLinea = d.TotalLinea,
                    Notas = d.Notas?.Trim()
                });
            }

            await _db.SaveChangesAsync();

            if (entity.Estado == EstadoPedidoCliente.Confirmado &&
                entity.Tipo == TipoPedidoCliente.Personalizado)
            {
                if (entity.Detalles.Count > 0)
                {
                    var sync = await SincronizarReservasAsync(entity);
                    if (sync is ConflictObjectResult) return sync;
                }
                else
                {
                    var reservas = await _db.PedidosClientesReservas
                        .Where(r => r.PedidoClienteId == entity.Id).ToListAsync();
                    if (reservas.Count > 0)
                    {
                        _db.PedidosClientesReservas.RemoveRange(reservas);
                        await _db.SaveChangesAsync();
                    }
                }
            }
            else
            {
                var reservas = await _db.PedidosClientesReservas
                    .Where(r => r.PedidoClienteId == entity.Id).ToListAsync();
                if (reservas.Count > 0)
                {
                    _db.PedidosClientesReservas.RemoveRange(reservas);
                    await _db.SaveChangesAsync();
                }
            }

            return NoContent();
        }

        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] PedidoClienteEstadoDto dto)
        {
            using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var entity = await _db.PedidosClientes
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity is null) return NotFound();

            bool ok = (entity.Estado, dto.NuevoEstado) switch
            {
                (EstadoPedidoCliente.Borrador, EstadoPedidoCliente.Confirmado) => true,
                (EstadoPedidoCliente.Confirmado, EstadoPedidoCliente.EnPreparacion) => true,
                (EstadoPedidoCliente.EnPreparacion, EstadoPedidoCliente.Listo) => true,
                (EstadoPedidoCliente.Listo, EstadoPedidoCliente.Entregado) => true,
                (EstadoPedidoCliente.Cancelado, EstadoPedidoCliente.Borrador) => true,
                (_, EstadoPedidoCliente.Cancelado) => true,
                _ => false
            };

            bool esReversionPorDevolucion = false;
            if (!ok && dto.NuevoEstado == EstadoPedidoCliente.Borrador && entity.Estado != EstadoPedidoCliente.Cancelado)
            {
                var pagosForZero = await _db.PedidosClientesPagos
                    .Where(x => x.PedidoClienteId == id).ToListAsync();

                if (Neto(pagosForZero) <= 0m)
                {
                    esReversionPorDevolucion = true;
                    ok = true;
                }
            }

            if (!ok) return Conflict(new { message = "Transición de estado no permitida." });

            if (entity.Estado == EstadoPedidoCliente.Borrador && dto.NuevoEstado == EstadoPedidoCliente.Confirmado)
            {
                if (entity.Tipo == TipoPedidoCliente.Personalizado && entity.Detalles.Count > 0)
                {
                    var sync = await SincronizarReservasAsync(entity);
                    if (sync is ConflictObjectResult) return sync;
                }
            }
            else if (dto.NuevoEstado == EstadoPedidoCliente.Cancelado)
            {
                var reservas = await _db.PedidosClientesReservas
                    .Where(r => r.PedidoClienteId == entity.Id).ToListAsync();
                if (reservas.Count > 0) _db.PedidosClientesReservas.RemoveRange(reservas);
                await _db.SaveChangesAsync();
            }
            else if (entity.Estado == EstadoPedidoCliente.Listo && dto.NuevoEstado == EstadoPedidoCliente.Entregado)
            {
                if (entity.Detalles.Count > 0)
                {
                    var descarga = await DescargarInventarioAsync(entity);
                    if (descarga is ConflictObjectResult) return descarga;
                }
            }
            else if (esReversionPorDevolucion)
            {
                var reservas = await _db.PedidosClientesReservas
                    .Where(r => r.PedidoClienteId == entity.Id).ToListAsync();
                if (reservas.Count > 0)
                {
                    _db.PedidosClientesReservas.RemoveRange(reservas);
                    await _db.SaveChangesAsync();
                }
            }

            if (entity.Estado == EstadoPedidoCliente.Cancelado && dto.NuevoEstado == EstadoPedidoCliente.Borrador)
            {
                var pagos = await _db.PedidosClientesPagos
                    .Where(x => x.PedidoClienteId == id).ToListAsync();

                var pagadoNeto = Neto(pagos);

                if (pagadoNeto > 0m)
                {
                    entity.Estado = EstadoPedidoCliente.Confirmado;

                    if (entity.Tipo == TipoPedidoCliente.Personalizado && entity.Detalles.Count > 0)
                    {
                        var sync = await SincronizarReservasAsync(entity);
                        if (sync is ConflictObjectResult) return sync;
                    }
                }
                else
                {
                    entity.Estado = EstadoPedidoCliente.Borrador;
                }
            }
            else
            {
                entity.Estado = dto.NuevoEstado;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { entity.Id, entity.Estado });
        }

        [HttpPost("{id:int}/convertir-a-venta")]
        public async Task<IActionResult> ConvertirAVenta(int id)
        {
            using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var p = await _db.PedidosClientes
                .Include(x => x.Detalles)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p is null) return NotFound();

            if (p.VentaId.HasValue)
                return Conflict(new { message = "Este pedido ya se convirtió en venta.", ventaId = p.VentaId });

            if (p.Estado != EstadoPedidoCliente.Listo && p.Estado != EstadoPedidoCliente.Entregado)
                return Conflict(new { message = "Solo se puede convertir cuando el pedido está Listo o Entregado." });

            if (p.Detalles == null || p.Detalles.Count == 0)
                return Conflict(new { message = "No se puede convertir a venta un pedido sin ítems." });

            if (p.Estado == EstadoPedidoCliente.Listo)
            {
                var descarga = await DescargarInventarioAsync(p);
                if (descarga is ConflictObjectResult) return descarga;
                p.Estado = EstadoPedidoCliente.Entregado;
                await _db.SaveChangesAsync();
            }

            var venta = new Venta
            {
                Fecha = DateTime.UtcNow,
                ClienteId = p.ClienteId,
                Serie = null,
                Numero = null,
                Observaciones = $"Venta creada desde pedido #{p.Id}",
                Estado = "Registrada",
                Anulada = false,
                FormaPagoId = null,
                UsuarioId = p.UsuarioId
            };

            // Mapas para costo “snapshot”
            var pids = p.Detalles.Select(d => d.PresentacionId).Distinct().ToArray();

            var stockMap = await _db.ProductoStocks
                .Where(s => pids.Contains(s.PresentacionId))
                .ToDictionaryAsync(s => s.PresentacionId, s => s);

            var presMap = await _db.Presentaciones
                .Where(pr => pids.Contains(pr.Id))
                .ToDictionaryAsync(pr => pr.Id, pr => pr);

            static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

            decimal subtotal = 0m, descuento = 0m;
            foreach (var d in p.Detalles)
            {
                var st = stockMap.TryGetValue(d.PresentacionId, out var s) ? s : null;
                var costoProm = st?.CostoPromedio ?? 0m;

                // === Costo a "congelar" ===
                var costoSnap = (costoProm > 0m)
                    ? costoProm
                    : (presMap.TryGetValue(d.PresentacionId, out var pr)
                        ? (pr.PrecioCompraDefault ?? 0m)
                        : 0m);

                var totalLinea = Round2(d.Cantidad * (d.PrecioUnitario - d.DescuentoUnitario));
                subtotal += Round2(d.Cantidad * d.PrecioUnitario);
                descuento += Round2(d.Cantidad * d.DescuentoUnitario);

                venta.Detalles.Add(new VentaDetalle
                {
                    PresentacionId = d.PresentacionId,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    DescuentoUnitario = d.DescuentoUnitario,
                    TotalLinea = totalLinea,
                    CostoUnitario = costoSnap,   // snapshot
                    Notas = d.Notas
                });
            }
            venta.Subtotal = subtotal;
            venta.Descuento = descuento;
            venta.Total = subtotal - descuento;

            _db.Ventas.Add(venta);
            await _db.SaveChangesAsync();

            p.VentaId = venta.Id;
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return Ok(new { message = "Pedido convertido a venta.", venta.Id });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.PedidosClientes
                .Include(p => p.Detalles)
                .Include(p => p.Reservas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity is null) return NotFound();

            // 🚫 Bloqueo: no permitir borrar pedidos ya convertidos a venta
            if (entity.VentaId.HasValue)
                return Conflict(new { message = "No se puede eliminar un pedido ya convertido a venta.", ventaId = entity.VentaId });

            _db.PedidosClientesReservas.RemoveRange(entity.Reservas);
            _db.PedidosClientesDetalles.RemoveRange(entity.Detalles);
            _db.PedidosClientes.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ================= PAGOS =================

        [HttpGet("{id:int}/pagos")]
        public async Task<ActionResult<IEnumerable<PedidoClientePagoDto>>> GetPagos(int id)
        {
            var exists = await _db.PedidosClientes.AsNoTracking().AnyAsync(p => p.Id == id);
            if (!exists) return NotFound();

            var listEnt = await _db.PedidosClientesPagos
                .AsNoTracking()
                .Where(x => x.PedidoClienteId == id)
                .OrderBy(x => x.FechaUtc)
                .ToListAsync();

            var list = listEnt.Select(x => new PedidoClientePagoDto
            {
                Id = x.Id,
                FechaUtc = DateTime.SpecifyKind(x.FechaUtc, DateTimeKind.Utc),
                FormaPagoId = x.FormaPagoId,
                FormaPagoNombre = x.FormaPagoNombre,
                Monto = x.Monto,
                Referencia = x.Referencia,
                Notas = x.Notas,
                EsDevolucion = x.EsDevolucion,
                PagoOriginalId = x.PagoOriginalId
            }).ToList();

            return Ok(list);
        }

        [HttpPost("{id:int}/pagos")]
        public async Task<IActionResult> AgregarPago(int id, [FromBody] PedidoClientePagoCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var pedido = await _db.PedidosClientes
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (pedido is null) return NotFound();

            if (pedido.Estado == EstadoPedidoCliente.Cancelado)
                return Conflict(new { message = "No se pueden registrar pagos a pedidos cancelados." });

            var forma = await _db.FormasPago.AsNoTracking().FirstOrDefaultAsync(f => f.Id == dto.FormaPagoId);
            if (forma is null || !forma.Activo)
                return BadRequest(new { message = "Forma de pago inválida o inactiva." });

            if (forma.RequiereReferencia && string.IsNullOrWhiteSpace(dto.Referencia))
                return BadRequest(new { message = "Esta forma de pago requiere número de referencia." });

            if (forma.AfectaCaja)
            {
                var estado = await _caja.EstadoAsync();
                if (!estado.Abierta)
                    return BadRequest(new { message = "No hay caja abierta para registrar el cobro." });
            }

            using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var pagosExist = await _db.PedidosClientesPagos
                .Where(x => x.PedidoClienteId == id)
                .ToListAsync();

            var pagadoNeto = Neto(pagosExist);
            var intento = Redondear2(dto.Monto);

            if (pagadoNeto + intento > pedido.Total)
            {
                return Conflict(new
                {
                    message = "El pago excede el total del pedido.",
                    total = pedido.Total,
                    pagado = pagadoNeto,
                    intento = dto.Monto
                });
            }

            DateTime fechaPagoUtc =
                dto.FechaUtc.HasValue
                    ? (dto.FechaUtc.Value.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dto.FechaUtc.Value, DateTimeKind.Utc)
                        : dto.FechaUtc.Value.ToUniversalTime())
                    : DateTime.UtcNow;

            var pago = new PedidoClientePago
            {
                PedidoClienteId = id,
                FechaUtc = fechaPagoUtc,
                FormaPagoId = forma.Id,
                FormaPagoNombre = forma.Nombre,
                Monto = intento,
                Referencia = dto.Referencia?.Trim(),
                Notas = dto.Notas?.Trim(),
                EsDevolucion = false
            };

            _db.PedidosClientesPagos.Add(pago);
            await _db.SaveChangesAsync();

            if (forma.AfectaCaja)
            {
                await _caja.AddMovimientoEnCajaAbiertaAsync(new DCaja.CajaMovimientoCreateDto
                {
                    Tipo = (int)TipoMovimientoCaja.Ingreso,
                    Monto = pago.Monto,
                    Concepto = $"Pago pedido #{id} - {forma.Nombre}",
                    Documento = "PedidoClientePago",
                    DocumentoId = pago.Id
                }, usuarioId: GetUserId());
            }

            if (pedido.Estado == EstadoPedidoCliente.Borrador)
            {
                pedido.Estado = EstadoPedidoCliente.Confirmado;
                await _db.SaveChangesAsync();

                if (pedido.Tipo == TipoPedidoCliente.Personalizado && pedido.Detalles.Count > 0)
                {
                    var sync = await SincronizarReservasAsync(pedido);
                    if (sync is ConflictObjectResult) return sync;
                }
            }

            await tx.CommitAsync();
            return CreatedAtAction(nameof(GetPagos), new { id }, new { pago.Id });
        }

        // ========= Devoluciones =========
        [HttpPost("{id:int}/devoluciones")]
        public async Task<IActionResult> AgregarDevolucion(int id, [FromBody] PedidoClienteDevolucionCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.Monto <= 0) return BadRequest(new { message = "El monto a devolver debe ser > 0." });

            var pedido = await _db.PedidosClientes.FirstOrDefaultAsync(p => p.Id == id);
            if (pedido is null) return NotFound();

            var forma = await _db.FormasPago.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == dto.FormaPagoId && f.Activo);
            if (forma is null) return BadRequest(new { message = "Forma de pago inválida o inactiva." });

            if (forma.RequiereReferencia && string.IsNullOrWhiteSpace(dto.Referencia))
                return BadRequest(new { message = "Esta forma de pago requiere número de referencia." });

            if (forma.AfectaCaja)
            {
                var estado = await _caja.EstadoAsync();
                if (!estado.Abierta)
                    return BadRequest(new { message = "No hay caja abierta para registrar la devolución." });
            }

            await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var pagos = await _db.PedidosClientesPagos
                .Where(x => x.PedidoClienteId == id)
                .ToListAsync();

            var cobrado = pagos.Where(x => !x.EsDevolucion).Sum(x => x.Monto);
            var devuelto = pagos.Where(x => x.EsDevolucion).Sum(x => x.Monto);
            var saldoADevolver = Redondear2(cobrado - devuelto);

            var intento = Redondear2(dto.Monto);
            if (saldoADevolver <= 0m)
                return Conflict(new { message = "No hay saldo a devolver para este pedido." });

            if (intento > saldoADevolver)
                return Conflict(new
                {
                    message = "La devolución excede lo cobrado neto del pedido.",
                    cobrado,
                    devuelto,
                    saldoDisponibleParaDevolver = saldoADevolver,
                    intento = dto.Monto
                });

            DateTime fechaDevUtc =
                dto.FechaUtc.HasValue
                    ? (dto.FechaUtc.Value.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dto.FechaUtc.Value, DateTimeKind.Utc)
                        : dto.FechaUtc.Value.ToUniversalTime())
                    : DateTime.UtcNow;

            if (dto.PagoOriginalId.HasValue)
            {
                var original = pagos.FirstOrDefault(x => x.Id == dto.PagoOriginalId.Value);
                if (original is null || original.PedidoClienteId != id || original.EsDevolucion)
                    return BadRequest(new { message = "Pago original inválido." });

                var yaDevueltoDeEsePago = pagos
                    .Where(x => x.EsDevolucion && x.PagoOriginalId == original.Id)
                    .Sum(x => x.Monto);

                var saldoDeEsePago = Redondear2(original.Monto - yaDevueltoDeEsePago);
                if (saldoDeEsePago <= 0m)
                    return Conflict(new { message = "Ese pago ya fue devuelto totalmente." });

                if (intento > saldoDeEsePago)
                    return Conflict(new
                    {
                        message = "La devolución excede el saldo del pago original.",
                        pagoOriginal = original.Id,
                        montoPagoOriginal = original.Monto,
                        yaDevueltoDeEsePago,
                        saldoDisponibleDeEsePago = saldoDeEsePago,
                        intento = dto.Monto
                    });
            }

            var devol = new PedidoClientePago
            {
                PedidoClienteId = id,
                FechaUtc = fechaDevUtc,
                FormaPagoId = forma.Id,
                FormaPagoNombre = forma.Nombre,
                Monto = intento,
                Referencia = dto.Referencia?.Trim(),
                Notas = dto.Notas?.Trim(),
                EsDevolucion = true,
                PagoOriginalId = dto.PagoOriginalId
            };

            _db.PedidosClientesPagos.Add(devol);
            await _db.SaveChangesAsync();

            try
            {
                if (forma.AfectaCaja)
                {
                    await _caja.AddMovimientoEnCajaAbiertaAsync(new DCaja.CajaMovimientoCreateDto
                    {
                        Tipo = (int)TipoMovimientoCaja.Egreso,
                        Monto = devol.Monto,
                        Concepto = $"Devolución pedido #{id} - {forma.Nombre}",
                        Documento = "PedidoClienteDevolucion",
                        DocumentoId = devol.Id
                    }, usuarioId: GetUserId());
                }

                var pagos2 = await _db.PedidosClientesPagos
                    .Where(x => x.PedidoClienteId == id)
                    .ToListAsync();

                var pagadoNeto = Neto(pagos2);
                if (pagadoNeto <= 0m && pedido.Estado != EstadoPedidoCliente.Cancelado)
                {
                    pedido.Estado = EstadoPedidoCliente.Borrador;
                    await _db.SaveChangesAsync();

                    var reservas = await _db.PedidosClientesReservas
                        .Where(r => r.PedidoClienteId == id)
                        .ToListAsync();

                    if (reservas.Count > 0)
                    {
                        _db.PedidosClientesReservas.RemoveRange(reservas);
                        await _db.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();
                return CreatedAtAction(nameof(GetPagos), new { id }, new { devol.Id });
            }
            catch (CajaFondosInsuficientesException ex)
            {
                await tx.RollbackAsync();
                // Respuesta limpia para el front
                return Conflict(new
                {
                    error = "Fondos insuficientes en caja",
                    message = $"Fondos insuficientes. Disponible Q {Math.Round(ex.Disponible, 2):0.00}, solicitado Q {Math.Round(ex.Solicitado, 2):0.00}.",
                    disponible = Math.Round(ex.Disponible, 2),
                    solicitado = Math.Round(ex.Solicitado, 2)
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("No hay caja", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync();
                return BadRequest(new { message = "No puedes registrar devoluciones: la caja está cerrada." });
            }
        }

        // ========= Disponible / catálogo =========

        [HttpGet("disponible/{presentacionId:int}")]
        public async Task<ActionResult<StockDisponibleDto>> GetDisponiblePorPresentacion(
            int presentacionId, [FromQuery] int? excluirPedidoId = null)
        {
            var stock = await _db.ProductoStocks
                .AsNoTracking()
                .Where(s => s.PresentacionId == presentacionId)
                .Select(s => new { s.PresentacionId, s.Cantidad, s.Presentacion.PrecioVentaDefault })
                .FirstOrDefaultAsync();

            var reservado = await _db.PedidosClientesReservas
                .AsNoTracking()
                .Where(r => r.PresentacionId == presentacionId &&
                            (!excluirPedidoId.HasValue || r.PedidoClienteId != excluirPedidoId.Value))
                .SumAsync(r => (decimal?)r.Cantidad) ?? 0m;

            var stockCant = stock?.Cantidad ?? 0m;
            var dto = new StockDisponibleDto
            {
                PresentacionId = presentacionId,
                Stock = stockCant,
                Reservado = reservado,
                Disponible = stockCant - reservado,
                PrecioVenta = stock?.PrecioVentaDefault
            };

            return Ok(dto);
        }

        [HttpGet("disponible")]
        public async Task<ActionResult<IEnumerable<StockDisponibleDto>>> GetDisponiblePorPresentaciones(
            [FromQuery] int[] ids, [FromQuery] int? excluirPedidoId = null)
        {
            if (ids == null || ids.Length == 0) return Ok(Array.Empty<StockDisponibleDto>());

            var idsSet = ids.Distinct().ToArray();

            var stockMap = await _db.ProductoStocks
                .AsNoTracking()
                .Where(s => idsSet.Contains(s.PresentacionId))
                .Select(s => new { s.PresentacionId, s.Cantidad, s.Presentacion.PrecioVentaDefault })
                .ToDictionaryAsync(x => x.PresentacionId, x => x);

            var reservas = await _db.PedidosClientesReservas
                .AsNoTracking()
                .Where(r => idsSet.Contains(r.PresentacionId) &&
                            (!excluirPedidoId.HasValue || r.PedidoClienteId != excluirPedidoId.Value))
                .GroupBy(r => r.PresentacionId)
                .Select(g => new { PresentacionId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .ToDictionaryAsync(x => x.PresentacionId, x => x.Cantidad);

            var result = idsSet.Select(pid =>
            {
                var st = stockMap.GetValueOrDefault(pid);
                var reservado = reservas.GetValueOrDefault(pid);
                var stockCant = st?.Cantidad ?? 0m;

                return new StockDisponibleDto
                {
                    PresentacionId = pid,
                    Stock = stockCant,
                    Reservado = reservado,
                    Disponible = stockCant - reservado,
                    PrecioVenta = st?.PrecioVentaDefault
                };
            }).ToList();

            return Ok(result);
        }

        // ====== CATÁLOGO basado en INVENTARIO (ProductoStocks) ======
        [HttpGet("catalogo")]
        public async Task<ActionResult<IEnumerable<object>>> CatalogoConDisponible(
            [FromQuery] string? term = null,
            [FromQuery] bool soloActivos = true,
            [FromQuery] int take = 50,
            [FromQuery] int? excluirPedidoId = null,
            [FromQuery] int? categoriaId = null)
        {
            var q = _db.ProductoStocks
                .AsNoTracking()
                .Include(s => s.Presentacion).ThenInclude(p => p.Producto).ThenInclude(pr => pr.Categoria)
                .Include(s => s.Presentacion).ThenInclude(p => p.Unidad)
                .AsQueryable();

            if (soloActivos)
                q = q.Where(s => s.Presentacion.Activo && s.Presentacion.Producto.Activo);

            if (categoriaId.HasValue)
                q = q.Where(s => s.Presentacion.Producto.CategoriaId == categoriaId.Value);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                q = q.Where(s =>
                    EF.Functions.Like(s.Presentacion.Producto.Nombre, $"%{t}%") ||
                    EF.Functions.Like(s.Presentacion.Nombre, $"%{t}%") ||
                    (s.Presentacion.Producto.Codigo != null && EF.Functions.Like(s.Presentacion.Producto.Codigo, $"%{t}%")) ||
                    (s.Presentacion.SKU != null && EF.Functions.Like(s.Presentacion.SKU, $"%{t}%")) ||
                    (s.Presentacion.CodigoBarras != null && EF.Functions.Like(s.Presentacion.CodigoBarras, $"%{t}%")));
            }

            var filas = await q
                .OrderBy(s => s.Presentacion.Producto.Nombre).ThenBy(s => s.Presentacion.Nombre)
                .Take(Math.Clamp(take, 1, 200))
                .Select(s => new
                {
                    Id = s.PresentacionId,
                    ProductoId = s.Presentacion.ProductoId,
                    Producto = s.Presentacion.Producto.Nombre,
                    ProductoCodigo = s.Presentacion.Producto.Codigo,
                    Nombre = s.Presentacion.Nombre,
                    PrecioVentaDefault = s.Presentacion.PrecioVentaDefault,
                    Unidad = s.Presentacion.Unidad != null ? s.Presentacion.Unidad.Simbolo : null,
                    FotoUrl = s.Presentacion.Producto.FotoUrl,
                    Stock = s.Cantidad,
                    CategoriaId = s.Presentacion.Producto.CategoriaId,
                    Categoria = s.Presentacion.Producto.Categoria != null ? s.Presentacion.Producto.Categoria.Nombre : null
                })
                .ToListAsync();

            var ids = filas.Select(x => x.Id).ToArray();

            var reservas = await _db.PedidosClientesReservas
                .AsNoTracking()
                .Where(r => ids.Contains(r.PresentacionId) &&
                            (!excluirPedidoId.HasValue || r.PedidoClienteId != excluirPedidoId.Value))
                .GroupBy(r => r.PresentacionId)
                .Select(g => new { PresentacionId = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .ToDictionaryAsync(x => x.PresentacionId, x => x.Cantidad);

            var result = filas.Select(p =>
            {
                var reservado = reservas.GetValueOrDefault(p.Id);
                var disponible = (p.Stock) - (reservado);
                return new
                {
                    p.Id,
                    p.ProductoId,
                    p.Producto,
                    p.ProductoCodigo,
                    p.Nombre,
                    p.PrecioVentaDefault,
                    p.Unidad,
                    p.FotoUrl,
                    Stock = p.Stock,
                    Reservado = reservado,
                    Disponible = disponible,
                    p.CategoriaId,
                    p.Categoria
                };
            }).ToList();

            return Ok(result);
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaOriginalBackend.Data;
using LaOriginalBackend.Models;
using LaOriginalBackend.Dtos.Reportes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Controllers.Reportes
{
    [ApiController]
    [Route("api/reportes")]
    public class ReportesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReportesController(AppDbContext db) => _db = db;

        // ======= Zona horaria empresarial (Linux/Windows) =======
        private static readonly TimeZoneInfo AppTz = ResolveTimeZone(
            "America/Guatemala", "Central America Standard Time"
        );

        private static TimeZoneInfo ResolveTimeZone(params string[] ids)
        {
            foreach (var id in ids)
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch { }
            }
            return TimeZoneInfo.Utc;
        }

        // Convierte "fecha de calendario LOCAL" (00:00) a límites UTC inclusivo/exclusivo
        private static DateTime? LocalStartUtc(DateTime? d)
        {
            if (!d.HasValue) return null;
            var localStart = DateTime.SpecifyKind(d.Value.Date, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(localStart, AppTz);
        }

        private static DateTime? LocalEndExclUtc(DateTime? d)
        {
            if (!d.HasValue) return null;
            var localEnd = DateTime.SpecifyKind(d.Value.Date.AddDays(1), DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(localEnd, AppTz);
        }

        private static DateTime ToLocal(DateTime utc)
        {
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, AppTz);
        }

        private static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        private IQueryable<Venta> VentasFiltradas(DateTime? desde, DateTime? hasta)
        {
            var d = LocalStartUtc(desde);
            var h = LocalEndExclUtc(hasta);

            var q = _db.Ventas.AsNoTracking().Where(v => !v.Anulada);
            if (d.HasValue) q = q.Where(v => v.Fecha >= d.Value);
            if (h.HasValue) q = q.Where(v => v.Fecha < h.Value);
            return q;
        }

        private IQueryable<VentaDetalle> DetallesFiltrados(DateTime? desde, DateTime? hasta)
        {
            var d = LocalStartUtc(desde);
            var h = LocalEndExclUtc(hasta);

            var q = from det in _db.VentasDetalle.AsNoTracking()
                    join v in _db.Ventas.AsNoTracking() on det.VentaId equals v.Id
                    where !v.Anulada
                    select new { det, v.Fecha };

            if (d.HasValue) q = q.Where(x => x.Fecha >= d.Value);
            if (h.HasValue) q = q.Where(x => x.Fecha < h.Value);

            return q.Select(x => x.det);
        }

        // ========= Ventas diarias (por día LOCAL) =========
        [HttpGet("ventas/diarias")]
        public async Task<ActionResult<IEnumerable<VentaDiariaDto>>> VentasDiarias(
            [FromQuery] DateTime? Desde, [FromQuery] DateTime? Hasta)
        {
            var qVentas = VentasFiltradas(Desde, Hasta);

            // Traemos lo necesario y agrupamos por día LOCAL en memoria
            var ventas = await qVentas
                .Select(v => new { v.Fecha, v.Subtotal, v.Descuento, v.Total })
                .ToListAsync();

            var porDia = ventas
                .GroupBy(v =>
                {
                    var utc = DateTime.SpecifyKind(v.Fecha, DateTimeKind.Utc);
                    return ToLocal(utc).Date;
                })
                .Select(g => new
                {
                    Fecha = g.Key,
                    Ventas = g.Count(),
                    Subtotal = g.Sum(x => (decimal?)x.Subtotal) ?? 0m,
                    Descuento = g.Sum(x => (decimal?)x.Descuento) ?? 0m,
                    Total = g.Sum(x => (decimal?)x.Total) ?? 0m
                })
                .OrderBy(x => x.Fecha)
                .ToList();

            // Items vendidos por día (LOCAL)
            var qDet = from d in _db.VentasDetalle.AsNoTracking()
                       join v in _db.Ventas.AsNoTracking() on d.VentaId equals v.Id
                       where !v.Anulada
                       select new { v.Fecha, d.Cantidad };

            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);
            if (d0.HasValue) qDet = qDet.Where(x => x.Fecha >= d0);
            if (d1.HasValue) qDet = qDet.Where(x => x.Fecha < d1);

            var dets = await qDet.ToListAsync();

            var itemsPorDia = dets
                .GroupBy(x =>
                {
                    var utc = DateTime.SpecifyKind(x.Fecha, DateTimeKind.Utc);
                    return ToLocal(utc).Date;
                })
                .Select(g => new { Fecha = g.Key, Items = g.Sum(x => (decimal?)x.Cantidad) ?? 0m })
                .ToDictionary(x => x.Fecha, x => x.Items);

            var result = porDia.Select(x => new VentaDiariaDto
            {
                Fecha = x.Fecha.ToString("yyyy-MM-dd"),
                Ventas = x.Ventas,
                Items = itemsPorDia.TryGetValue(x.Fecha, out var n) ? n : 0m,
                Subtotal = x.Subtotal,
                Descuento = x.Descuento,
                Total = x.Total
            }).ToList();

            return Ok(result);
        }

        [HttpGet("ventas/por-usuario")]
        public async Task<ActionResult<IEnumerable<VentasPorUsuarioDto>>> VentasPorUsuario(
            [FromQuery] DateTime? Desde, [FromQuery] DateTime? Hasta,
            [FromQuery] bool incluirUtilidad = false)
        {
            var baseRows = await VentasFiltradas(Desde, Hasta)
                .GroupBy(v => v.UsuarioId)
                .Select(g => new
                {
                    UsuarioId = g.Key,
                    Ventas = g.Count(),
                    Total = g.Sum(x => (decimal?)x.Total) ?? 0m
                })
                .ToListAsync();

            var ids = baseRows.Where(x => x.UsuarioId.HasValue)
                              .Select(x => x.UsuarioId!.Value)
                              .Distinct()
                              .ToArray();

            var nombres = await _db.Usuarios.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    Nombre = ((u.PrimerNombre ?? "") + " " + (u.PrimerApellido ?? "")).Trim()
                })
                .ToDictionaryAsync(x => x.Id, x => x.Nombre);

            Dictionary<int?, decimal>? utilMap = null;

            if (incluirUtilidad)
            {
                try
                {
                    var d0 = LocalStartUtc(Desde);
                    var d1 = LocalEndExclUtc(Hasta);

                    var dets = await (
                        from vd in _db.VentasDetalle.AsNoTracking()
                        join v in _db.Ventas.AsNoTracking() on vd.VentaId equals v.Id
                        join pr0 in _db.Presentaciones.AsNoTracking()
                            on vd.PresentacionId equals pr0.Id into jpr
                        from pr in jpr.DefaultIfEmpty()
                        where !v.Anulada
                              && (!d0.HasValue || v.Fecha >= d0.Value)
                              && (!d1.HasValue || v.Fecha < d1.Value)
                        select new
                        {
                            v.UsuarioId,
                            vd.TotalLinea,
                            vd.Cantidad,
                            CostoUnit = (decimal?)vd.CostoUnitario,
                            PrecioCompraDefault = (decimal?)pr.PrecioCompraDefault
                        }
                    ).ToListAsync();

                    utilMap = dets
                        .GroupBy(x => x.UsuarioId)
                        .ToDictionary(
                            g => g.Key,
                            g => R2(g.Sum(x =>
                            {
                                var costoUnit = (x.CostoUnit.HasValue && x.CostoUnit.Value > 0m)
                                    ? x.CostoUnit.Value
                                    : (x.PrecioCompraDefault ?? 0m);
                                return x.TotalLinea - (costoUnit * x.Cantidad);
                            })));
                }
                catch
                {
                    utilMap = new Dictionary<int?, decimal>();
                    incluirUtilidad = false;
                }
            }

            var result = baseRows.Select(x =>
            {
                var nombre = x.UsuarioId.HasValue &&
                             nombres.TryGetValue(x.UsuarioId.Value, out var n) &&
                             !string.IsNullOrWhiteSpace(n)
                    ? n
                    : "Sin asignar";

                return new VentasPorUsuarioDto
                {
                    UsuarioId = x.UsuarioId,
                    Usuario = nombre,
                    Ventas = x.Ventas,
                    Total = x.Total,
                    TicketPromedio = x.Ventas > 0 ? R2(x.Total / x.Ventas) : 0m,
                    Utilidad = incluirUtilidad ? utilMap!.GetValueOrDefault(x.UsuarioId, 0m) : (decimal?)null
                };
            })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Usuario)
            .ToList();

            return Ok(result);
        }

        // ========= Top clientes =========
        [HttpGet("ventas/top-clientes")]
        public async Task<ActionResult<IEnumerable<ClienteTopDto>>> TopClientes(
            [FromQuery] DateTime? Desde, [FromQuery] DateTime? Hasta, [FromQuery] int top = 10)
        {
            top = Math.Clamp(top, 1, 100);

            var q = VentasFiltradas(Desde, Hasta)
                .Select(v => new
                {
                    v.Id,
                    v.ClienteId,
                    Cliente = v.Cliente != null ? v.Cliente.Nombre : "Consumidor Final",
                    v.Total,
                    v.Fecha
                });

            var rows = await q
                .GroupBy(x => new { x.ClienteId, x.Cliente })
                .Select(g => new ClienteTopDto
                {
                    ClienteId = g.Key.ClienteId ?? 0,
                    Cliente = g.Key.Cliente,
                    Compras = g.Count(),
                    Total = g.Sum(z => (decimal?)z.Total) ?? 0m,
                    UltimaCompra = g.Max(z => (DateTime?)z.Fecha)
                })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.Cliente)
                .Take(top)
                .ToListAsync();

            return Ok(rows);
        }

        // ========= Ventas por producto =========
        [HttpGet("ventas/por-producto")]
        public async Task<ActionResult<IEnumerable<VentasPorProductoDto>>> VentasPorProducto(
            [FromQuery] DateTime? Desde, [FromQuery] DateTime? Hasta)
        {
            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);

            var q = from det in _db.VentasDetalle.AsNoTracking()
                    join v in _db.Ventas.AsNoTracking() on det.VentaId equals v.Id
                    where !v.Anulada
                    select new { det, v.Fecha };

            if (d0.HasValue) q = q.Where(x => x.Fecha >= d0);
            if (d1.HasValue) q = q.Where(x => x.Fecha < d1);

            var rows = await q
                .GroupBy(x => new
                {
                    x.det.PresentacionId,
                    Producto = x.det.Presentacion!.Producto.Nombre,
                    Presentacion = x.det.Presentacion.Nombre,
                    Categoria = x.det.Presentacion.Producto.Categoria != null
                                ? x.det.Presentacion.Producto.Categoria.Nombre
                                : ""
                })
                .Select(g => new VentasPorProductoDto
                {
                    PresentacionId = g.Key.PresentacionId,
                    Producto = g.Key.Producto,
                    Presentacion = g.Key.Presentacion,
                    Categoria = g.Key.Categoria,
                    CantidadVendida = g.Sum(z => (decimal?)z.det.Cantidad) ?? 0m,
                    Total = g.Sum(z => (decimal?)z.det.TotalLinea) ?? 0m
                })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.Producto)
                .ToListAsync();

            return Ok(rows);
        }

        // ========= Ventas por categoría =========
        [HttpGet("ventas/por-categoria")]
        public async Task<ActionResult<IEnumerable<VentasPorCategoriaDto>>> VentasPorCategoria(
            [FromQuery] DateTime? Desde, [FromQuery] DateTime? Hasta)
        {
            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);

            var q = from det in _db.VentasDetalle.AsNoTracking()
                    join v in _db.Ventas.AsNoTracking() on det.VentaId equals v.Id
                    where !v.Anulada
                    select new { det, v.Fecha };

            if (d0.HasValue) q = q.Where(x => x.Fecha >= d0);
            if (d1.HasValue) q = q.Where(x => x.Fecha < d1);

            var rows = await q
                .GroupBy(x => new
                {
                    CategoriaId = x.det.Presentacion!.Producto.CategoriaId,
                    Categoria = x.det.Presentacion!.Producto.Categoria != null
                                ? x.det.Presentacion.Producto.Categoria.Nombre
                                : ""
                })
                .Select(g => new VentasPorCategoriaDto
                {
                    CategoriaId = g.Key.CategoriaId,
                    Categoria = g.Key.Categoria,
                    CantidadVendida = g.Sum(z => (decimal?)z.det.Cantidad) ?? 0m,
                    Total = g.Sum(z => (decimal?)z.det.TotalLinea) ?? 0m
                })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.Categoria)
                .ToListAsync();

            return Ok(rows);
        }

        // ========= GANANCIA por producto =========
        [HttpGet("ganancia/por-producto")]
        public async Task<ActionResult<IEnumerable<GananciaPorProductoDto>>> GananciaPorProducto(
            [FromQuery] DateTime? Desde, [FromQuery] DateTime? Hasta)
        {
            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);

            var q = from det in _db.VentasDetalle.AsNoTracking()
                    join v in _db.Ventas.AsNoTracking() on det.VentaId equals v.Id
                    where !v.Anulada
                    select new { det, v.Fecha };

            if (d0.HasValue) q = q.Where(x => x.Fecha >= d0);
            if (d1.HasValue) q = q.Where(x => x.Fecha < d1);

            var rows = await q
                .GroupBy(x => new
                {
                    x.det.PresentacionId,
                    Producto = x.det.Presentacion!.Producto.Nombre,
                    Presentacion = x.det.Presentacion.Nombre,
                    Categoria = x.det.Presentacion.Producto.Categoria != null
                                ? x.det.Presentacion.Producto.Categoria.Nombre
                                : ""
                })
                .Select(g => new
                {
                    g.Key.PresentacionId,
                    g.Key.Producto,
                    g.Key.Presentacion,
                    g.Key.Categoria,
                    Cant = g.Sum(z => (decimal?)z.det.Cantidad) ?? 0m,
                    Venta = g.Sum(z => (decimal?)z.det.TotalLinea) ?? 0m,
                    Costo = g.Sum(z => (decimal?)(
                        ((EF.Property<decimal?>(z.det, nameof(z.det.CostoUnitario)) ?? 0m) > 0m
                            ? EF.Property<decimal?>(z.det, nameof(z.det.CostoUnitario)) ?? 0m
                            : (z.det.Presentacion.PrecioCompraDefault ?? 0m)
                        ) * z.det.Cantidad
                    )) ?? 0m
                })
                .ToListAsync();

            var result = rows
                .Select(x =>
                {
                    var venta = x.Venta;
                    var costo = x.Costo;
                    var utilidad = venta - costo;
                    var margenPct = venta > 0 ? (utilidad / venta * 100m) : 0m;

                    return new GananciaPorProductoDto
                    {
                        PresentacionId = x.PresentacionId,
                        Producto = x.Producto,
                        Presentacion = x.Presentacion,
                        Categoria = x.Categoria,
                        Cantidad = R2(x.Cant),
                        Venta = R2(venta),
                        Costo = R2(costo),
                        Utilidad = R2(utilidad),
                        MargenPct = R2(margenPct)
                    };
                })
                .OrderByDescending(x => x.Utilidad)
                .ThenBy(x => x.Producto)
                .ToList();

            return Ok(result);
        }

        // ========= GANANCIA RESUMEN (totales del rango) =========
        [HttpGet("ganancia/resumen")]
        public async Task<ActionResult<GananciaResumenDto>> GananciaResumen(
            [FromQuery] DateTime? Desde = null, [FromQuery] DateTime? Hasta = null)
        {
            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);

            var q = from det in _db.VentasDetalle.AsNoTracking()
                    join v in _db.Ventas.AsNoTracking() on det.VentaId equals v.Id
                    where !v.Anulada
                          && (!d0.HasValue || v.Fecha >= d0.Value)
                          && (!d1.HasValue || v.Fecha < d1.Value)
                    select new
                    {
                        det.TotalLinea,
                        CostoCalculado = (
                        (det.CostoUnitario > 0m ? det.CostoUnitario
                         : (det.Presentacion.PrecioCompraDefault ?? 0m))
                        * det.Cantidad
                    )
                    };


            var venta = await q.SumAsync(x => (decimal?)x.TotalLinea) ?? 0m;
            var costo = await q.SumAsync(x => (decimal?)x.CostoCalculado) ?? 0m;
            var utilidad = venta - costo;
            var margenPct = venta > 0 ? (utilidad / venta * 100m) : 0m;

            var dto = new GananciaResumenDto
            {
                DesdeUtc = d0,
                HastaUtc = d1,
                Venta = R2(venta),
                Costo = R2(costo),
                Utilidad = R2(utilidad),
                MargenPct = R2(margenPct)
            };

            return Ok(dto);
        }

        // ========= Ventas por forma de pago =========
        [HttpGet("ventas/por-forma-pago")]
        public async Task<ActionResult<IEnumerable<VentasPorFormaPagoDto>>> VentasPorFormaPago(
            [FromQuery] DateTime? Desde, [FromQuery] DateTime? Hasta)
        {
            var q = VentasFiltradas(Desde, Hasta)
                .Select(v => new
                {
                    v.FormaPagoId,
                    Nombre = v.FormaPago != null ? v.FormaPago.Nombre : "Sin forma de pago",
                    v.Total
                });

            var rows = await q.GroupBy(x => new { x.FormaPagoId, x.Nombre })
                .Select(g => new VentasPorFormaPagoDto
                {
                    FormaPagoId = g.Key.FormaPagoId,
                    FormaPago = g.Key.Nombre,
                    Ventas = g.Count(),
                    Total = g.Sum(z => (decimal?)z.Total) ?? 0m,
                    TicketPromedio = g.Count() > 0
                        ? R2((g.Sum(z => (decimal?)z.Total) ?? 0m) / g.Count())
                        : 0m
                })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.FormaPago)
                .ToListAsync();

            return Ok(rows);
        }

        // ========= Compras por proveedor =========
        [HttpGet("compras/por-proveedor")]
        public async Task<ActionResult<IEnumerable<ComprasPorProveedorDto>>> ComprasPorProveedor(
            [FromQuery] DateTime? Desde, [FromQuery] DateTime? Hasta)
        {
            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);

            var q = _db.Compras.AsNoTracking().Where(c => !c.Anulada);
            if (d0.HasValue) q = q.Where(c => c.Fecha >= d0.Value);
            if (d1.HasValue) q = q.Where(c => c.Fecha < d1.Value);

            var rows = await q.Select(c => new
            {
                c.ProveedorId,
                Proveedor = c.Proveedor != null ? c.Proveedor.Nombre : "(Sin proveedor)",
                c.Total,
                c.Fecha
            })
                .GroupBy(x => new { x.ProveedorId, x.Proveedor })
                .Select(g => new ComprasPorProveedorDto
                {
                    ProveedorId = g.Key.ProveedorId,
                    Proveedor = g.Key.Proveedor,
                    Documentos = g.Count(),
                    Total = g.Sum(z => (decimal?)z.Total) ?? 0m,
                    UltimaCompra = g.Max(z => (DateTime?)z.Fecha)
                })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.Proveedor)
                .ToListAsync();

            return Ok(rows);
        }

        // ========= Caja: ingresos/egresos diarios (día LOCAL) =========
        [HttpGet("caja/ingresos-egresos-diarios")]
        public async Task<ActionResult<IEnumerable<CajaDiariaDto>>> CajaDiaria(
            [FromQuery] DateTime? Desde, [FromQuery] DateTime? Hasta)
        {
            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);

            var q = _db.CajaMovimientos.AsNoTracking().AsQueryable();
            if (d0.HasValue) q = q.Where(m => m.FechaUtc >= d0.Value);
            if (d1.HasValue) q = q.Where(m => m.FechaUtc < d1.Value);

            var tmp = await q
                .Select(m => new { m.FechaUtc, m.Tipo, m.Monto })
                .ToListAsync();

            var rows = tmp
                .GroupBy(m => ToLocal(DateTime.SpecifyKind(m.FechaUtc, DateTimeKind.Utc)).Date)
                .Select(g => new CajaDiariaDto
                {
                    Fecha = g.Key.ToString("yyyy-MM-dd"),
                    Ingresos = g.Where(m => m.Tipo == TipoMovimientoCaja.Ingreso || m.Tipo == TipoMovimientoCaja.CobroVenta)
                                .Sum(m => m.Monto),
                    Egresos = g.Where(m => m.Tipo == TipoMovimientoCaja.Egreso || m.Tipo == TipoMovimientoCaja.PagoProveedor)
                                .Sum(m => m.Monto)
                })
                .OrderBy(x => x.Fecha)
                .ToList();

            return Ok(rows);
        }

        // ========= Caja: sesiones cerradas (día LOCAL para CierreDia) =========
        [HttpGet("caja/sesiones-cerradas")]
        public async Task<ActionResult<IEnumerable<CajaSesionCerradaDto>>> CajaSesionesCerradas(
            [FromQuery] DateTime? Desde, [FromQuery] DateTime? Hasta)
        {
            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);

            var aperturas = _db.CajaAperturas.AsNoTracking()
                .Where(a => a.FechaCierreUtc != null);

            if (d0.HasValue) aperturas = aperturas.Where(a => a.FechaCierreUtc! >= d0.Value);
            if (d1.HasValue) aperturas = aperturas.Where(a => a.FechaCierreUtc! < d1.Value);

            var baseRows = await aperturas
                .Select(a => new
                {
                    a.Id,
                    a.Codigo,
                    a.CajeroNombre,
                    a.MontoInicial,
                    a.FechaAperturaUtc,
                    a.FechaCierreUtc
                })
                .OrderByDescending(a => a.FechaCierreUtc)
                .ToListAsync();

            var ids = baseRows.Select(x => x.Id).ToArray();

            var movsAgg = await _db.CajaMovimientos.AsNoTracking()
                .Where(m => ids.Contains(m.CajaAperturaId))
                .GroupBy(m => m.CajaAperturaId)
                .Select(g => new
                {
                    Id = g.Key,
                    Ingresos = g.Where(m => m.Tipo == TipoMovimientoCaja.Ingreso || m.Tipo == TipoMovimientoCaja.CobroVenta)
                                .Sum(m => (decimal?)m.Monto) ?? 0m,
                    Egresos = g.Where(m => m.Tipo == TipoMovimientoCaja.Egreso || m.Tipo == TipoMovimientoCaja.PagoProveedor)
                                .Sum(m => (decimal?)m.Monto) ?? 0m
                })
                .ToDictionaryAsync(x => x.Id, x => x);

            var list = baseRows.Select(a =>
            {
                var agg = movsAgg.GetValueOrDefault(a.Id);
                var ingresos = agg?.Ingresos ?? 0m;
                var egresos = agg?.Egresos ?? 0m;

                var cierreUtc = DateTime.SpecifyKind(a.FechaCierreUtc!.Value, DateTimeKind.Utc);
                var aperturaUtc = DateTime.SpecifyKind(a.FechaAperturaUtc, DateTimeKind.Utc);
                var cierreLocal = ToLocal(cierreUtc);

                return new CajaSesionCerradaDto
                {
                    AperturaId = a.Id,
                    Codigo = a.Codigo ?? $"A-{a.Id:D4}",
                    CajeroNombre = a.CajeroNombre,
                    MontoInicial = R2(a.MontoInicial),
                    Ingresos = R2(ingresos),
                    Egresos = R2(egresos),
                    Neto = R2(a.MontoInicial + ingresos - egresos),
                    FechaAperturaUtc = aperturaUtc,
                    FechaCierreUtc = cierreUtc,
                    CierreDia = cierreLocal.ToString("yyyy-MM-dd")
                };
            })
            .OrderByDescending(x => x.FechaCierreUtc)
            .ToList();

            return Ok(list);
        }

        // ========================================================================
        // =======================  REPORTES DE PEDIDOS  ==========================
        // ========================================================================

        [HttpGet("pedidos/cobros-forma-pago")]
        public async Task<ActionResult<ReporteCobrosFormaPagoResponseDto>> CobrosPorFormaPagoPedidos(
            [FromQuery] DateTime? Desde = null,
            [FromQuery] DateTime? Hasta = null,
            [FromQuery] int? clienteId = null,
            [FromQuery] int? formaPagoId = null,
            [FromQuery] bool incluirCancelados = false)
        {
            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);

            var pagos = _db.PedidosClientesPagos.AsNoTracking()
                .Join(_db.PedidosClientes.AsNoTracking(),
                    pago => pago.PedidoClienteId,
                    ped => ped.Id,
                    (pago, ped) => new { pago, ped })
                .AsQueryable();

            if (d0.HasValue) pagos = pagos.Where(x => x.pago.FechaUtc >= d0.Value);
            if (d1.HasValue) pagos = pagos.Where(x => x.pago.FechaUtc < d1.Value);
            if (clienteId.HasValue) pagos = pagos.Where(x => x.ped.ClienteId == clienteId.Value);
            if (formaPagoId.HasValue) pagos = pagos.Where(x => x.pago.FormaPagoId == formaPagoId.Value);
            if (!incluirCancelados) pagos = pagos.Where(x => x.ped.Estado != EstadoPedidoCliente.Cancelado);

            var rows = await pagos
                .GroupBy(x => new { x.pago.FormaPagoId, x.pago.FormaPagoNombre })
                .Select(g => new ReporteCobrosFormaPagoRowDto
                {
                    FormaPagoId = g.Key.FormaPagoId,
                    FormaPago = g.Key.FormaPagoNombre,
                    Cobros = g.Where(z => !z.pago.EsDevolucion).Sum(z => (decimal?)z.pago.Monto) ?? 0m,
                    Devoluciones = g.Where(z => z.pago.EsDevolucion).Sum(z => (decimal?)z.pago.Monto) ?? 0m,
                    CantCobros = g.Count(z => !z.pago.EsDevolucion),
                    CantDevoluciones = g.Count(z => z.pago.EsDevolucion),
                    FechaMin = g.Min(z => (DateTime?)z.pago.FechaUtc),
                    FechaMax = g.Max(z => (DateTime?)z.pago.FechaUtc)
                })
                .OrderByDescending(r => r.Cobros - r.Devoluciones)
                .ToListAsync();

            foreach (var r in rows)
            {
                r.Cobros = R2(r.Cobros);
                r.Devoluciones = R2(r.Devoluciones);
                r.Neto = R2(r.Cobros - r.Devoluciones);
            }

            var totalCobros = R2(rows.Sum(r => r.Cobros));
            var totalDevs = R2(rows.Sum(r => r.Devoluciones));
            var totalNeto = R2(totalCobros - totalDevs);

            var result = new ReporteCobrosFormaPagoResponseDto
            {
                DesdeUtc = d0,
                HastaUtc = d1,
                TotalCobros = totalCobros,
                TotalDevoluciones = totalDevs,
                TotalNeto = totalNeto,
                Filas = rows
            };

            return Ok(result);
        }

        [HttpGet("pedidos/cobros-detalle")]
        public async Task<ActionResult<IEnumerable<ReporteCobrosDetalleDto>>> CobrosDetallePedidos(
            [FromQuery] DateTime? Desde = null,
            [FromQuery] DateTime? Hasta = null,
            [FromQuery] int? clienteId = null,
            [FromQuery] int? formaPagoId = null,
            [FromQuery] bool incluirCancelados = false,
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] int take = 200)
        {
            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);

            var q = _db.PedidosClientesPagos.AsNoTracking()
                .Join(_db.PedidosClientes.AsNoTracking(),
                    pago => pago.PedidoClienteId,
                    ped => ped.Id,
                    (pago, ped) => new { pago, ped })
                .AsQueryable();

            if (d0.HasValue) q = q.Where(x => x.pago.FechaUtc >= d0.Value);
            if (d1.HasValue) q = q.Where(x => x.pago.FechaUtc < d1.Value);
            if (clienteId.HasValue) q = q.Where(x => x.ped.ClienteId == clienteId.Value);
            if (formaPagoId.HasValue) q = q.Where(x => x.pago.FormaPagoId == formaPagoId.Value);
            if (!incluirCancelados) q = q.Where(x => x.ped.Estado != EstadoPedidoCliente.Cancelado);

            var total = await q.CountAsync();

            var ordered = q.OrderByDescending(x => x.pago.FechaUtc)
                           .ThenByDescending(x => x.pago.Id);

            IEnumerable<ReporteCobrosDetalleDto> map(IEnumerable<dynamic> rows) =>
                rows.Select(x => new ReporteCobrosDetalleDto
                {
                    PagoId = x.pago.Id,
                    PedidoId = x.pago.PedidoClienteId,
                    FechaUtc = DateTime.SpecifyKind(x.pago.FechaUtc, DateTimeKind.Utc),
                    EsDevolucion = x.pago.EsDevolucion,
                    Monto = R2(x.pago.Monto),
                    FormaPagoId = x.pago.FormaPagoId,
                    FormaPago = x.pago.FormaPagoNombre,
                    Referencia = x.pago.Referencia,
                    Notas = x.pago.Notas,
                    ClienteId = x.ped.ClienteId,
                    Cliente = x.ped.ClienteNombre,
                    EstadoPedido = x.ped.Estado
                });

            if (page.HasValue || pageSize.HasValue)
            {
                var ps = Math.Clamp(pageSize ?? 50, 1, 500);
                var pg = Math.Max(1, page ?? 1);
                var skip = (pg - 1) * ps;

                var rows = await ordered.Skip(skip).Take(ps).ToListAsync();
                Response.Headers["X-Total-Count"] = total.ToString();
                return Ok(map(rows));
            }
            else
            {
                var rows = await ordered.Take(Math.Clamp(take, 1, 1000)).ToListAsync();
                Response.Headers["X-Total-Count"] = total.ToString();
                return Ok(map(rows));
            }
        }

        [HttpGet("pedidos/estados")]
        public async Task<ActionResult<IEnumerable<ReportePedidosPorEstadoRowDto>>> PedidosPorEstado(
            [FromQuery] DateTime? Desde = null,
            [FromQuery] DateTime? Hasta = null,
            [FromQuery] int? clienteId = null)
        {
            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);

            var pedidos = _db.PedidosClientes.AsNoTracking().AsQueryable();
            if (d0.HasValue) pedidos = pedidos.Where(p => p.FechaCreacionUtc >= d0.Value);
            if (d1.HasValue) pedidos = pedidos.Where(p => p.FechaCreacionUtc < d1.Value);
            if (clienteId.HasValue) pedidos = pedidos.Where(p => p.ClienteId == clienteId.Value);

            var pedidosList = await pedidos
                .Select(p => new { p.Id, p.Estado, p.Total })
                .ToListAsync();

            var ids = pedidosList.Select(p => p.Id).ToArray();

            var pagos = await _db.PedidosClientesPagos.AsNoTracking()
                .Where(x => ids.Contains(x.PedidoClienteId))
                .GroupBy(x => x.PedidoClienteId)
                .Select(g => new
                {
                    PedidoId = g.Key,
                    Neto = (g.Sum(z => z.EsDevolucion ? -z.Monto : z.Monto))
                })
                .ToListAsync();

            var netoMap = pagos.ToDictionary(x => x.PedidoId, x => R2(x.Neto));

            var rows = pedidosList
                .GroupBy(p => p.Estado)
                .Select(g =>
                {
                    var totalPedidos = g.Sum(z => z.Total);
                    var pagado = g.Sum(z => netoMap.GetValueOrDefault(z.Id));
                    var saldo = totalPedidos - pagado;
                    return new ReportePedidosPorEstadoRowDto
                    {
                        Estado = g.Key,
                        Cantidad = g.Count(),
                        Total = R2(totalPedidos),
                        PagadoNeto = R2(pagado),
                        Saldo = R2(saldo)
                    };
                })
                .OrderBy(r => r.Estado)
                .ToList();

            return Ok(rows);
        }

        [HttpGet("pedidos/top-productos")]
        public async Task<ActionResult<IEnumerable<ReporteTopProductoRowDto>>> TopProductosPedidos(
            [FromQuery] DateTime? Desde = null,
            [FromQuery] DateTime? Hasta = null,
            [FromQuery] int take = 10,
            [FromQuery] bool incluirBorrador = false,
            [FromQuery] bool incluirCancelado = false,
            [FromQuery] int? categoriaId = null)
        {
            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);

            var detalles = _db.PedidosClientesDetalles.AsNoTracking()
                .Join(_db.PedidosClientes.AsNoTracking(),
                    d => d.PedidoClienteId,
                    p => p.Id,
                    (d, p) => new { d, p })
                .AsQueryable();

            if (d0.HasValue) detalles = detalles.Where(x => x.p.FechaCreacionUtc >= d0.Value);
            if (d1.HasValue) detalles = detalles.Where(x => x.p.FechaCreacionUtc < d1.Value);
            if (!incluirBorrador) detalles = detalles.Where(x => x.p.Estado != EstadoPedidoCliente.Borrador);
            if (!incluirCancelado) detalles = detalles.Where(x => x.p.Estado != EstadoPedidoCliente.Cancelado);

            if (categoriaId.HasValue)
            {
                detalles = detalles
                    .Join(_db.Presentaciones.AsNoTracking(),
                        x => x.d.PresentacionId,
                        pr => pr.Id,
                        (x, pr) => new { x.d, x.p, pr })
                    .Where(z => z.pr.Producto.CategoriaId == categoriaId.Value)
                    .Select(z => new { d = z.d, p = z.p });
            }

            var rows = await detalles
                .GroupBy(x => new { x.d.PresentacionId, x.d.PresentacionNombre })
                .Select(g => new ReporteTopProductoRowDto
                {
                    PresentacionId = g.Key.PresentacionId,
                    Presentacion = g.Key.PresentacionNombre,
                    Cantidad = g.Sum(z => (decimal?)z.d.Cantidad) ?? 0m,
                    Importe = g.Sum(z => (decimal?)(z.d.Cantidad * (z.d.PrecioUnitario - z.d.DescuentoUnitario))) ?? 0m
                })
                .OrderByDescending(r => r.Cantidad)
                .ThenByDescending(r => r.Importe)
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync();

            foreach (var r in rows)
            {
                r.Cantidad = R2(r.Cantidad);
                r.Importe = R2(r.Importe);
            }

            return Ok(rows);
        }

        // ========= Reportes de USUARIOS =========
        [HttpGet("usuarios/resumen")]
        public async Task<ActionResult<ReporteUsuariosResumenDto>> UsuariosResumen(
            [FromQuery] DateTime? Desde = null,
            [FromQuery] DateTime? Hasta = null,
            [FromQuery] int? rolId = null,
            [FromQuery] string? estado = null)
        {
            var d0 = LocalStartUtc(Desde);
            var d1 = LocalEndExclUtc(Hasta);

            var q = _db.Usuarios.AsNoTracking().AsQueryable();
            if (rolId.HasValue) q = q.Where(u => u.RolId == rolId.Value);
            if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(u => u.Estado == estado);

            var total = await q.CountAsync();
            var activos = await q.CountAsync(u => u.Estado == "Activo");
            var inactivos = await q.CountAsync(u => u.Estado == "Inactivo");
            var suspendidos = await q.CountAsync(u => u.Estado == "Suspendido");

            var porRol = await q.Include(u => u.Rol)
                .GroupBy(u => new { u.RolId, Rol = u.Rol != null ? u.Rol.Nombre : "(Sin rol)" })
                .Select(g => new UsuariosPorRolDto
                {
                    RolId = g.Key.RolId,
                    Rol = g.Key.Rol,
                    Total = g.Count(),
                    Activos = g.Count(u => u.Estado == "Activo"),
                    Inactivos = g.Count(u => u.Estado == "Inactivo"),
                    Suspendidos = g.Count(u => u.Estado == "Suspendido")
                })
                .OrderByDescending(x => x.Total)
                .ToListAsync();

            // Altas por mes dentro del rango (si se indicó)
            var qAltas = q;
            if (d0.HasValue) qAltas = qAltas.Where(u => u.FechaIngreso >= d0.Value);
            if (d1.HasValue) qAltas = qAltas.Where(u => u.FechaIngreso < d1.Value);

            var altasPorMes = await qAltas
                .GroupBy(u => new { u.FechaIngreso.Year, u.FechaIngreso.Month })
                .Select(g => new AltasPorMesDto { Anio = g.Key.Year, Mes = g.Key.Month, Cantidad = g.Count() })
                .OrderBy(x => x.Anio).ThenBy(x => x.Mes)
                .ToListAsync();

            var cumplesPorMes = await _db.Usuarios.AsNoTracking()
                .GroupBy(u => u.FechaNacimiento.Month)
                .Select(g => new CumplesPorMesDto { Mes = g.Key, Cantidad = g.Count() })
                .OrderBy(x => x.Mes)
                .ToListAsync();

            return Ok(new ReporteUsuariosResumenDto
            {
                DesdeUtc = d0,
                HastaUtc = d1,
                Total = total,
                Activos = activos,
                Inactivos = inactivos,
                Suspendidos = suspendidos,
                PorRol = porRol,
                AltasPorMes = altasPorMes,
                CumplesPorMes = cumplesPorMes
            });
        }
    }
}

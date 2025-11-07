// Controllers/PedidosProveedoresController.cs
using System.Data;
using System.Globalization;
using System.Text;
using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using LaOriginalBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosProveedoresController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICajaDomainService _caja;

        public PedidosProveedoresController(AppDbContext db, ICajaDomainService caja)
        {
            _db = db;
            _caja = caja;
        }

        // ---------- helpers ----------
        private static void RecalcularTotales(PedidoProveedor pedido)
        {
            var sub = pedido.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
            var desc = pedido.Detalles.Sum(d => d.Descuento);
            pedido.Subtotal = Math.Round(sub, 2);
            pedido.Descuento = Math.Round(desc, 2);
            pedido.Total = Math.Round(sub - desc, 2);
        }

        private static bool EsColisionUnique(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sql)
                return sql.Number == 2601 || sql.Number == 2627;
            return false;
        }

        private async Task<string> GenerarNumeroAsync()
        {
            var año = DateTime.UtcNow.Year;
            var prefix = $"OC-{año}-";
            var ultimo = await _db.PedidosProveedores
                .Where(p => p.Numero != null && p.Numero.StartsWith(prefix))
                .OrderByDescending(p => p.Numero)
                .Select(p => p.Numero!)
                .FirstOrDefaultAsync();

            var n = 1;
            if (!string.IsNullOrEmpty(ultimo))
            {
                var suf = ultimo.Substring(prefix.Length);
                if (int.TryParse(suf, out var k)) n = k + 1;
            }
            return $"{prefix}{n.ToString().PadLeft(4, '0')}";
        }

        private static string Norm(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var formD = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (var ch in formD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return sb.ToString().ToLowerInvariant().Trim();
        }

        // ================= LISTAR =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PedidoProveedorListItemDto>>> Get(
            [FromQuery] int? proveedorId,
            [FromQuery] EstadoPedidoProveedor? estado,
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var q = _db.PedidosProveedores
                .Include(p => p.Proveedor)
                .AsNoTracking()
                .AsQueryable();

            if (proveedorId is not null) q = q.Where(p => p.ProveedorId == proveedorId);
            if (estado is not null) q = q.Where(p => p.Estado == estado);

            if (desde is not null)
            {
                var d = DateTime.SpecifyKind(desde.Value, DateTimeKind.Utc);
                q = q.Where(p => p.Fecha >= d);
            }

            if (hasta is not null)
            {
                var h = DateTime.SpecifyKind(hasta.Value, DateTimeKind.Utc);
                q = q.Where(p => p.Fecha < h.AddDays(1));
            }

            var items = await q
                .OrderByDescending(p => p.Fecha)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PedidoProveedorListItemDto
                {
                    Id = p.Id,
                    Fecha = DateTime.SpecifyKind(p.Fecha, DateTimeKind.Utc),
                    Numero = p.Numero,
                    ProveedorId = p.ProveedorId,
                    ProveedorNombre = p.Proveedor.Nombre,
                    Estado = p.Estado,
                    Subtotal = p.Subtotal,
                    Descuento = p.Descuento,
                    Total = p.Total
                })
                .ToListAsync();

            return Ok(items);
        }

        // ================= OBTENER =================
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PedidoProveedorDto>> GetById(int id)
        {
            var p = await _db.PedidosProveedores
                .Include(p => p.Proveedor)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(pr => pr.Unidad)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(pr => pr.Producto)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (p is null) return NotFound();

            string? formaPagoNombre = null;
            if (p.Detalles.Count > 0)
            {
                var detIds = p.Detalles.Select(d => d.Id).ToList();
                formaPagoNombre = await _db.Compras
                    .AsNoTracking()
                    .Include(c => c.FormaPago)
                    .Where(c => c.Detalles.Any(cd =>
                        cd.PedidoProveedorDetalleId != null &&
                        detIds.Contains(cd.PedidoProveedorDetalleId.Value)))
                    .OrderByDescending(c => c.Id)
                    .Select(c => c.FormaPago.Nombre)
                    .FirstOrDefaultAsync();
            }

            var dto = new PedidoProveedorDto
            {
                Id = p.Id,
                Fecha = DateTime.SpecifyKind(p.Fecha, DateTimeKind.Utc),
                Numero = p.Numero,
                ProveedorId = p.ProveedorId,
                ProveedorNombre = p.Proveedor.Nombre,
                Estado = p.Estado,
                Subtotal = p.Subtotal,
                Descuento = p.Descuento,
                Total = p.Total,
                Observaciones = p.Observaciones,
                FormaPago = formaPagoNombre,
                Detalles = p.Detalles.Select(d => new PedidoProveedorDetalleDto
                {
                    Id = d.Id,
                    PresentacionId = d.PresentacionId,
                    ProductoNombre = d.Presentacion.Producto.Nombre,
                    PresentacionNombre = d.Presentacion.Nombre,
                    Unidad = d.Presentacion.Unidad.Simbolo,
                    SKU = d.Presentacion.SKU,
                    Cantidad = d.Cantidad,
                    CantidadRecibida = d.CantidadRecibida,
                    PrecioUnitario = d.PrecioUnitario,
                    Descuento = d.Descuento,
                    TotalLinea = d.TotalLinea,
                    Notas = d.Notas
                }).ToList()
            };
            return Ok(dto);
        }

        // ================= CREAR =================
        [HttpPost]
        public async Task<ActionResult<object>> Create(PedidoProveedorCreateDto body)
        {
            var proveedor = await _db.Proveedores.FindAsync(body.ProveedorId);
            if (proveedor is null) return BadRequest("Proveedor no existe.");

            var presentacionIds = body.Detalles.Select(d => d.PresentacionId).Distinct().ToList();
            var countPres = await _db.Presentaciones.CountAsync(x => presentacionIds.Contains(x.Id));
            if (countPres != presentacionIds.Count) return BadRequest("Una o más presentaciones no existen.");

            var catalogo = await _db.ProveedoresPresentaciones
                .Include(pp => pp.Presentacion).ThenInclude(pr => pr.Producto) // ✅ para fallback al producto
                .Where(pp => pp.ProveedorId == body.ProveedorId && presentacionIds.Contains(pp.PresentacionId))
                .ToListAsync();
            if (catalogo.Count != presentacionIds.Count)
                return BadRequest("Una o más presentaciones no están en el catálogo del proveedor.");

            var numero = string.IsNullOrWhiteSpace(body.Numero) ? await GenerarNumeroAsync() : body.Numero;

            var pedido = new PedidoProveedor
            {
                Fecha = DateTime.UtcNow,
                Numero = numero,
                ProveedorId = body.ProveedorId,
                Observaciones = body.Observaciones,
                Estado = EstadoPedidoProveedor.Borrador,
                Detalles = new List<PedidoProveedorDetalle>()
            };

            foreach (var d in body.Detalles)
            {
                var cat = catalogo.First(pp => pp.PresentacionId == d.PresentacionId);
                if (!cat.Activo) return BadRequest("Hay presentaciones inactivas en el catálogo del proveedor.");

                // ✅ Precio compra: Ultimo > Lista > PrecioCompraDefault (Presentación) > PrecioCompraDefault (Producto) > 0
                var precio = cat.PrecioUltimo
                           ?? cat.PrecioLista
                           ?? cat.Presentacion.PrecioCompraDefault
                           ?? cat.Presentacion.Producto.PrecioCompraDefault
                           ?? 0m;

                pedido.Detalles.Add(new PedidoProveedorDetalle
                {
                    PresentacionId = d.PresentacionId,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = precio,
                    Descuento = d.Descuento,
                    TotalLinea = Math.Round(d.Cantidad * precio - d.Descuento, 2),
                    Notas = d.Notas
                });
            }

            RecalcularTotales(pedido);

            const int maxIntentos = 3;
            for (int intento = 1; intento <= maxIntentos; intento++)
            {
                try
                {
                    _db.PedidosProveedores.Add(pedido);
                    await _db.SaveChangesAsync();
                    return CreatedAtAction(nameof(GetById), new { id = pedido.Id }, new { pedido.Id });
                }
                catch (DbUpdateException ex) when (EsColisionUnique(ex))
                {
                    _db.Entry(pedido).State = EntityState.Detached;
                    pedido.Id = 0;
                    pedido.Numero = await GenerarNumeroAsync();
                    if (intento == maxIntentos)
                    {
                        return Problem(title: "No fue posible emitir el pedido",
                            detail: "Se detectaron colisiones repetidas en la numeración.",
                            statusCode: 409);
                    }
                }
            }
            return Problem(title: "Error inesperado al crear el pedido.", statusCode: 500);
        }

        // ================= EDITAR ENCABEZADO/LÍNEAS =================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, PedidoProveedorUpdateDto body)
        {
            var p = await _db.PedidosProveedores.FindAsync(id);
            if (p is null) return NotFound();
            if (p.Estado != EstadoPedidoProveedor.Borrador)
                return BadRequest("Solo se puede editar en estado Borrador.");

            p.Numero = body.Numero ?? p.Numero;
            p.Observaciones = body.Observaciones ?? p.Observaciones;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/detalle")]
        public async Task<ActionResult> AddLinea(int id, PedidoProveedorDetalleCreateDto d)
        {
            var p = await _db.PedidosProveedores.Include(x => x.Detalles).FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound();
            if (p.Estado != EstadoPedidoProveedor.Borrador)
                return BadRequest("Solo se puede modificar en estado Borrador.");

            var cat = await _db.ProveedoresPresentaciones
                .Include(x => x.Presentacion).ThenInclude(pr => pr.Producto) // ✅
                .FirstOrDefaultAsync(x => x.ProveedorId == p.ProveedorId && x.PresentacionId == d.PresentacionId);
            if (cat is null || !cat.Activo)
                return BadRequest("La presentación no está en el catálogo del proveedor.");

            var precio = cat.PrecioUltimo
                       ?? cat.PrecioLista
                       ?? cat.Presentacion.PrecioCompraDefault
                       ?? cat.Presentacion.Producto.PrecioCompraDefault
                       ?? 0m;

            var det = new PedidoProveedorDetalle
            {
                PedidoProveedorId = id,
                PresentacionId = d.PresentacionId,
                Cantidad = d.Cantidad,
                PrecioUnitario = precio,
                Descuento = d.Descuento,
                TotalLinea = Math.Round(d.Cantidad * precio - d.Descuento, 2),
                Notas = d.Notas
            };
            p.Detalles.Add(det);
            RecalcularTotales(p);
            await _db.SaveChangesAsync();
            return Ok(new { det.Id });
        }

        [HttpPut("{id:int}/detalle/{detalleId:int}")]
        public async Task<IActionResult> UpdateLinea(int id, int detalleId, PedidoProveedorDetalleCreateDto d)
        {
            var p = await _db.PedidosProveedores.Include(x => x.Detalles).FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound();
            if (p.Estado != EstadoPedidoProveedor.Borrador)
                return BadRequest("Solo se puede modificar en estado Borrador.");

            var det = p.Detalles.FirstOrDefault(x => x.Id == detalleId);
            if (det is null) return NotFound();

            var cat = await _db.ProveedoresPresentaciones
                .Include(x => x.Presentacion).ThenInclude(pr => pr.Producto) // ✅
                .FirstOrDefaultAsync(x => x.ProveedorId == p.ProveedorId && x.PresentacionId == d.PresentacionId);
            if (cat is null || !cat.Activo)
                return BadRequest("La presentación no está en el catálogo del proveedor.");

            var precio = cat.PrecioUltimo
                       ?? cat.PrecioLista
                       ?? cat.Presentacion.PrecioCompraDefault
                       ?? cat.Presentacion.Producto.PrecioCompraDefault
                       ?? 0m;

            det.PresentacionId = d.PresentacionId;
            det.Cantidad = d.Cantidad;
            det.PrecioUnitario = precio;
            det.Descuento = d.Descuento;
            det.TotalLinea = Math.Round(d.Cantidad * precio - d.Descuento, 2);
            det.Notas = d.Notas;

            RecalcularTotales(p);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}/detalle/{detalleId:int}")]
        public async Task<IActionResult> DeleteLinea(int id, int detalleId)
        {
            var p = await _db.PedidosProveedores.Include(x => x.Detalles).FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound();
            if (p.Estado != EstadoPedidoProveedor.Borrador)
                return BadRequest("Solo se puede modificar en estado Borrador.");

            var det = p.Detalles.FirstOrDefault(x => x.Id == detalleId);
            if (det is null) return NotFound();

            _db.PedidosProveedoresDetalle.Remove(det);
            RecalcularTotales(p);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ================= WORKFLOW =================
        [HttpPost("{id:int}/enviar")]
        public async Task<IActionResult> Enviar(int id)
        {
            var p = await _db.PedidosProveedores.FindAsync(id);
            if (p is null) return NotFound();
            if (p.Estado != EstadoPedidoProveedor.Borrador)
                return BadRequest("Solo se puede enviar desde Borrador.");
            p.Estado = EstadoPedidoProveedor.Enviado;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/aprobar")]
        public async Task<IActionResult> Aprobar(int id)
        {
            var p = await _db.PedidosProveedores.FindAsync(id);
            if (p is null) return NotFound();
            if (p.Estado != EstadoPedidoProveedor.Enviado)
                return BadRequest("Solo se puede aprobar desde Enviado.");
            p.Estado = EstadoPedidoProveedor.Aprobado;
            p.AprobadoEl = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/cancelar")]
        public async Task<IActionResult> Cancelar(int id)
        {
            var p = await _db.PedidosProveedores
                .Include(x => x.Detalles)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p is null) return NotFound();

            if (p.Estado is EstadoPedidoProveedor.Cerrado or EstadoPedidoProveedor.Cancelado)
                return BadRequest("El pedido ya está cerrado/cancelado.");

            var tuvoRecepciones = p.Detalles.Any(d => d.CantidadRecibida > 0);
            if (tuvoRecepciones)
            {
                const string marca = " • Cancelado con recepción parcial";
                if (string.IsNullOrWhiteSpace(p.Observaciones))
                {
                    p.Observaciones = marca.Trim();
                }
                else if (!p.Observaciones.Contains("Cancelado con recepción parcial", StringComparison.OrdinalIgnoreCase))
                {
                    p.Observaciones += marca;
                }
            }

            p.Estado = EstadoPedidoProveedor.Cancelado;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ================= RECEPCIÓN =================
        [HttpPost("{id:int}/recepciones")]
        public async Task<ActionResult> Recibir(int id, PedidoRecepcionCreateDto body)
        {
            var estado = await _caja.EstadoAsync();
            if (!estado.Abierta)
                return BadRequest(new { message = "No puedes registrar recepciones: la caja está cerrada." });

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var p = await _db.PedidosProveedores
                    .Include(x => x.Detalles)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (p is null) return NotFound();
                if (p.Estado is EstadoPedidoProveedor.Cancelado or EstadoPedidoProveedor.Cerrado)
                    return BadRequest("No se puede recibir: pedido cancelado o cerrado.");

                var compra = new Compra
                {
                    Fecha = DateTime.SpecifyKind(body.Fecha, DateTimeKind.Utc),
                    ProveedorId = p.ProveedorId,
                    Numero = body.Numero,
                    Observaciones = $"Recepción de Pedido #{p.Id}",
                    Subtotal = 0m,
                    Descuento = 0m,
                    Total = 0m,
                    FormaPagoId = body.FormaPagoId,
                    Estado = "Registrada"
                };

                // Construir detalles de compra y actualizar recibos/total
                foreach (var l in body.Lineas)
                {
                    var det = p.Detalles.FirstOrDefault(d => d.Id == l.PedidoProveedorDetalleId);
                    if (det is null) return BadRequest($"Detalle {l.PedidoProveedorDetalleId} no pertenece al pedido.");
                    if (l.Cantidad <= 0) return BadRequest("Cantidad debe ser > 0.");

                    var pendiente = det.Cantidad - det.CantidadRecibida;
                    if (l.Cantidad > pendiente)
                        return BadRequest($"Cantidad recibida supera lo pendiente (pendiente: {pendiente}).");

                    var totalLinea = Math.Round(l.Cantidad * l.CostoUnitario, 2);
                    compra.Detalles.Add(new CompraDetalle
                    {
                        PresentacionId = det.PresentacionId,
                        Cantidad = l.Cantidad,
                        CostoUnitario = l.CostoUnitario,
                        TotalLinea = totalLinea,
                        Notas = l.Notas,
                        PedidoProveedorDetalleId = det.Id
                    });

                    compra.Subtotal += totalLinea;
                    det.CantidadRecibida += l.Cantidad;
                }

                compra.Total = compra.Subtotal;

                p.Estado = p.Detalles.All(d => d.CantidadRecibida >= d.Cantidad)
                    ? EstadoPedidoProveedor.Cerrado
                    : EstadoPedidoProveedor.ParcialmenteRecibido;

                // 1) Guardar compra + detalles para obtener compra.Id
                _db.Compras.Add(compra);
                await _db.SaveChangesAsync();

                // 2) Stock y Kardex (ya con DocumentoId correcto)
                foreach (var l in body.Lineas)
                {
                    var det = p.Detalles.First(d => d.Id == l.PedidoProveedorDetalleId);

                    var stock = await _db.ProductoStocks.FirstOrDefaultAsync(s => s.PresentacionId == det.PresentacionId);
                    if (stock == null)
                    {
                        stock = new ProductoStock { PresentacionId = det.PresentacionId, Cantidad = 0m };
                        _db.ProductoStocks.Add(stock);
                    }
                    stock.Cantidad += l.Cantidad;

                    _db.MovimientosInventario.Add(new MovimientoInventario
                    {
                        FechaUtc = DateTime.UtcNow,
                        PresentacionId = det.PresentacionId,
                        Tipo = TipoMovimiento.Entrada,
                        Cantidad = l.Cantidad,
                        CostoUnitario = l.CostoUnitario,
                        Documento = "Compra",
                        DocumentoId = compra.Id,
                        Notas = $"Recepción pedido #{p.Id}"
                    });

                    var cat = await _db.ProveedoresPresentaciones
                        .FirstOrDefaultAsync(x => x.ProveedorId == p.ProveedorId && x.PresentacionId == det.PresentacionId);
                    if (cat is not null) cat.PrecioUltimo = l.CostoUnitario;
                }

                await _db.SaveChangesAsync();

                // 3) Afectar caja si corresponde
                var fp = await _db.FormasPago
                    .Where(f => f.Id == compra.FormaPagoId)
                    .Select(f => new { f.Nombre, f.AfectaCaja, f.RequiereReferencia })
                    .FirstOrDefaultAsync();

                if (fp is null) return BadRequest(new { message = "Forma de pago inválida." });
                if (fp.RequiereReferencia && string.IsNullOrWhiteSpace(body.Referencia))
                    return BadRequest(new { message = "Debe indicar la referencia del depósito/transferencia." });

                if (fp.AfectaCaja)
                {
                    await _caja.AddMovimientoEnCajaAbiertaAsync(new CajaMovimientoCreateDto
                    {
                        Tipo = (int)TipoMovimientoCaja.PagoProveedor,
                        Monto = compra.Total,
                        Concepto = $"Pago compra (recepción pedido #{p.Id}) - {fp.Nombre}",
                        Documento = "Compra",
                        DocumentoId = compra.Id,
                        Observaciones = string.IsNullOrWhiteSpace(body.Referencia) ? null : $"Ref: {body.Referencia!.Trim()}"
                    }, null);
                }

                await tx.CommitAsync();
                return Ok(new { compra.Id });
            }
            catch (CajaFondosInsuficientesException ex)
            {
                await tx.RollbackAsync();
                return Conflict(new
                {
                    error = "Fondos insuficientes en caja",
                    disponible = ex.Disponible,
                    solicitado = ex.Solicitado
                });
            }
        }

        // ================= Reporte simple de compras =================
        [HttpGet("reportes/compras")]
        public async Task<ActionResult<IEnumerable<object>>> ReporteCompras(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] int? proveedorId)
        {
            var q = _db.Compras
                .Include(c => c.Proveedor)
                .AsNoTracking()
                .AsQueryable();

            if (desde is not null)
            {
                var d = DateTime.SpecifyKind(desde.Value, DateTimeKind.Utc);
                q = q.Where(c => c.Fecha >= d);
            }
            if (hasta is not null)
            {
                var h = DateTime.SpecifyKind(hasta.Value, DateTimeKind.Utc);
                q = q.Where(c => c.Fecha <= h);
            }
            if (proveedorId is not null) q = q.Where(c => c.ProveedorId == proveedorId);

            var list = await q
                .OrderByDescending(c => c.Fecha)
                .Select(c => new
                {
                    c.Id,
                    Fecha = DateTime.SpecifyKind(c.Fecha, DateTimeKind.Utc),
                    Proveedor = c.Proveedor.Nombre,
                    c.Numero,
                    c.Subtotal,
                    c.Total,
                    c.FormaPagoId
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}

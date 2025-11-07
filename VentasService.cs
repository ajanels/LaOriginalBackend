using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos.Ventas;
using LaOriginalBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Services
{
    public interface IVentasService
    {
        Task<Venta> CrearVentaAsync(VentaCreateDto dto, int? usuarioId, CancellationToken ct = default);
        Task<bool> AnularVentaAsync(int id, int? usuarioId, CancellationToken ct = default);
    }

    public class VentasService : IVentasService
    {
        private readonly AppDbContext _db;

        public VentasService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Venta> CrearVentaAsync(VentaCreateDto dto, int? usuarioId, CancellationToken ct = default)
        {
            // Validaciones básicas
            if (dto.Items is null || dto.Items.Count == 0)
                throw new ArgumentException("Debe incluir al menos un ítem");

            // Validar Cliente y FormaPago si vienen
            Cliente? cliente = null;
            if (dto.ClienteId.HasValue)
            {
                cliente = await _db.Clientes.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.ClienteId.Value, ct)
                    ?? throw new InvalidOperationException("Cliente no existe");
            }

            if (dto.FormaPagoId.HasValue)
            {
                var fp = await _db.FormasPago.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == dto.FormaPagoId.Value && f.Activo, ct);
                if (fp is null) throw new InvalidOperationException("Forma de pago inválida o inactiva");
            }

            // Traer presentaciones involucradas para validar y usar nombres
            var presentacionIds = dto.Items.Select(i => i.PresentacionId).Distinct().ToList();
            var presentaciones = await _db.Presentaciones
                .Include(p => p.Producto)
                .AsNoTracking()
                .Where(p => presentacionIds.Contains(p.Id) && p.Activo)
                .ToListAsync(ct);

            if (presentaciones.Count != presentacionIds.Count)
                throw new InvalidOperationException("Hay presentaciones inexistentes o inactivas en el carrito");

            // Cálculo de totales
            decimal subtotal = 0m, descuento = 0m, total = 0m;

            var venta = new Venta
            {
                Fecha = DateTime.UtcNow,
                ClienteId = dto.ClienteId,
                Serie = string.IsNullOrWhiteSpace(dto.Serie) ? null : dto.Serie!.Trim(),
                Numero = string.IsNullOrWhiteSpace(dto.Numero) ? null : dto.Numero!.Trim(),
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones!.Trim(),
                FormaPagoId = dto.FormaPagoId,
                Estado = "Completada",      // Parametrizable: Borrador/Completada/Pagada
                Anulada = false,
                UsuarioId = usuarioId
            };

            var detalles = new List<VentaDetalle>();

            foreach (var item in dto.Items)
            {
                var pres = presentaciones.First(p => p.Id == item.PresentacionId);

                if (item.Cantidad <= 0) throw new InvalidOperationException("Cantidad inválida");
                if (item.PrecioUnitario < 0) throw new InvalidOperationException("Precio inválido");
                if (item.DescuentoUnitario < 0) throw new InvalidOperationException("Descuento inválido");

                var linea = (item.Cantidad * item.PrecioUnitario) - item.DescuentoUnitario;
                if (linea < 0) throw new InvalidOperationException("Total de línea no puede ser negativo");

                subtotal += item.Cantidad * item.PrecioUnitario;
                descuento += item.DescuentoUnitario;
                total += linea;

                detalles.Add(new VentaDetalle
                {
                    PresentacionId = item.PresentacionId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    DescuentoUnitario = item.DescuentoUnitario,
                    TotalLinea = linea,
                    Notas = string.IsNullOrWhiteSpace(item.Notas) ? null : item.Notas!.Trim()
                });
            }

            venta.Subtotal = subtotal;
            venta.Descuento = descuento;
            venta.Total = total;

            // Transacción
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            _db.Ventas.Add(venta);
            await _db.SaveChangesAsync(ct);

            foreach (var d in detalles)
            {
                d.VentaId = venta.Id;
            }
            _db.VentasDetalle.AddRange(detalles);

            await _db.SaveChangesAsync(ct);

            // TODO (opcional, siguiente paso): ajustar inventario (descontar stock)
            // - Buscar stock por PresentacionId
            // - Validar existencias suficientes (si se desea)
            // - Registrar MovimientoInventario y actualizar ProductoStock

            await tx.CommitAsync(ct);
            return venta;
        }

        public async Task<bool> AnularVentaAsync(int id, int? usuarioId, CancellationToken ct = default)
        {
            var venta = await _db.Ventas.FirstOrDefaultAsync(v => v.Id == id, ct);
            if (venta is null) return false;
            if (venta.Anulada) return true; // ya estaba anulada

            venta.Anulada = true;
            venta.Estado = "Anulada";

            // TODO (opcional, siguiente paso): reponer inventario
            // - Leer detalles
            // - Sumar existencias por PresentacionId
            // - Registrar movimiento de anulación

            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}

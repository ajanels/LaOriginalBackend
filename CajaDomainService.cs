// Services/Caja/CajaDomainService.cs
using System.Data;
using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LaOriginalBackend.Services
{
    public class CajaDomainService : ICajaDomainService
    {
        private readonly AppDbContext _db;
        public CajaDomainService(AppDbContext db) { _db = db; }

        static decimal R(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        public async Task<CajaEstadoDto> EstadoAsync(CancellationToken ct = default)
        {
            var ap = await _db.CajaAperturas
                .AsNoTracking()
                .Where(a => a.FechaCierreUtc == null)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync(ct);

            if (ap == null)
            {
                return new CajaEstadoDto
                {
                    Abierta = false,
                    AperturaId = null,
                    SesionId = null,
                    Codigo = null,
                    Apertura = null,
                    CajeroNombre = null,
                    CapitalLiquido = 0,
                    EfectivoInicial = 0,
                    MontoInicial = null,
                    FechaAperturaUtc = null
                };
            }

            var resumen = await ResumenAsync(ap.Id, ct);

            return new CajaEstadoDto
            {
                Abierta = true,
                AperturaId = ap.Id,
                SesionId = ap.Id,
                Codigo = ap.Codigo ?? $"A-{ap.Id:D4}",
                Apertura = ap.FechaAperturaUtc,
                CajeroNombre = ap.CajeroNombre,
                CapitalLiquido = resumen.Esperado,
                EfectivoInicial = ap.MontoInicial,
                MontoInicial = ap.MontoInicial,
                FechaAperturaUtc = ap.FechaAperturaUtc
            };
        }

        public async Task<CajaResumenDto> ResumenAsync(int? aperturaId = null, CancellationToken ct = default)
        {
            var apId = aperturaId ??
                       await _db.CajaAperturas
                           .Where(a => a.FechaCierreUtc == null)
                           .OrderByDescending(a => a.Id)
                           .Select(a => (int?)a.Id)
                           .FirstOrDefaultAsync(ct);

            if (apId == null)
                return new CajaResumenDto();

            var ap = await _db.CajaAperturas.AsNoTracking().FirstAsync(a => a.Id == apId.Value, ct);

            var movs = await _db.CajaMovimientos
                .AsNoTracking()
                .Where(m => m.CajaAperturaId == apId.Value)
                .ToListAsync(ct);

            decimal ingresos = 0, egresos = 0;

            foreach (var m in movs)
            {
                switch (m.Tipo)
                {
                    case TipoMovimientoCaja.Apertura:
                        break; // El inicial está en CajaApertura.MontoInicial

                    case TipoMovimientoCaja.Cierre:
                    case TipoMovimientoCaja.Egreso:
                    case TipoMovimientoCaja.PagoProveedor:
                        egresos += m.Monto;
                        break;

                    case TipoMovimientoCaja.Ajuste:
                        if (m.Monto >= 0) ingresos += m.Monto;
                        else egresos += (-m.Monto);
                        break;

                    default:
                        ingresos += m.Monto; // Ingreso, CobroVenta, etc.
                        break;
                }
            }

            var esperado = R(ap.MontoInicial + ingresos - egresos);

            return new CajaResumenDto
            {
                AperturaId = ap.Id,
                FechaAperturaUtc = ap.FechaAperturaUtc,
                FechaCierreUtc = ap.FechaCierreUtc,
                MontoInicial = ap.MontoInicial,
                Ingresos = ingresos,
                Egresos = egresos,
                Esperado = esperado,
                Conteo = ap.MontoCierreDeclarado,
                Diferencia = ap.MontoCierreDeclarado.HasValue ? R(ap.MontoCierreDeclarado.Value - esperado) : (decimal?)null
            };
        }

        public async Task<CajaMovimiento> AddMovimientoEnCajaAbiertaAsync(
            CajaMovimientoCreateDto dto,
            int? usuarioId = null,
            CancellationToken ct = default)
        {
            // Usa transacción propia SOLO si no existe una activa (para no anidar).
            var ownsTx = _db.Database.CurrentTransaction == null;
            IDbContextTransaction? tx = null;
            if (ownsTx)
                tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            try
            {
                var ap = await _db.CajaAperturas
                    .Where(a => a.FechaCierreUtc == null)
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefaultAsync(ct);

                if (ap == null)
                    throw new InvalidOperationException("No hay caja abierta.");

                var tipo = (TipoMovimientoCaja)dto.Tipo;
                var monto = R(dto.Monto);

                // Disponible actual (en la misma transacción)
                var ingresos = await _db.CajaMovimientos
                    .Where(m => m.CajaAperturaId == ap.Id &&
                                (m.Tipo == TipoMovimientoCaja.Ingreso ||
                                 m.Tipo == TipoMovimientoCaja.CobroVenta ||
                                 (m.Tipo == TipoMovimientoCaja.Ajuste && m.Monto >= 0)))
                    .SumAsync(m => (decimal?)m.Monto, ct) ?? 0m;

                var egresos = await _db.CajaMovimientos
                    .Where(m => m.CajaAperturaId == ap.Id &&
                                (m.Tipo == TipoMovimientoCaja.Egreso ||
                                 m.Tipo == TipoMovimientoCaja.PagoProveedor ||
                                 m.Tipo == TipoMovimientoCaja.Cierre ||
                                 (m.Tipo == TipoMovimientoCaja.Ajuste && m.Monto < 0)))
                    .SumAsync(m => (decimal?)(
                        m.Tipo == TipoMovimientoCaja.Ajuste && m.Monto < 0 ? -m.Monto : m.Monto
                    ), ct) ?? 0m;

                var disponible = R(ap.MontoInicial + ingresos - egresos);

                bool esEgresoLike =
                    tipo == TipoMovimientoCaja.Egreso ||
                    tipo == TipoMovimientoCaja.PagoProveedor ||
                    tipo == TipoMovimientoCaja.Cierre;

                if (esEgresoLike && monto > disponible)
                    throw new CajaFondosInsuficientesException(disponible, monto);

                var mov = new CajaMovimiento
                {
                    CajaAperturaId = ap.Id,
                    FechaUtc = DateTime.UtcNow,
                    Tipo = tipo,
                    Monto = monto,
                    Concepto = string.IsNullOrWhiteSpace(dto.Concepto) ? TipoToConcepto(tipo) : dto.Concepto!.Trim(),
                    Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones!.Trim(),
                    Documento = string.IsNullOrWhiteSpace(dto.Documento) ? null : dto.Documento!.Trim(),
                    DocumentoId = dto.DocumentoId,
                    UsuarioId = usuarioId
                };

                _db.CajaMovimientos.Add(mov);
                await _db.SaveChangesAsync(ct);

                if (ownsTx) await tx!.CommitAsync(ct);
                return mov;
            }
            catch
            {
                if (ownsTx && tx is not null) await tx.RollbackAsync(ct);
                throw;
            }
        }

        private static string TipoToConcepto(TipoMovimientoCaja tipo) => tipo switch
        {
            TipoMovimientoCaja.Apertura => "Apertura de caja",
            TipoMovimientoCaja.Cierre => "Cierre de caja",
            TipoMovimientoCaja.Ingreso => "Ingreso",
            TipoMovimientoCaja.Egreso => "Egreso",
            TipoMovimientoCaja.CobroVenta => "Cobro de venta",
            TipoMovimientoCaja.PagoProveedor => "Pago a proveedor",
            TipoMovimientoCaja.Ajuste => "Ajuste de caja",
            _ => "Movimiento de caja"
        };
    }
}

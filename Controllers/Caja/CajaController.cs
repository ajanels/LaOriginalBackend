// Controllers/Caja/CajaController.cs
using LaOriginalBackend.Data;
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;
using LaOriginalBackend.Services;    // ⚠️ Importante para la excepción
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaOriginalBackend.Controllers.Caja
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CajaController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICajaDomainService _caja;

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

        private static DateTime ToLocal(DateTime utc)
        {
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, AppTz);
        }

        private static string ToLocalIso(DateTime utc)
        {
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            var offset = AppTz.GetUtcOffset(utc);
            var dto = new DateTimeOffset(utc, TimeSpan.Zero).ToOffset(offset);
            return dto.ToString("o");
        }

        public CajaController(AppDbContext db, ICajaDomainService caja)
        {
            _db = db;
            _caja = caja;
        }

        /* ================= Helpers usuario ================= */
        private int? GetUserId()
        {
            var idClaim = User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : (int?)null;
        }

        private string? GetUserName()
        {
            var name = User.FindFirst("name")?.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                var given = User.FindFirst("given_name")?.Value;
                var family = User.FindFirst("family_name")?.Value;
                name = $"{given} {family}".Trim();
            }
            if (string.IsNullOrWhiteSpace(name))
                name = User.Identity?.Name;

            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        private static decimal RoundMoney(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        /* ================= Estado / Resumen ================= */
        [HttpGet("estado")]
        public async Task<ActionResult<CajaEstadoDto>> Estado(CancellationToken ct)
        {
            var dto = await _caja.EstadoAsync(ct);

            if (dto != null)
            {
                if (dto.FechaAperturaUtc.HasValue)
                    dto.FechaAperturaUtc = DateTime.SpecifyKind(dto.FechaAperturaUtc.Value, DateTimeKind.Utc);

                if (!dto.Apertura.HasValue && dto.FechaAperturaUtc.HasValue)
                    dto.Apertura = dto.FechaAperturaUtc;

                if (dto.Apertura.HasValue)
                    dto.Apertura = DateTime.SpecifyKind(dto.Apertura.Value, DateTimeKind.Utc);

                dto.TimeZoneId = AppTz.Id;
                if (dto.Apertura.HasValue)
                {
                    dto.AperturaLocal = ToLocal(dto.Apertura.Value);
                    dto.AperturaLocalIso = ToLocalIso(dto.Apertura.Value);
                }
            }

            return Ok(dto);
        }

        [HttpGet("resumen")]
        public async Task<ActionResult<CajaResumenDto>> Resumen([FromQuery] int? aperturaId, CancellationToken ct)
        {
            var r = await _caja.ResumenAsync(aperturaId, ct);
            if (r != null)
            {
                r.FechaAperturaUtc = DateTime.SpecifyKind(r.FechaAperturaUtc, DateTimeKind.Utc);
                if (r.FechaCierreUtc.HasValue)
                    r.FechaCierreUtc = DateTime.SpecifyKind(r.FechaCierreUtc.Value, DateTimeKind.Utc);

                r.TimeZoneId = AppTz.Id;
                r.FechaAperturaLocal = ToLocal(r.FechaAperturaUtc);
                r.FechaAperturaLocalIso = ToLocalIso(r.FechaAperturaUtc);
                r.FechaCierreLocal = r.FechaCierreUtc.HasValue ? ToLocal(r.FechaCierreUtc.Value) : (DateTime?)null;
                r.FechaCierreLocalIso = r.FechaCierreUtc.HasValue ? ToLocalIso(r.FechaCierreUtc.Value) : null;
            }
            return Ok(r);
        }

        /* ================= Auditoría de sesiones ================= */
        [HttpGet("sesiones")]
        public async Task<ActionResult<IEnumerable<CajaSesionListDto>>> Sesiones([FromQuery] string? q = null, CancellationToken ct = default)
        {
            var query = _db.CajaAperturas.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(a =>
                    EF.Functions.Like((a.Codigo ?? "").ToLower(), $"%{term}%") ||
                    EF.Functions.Like((a.CajeroNombre ?? "").ToLower(), $"%{term}%"));
            }

            var rows = await query
                .OrderByDescending(a => a.Id)
                .Take(200)
                .Select(a => new CajaSesionListDto
                {
                    Id = a.Id,
                    Codigo = a.Codigo ?? $"A-{a.Id:D4}",
                    Apertura = DateTime.SpecifyKind(a.FechaAperturaUtc, DateTimeKind.Utc),
                    CajeroNombre = a.CajeroNombre,
                    Estado = a.FechaCierreUtc == null ? "Abierta" : "Cerrada"
                })
                .ToListAsync(ct);

            foreach (var s in rows)
            {
                s.TimeZoneId = AppTz.Id;
                s.AperturaLocal = ToLocal(s.Apertura);
                s.AperturaLocalIso = ToLocalIso(s.Apertura);
            }

            return Ok(rows);
        }

        /* ================= Abrir / Cerrar ================= */
        [HttpPost("abrir")]
        public async Task<ActionResult> Abrir([FromBody] CajaAbrirDto dto, CancellationToken ct)
        {
            if (dto == null) return BadRequest("Solicitud inválida.");
            if (dto.MontoInicial < 0) return BadRequest("El monto inicial no puede ser negativo.");

            var yaAbierta = await _db.CajaAperturas.AnyAsync(a => a.FechaCierreUtc == null, ct);
            if (yaAbierta) return BadRequest("Ya existe una caja abierta.");

            var now = DateTime.UtcNow;

            var apertura = new CajaApertura
            {
                FechaAperturaUtc = now,
                MontoInicial = RoundMoney(dto.MontoInicial),
                ObservacionesApertura = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones!.Trim(),
                CajeroNombre = string.IsNullOrWhiteSpace(dto.CajeroNombre) ? GetUserName() : dto.CajeroNombre!.Trim()
            };

            _db.CajaAperturas.Add(apertura);
            await _db.SaveChangesAsync(ct); // obtener Id

            apertura.Codigo = apertura.Codigo ?? $"A-{apertura.Id:D4}";
            await _db.SaveChangesAsync(ct);

            var userId = GetUserId();

            _db.CajaMovimientos.Add(new CajaMovimiento
            {
                CajaAperturaId = apertura.Id,
                FechaUtc = now,
                Tipo = TipoMovimientoCaja.Apertura,
                Monto = apertura.MontoInicial,
                Concepto = "Apertura de caja",
                Observaciones = apertura.ObservacionesApertura,
                UsuarioId = userId
            });

            await _db.SaveChangesAsync(ct);

            return Ok(new { apertura.Id, apertura.Codigo });
        }

        [HttpPost("cerrar")]
        public async Task<ActionResult> Cerrar([FromBody] CajaCerrarDto dto, CancellationToken ct)
        {
            if (dto == null) return BadRequest("Solicitud inválida.");
            if (dto.MontoConteo < 0) return BadRequest("El conteo no puede ser negativo.");

            var apertura = await _db.CajaAperturas.FirstOrDefaultAsync(a => a.FechaCierreUtc == null, ct);
            if (apertura == null) return BadRequest("No hay caja abierta.");

            var resumen = await _caja.ResumenAsync(apertura.Id, ct);

            var now = DateTime.UtcNow;
            apertura.FechaCierreUtc = now;
            apertura.MontoCierreDeclarado = RoundMoney(dto.MontoConteo);
            apertura.ObservacionesCierre = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones!.Trim();

            var userId = GetUserId();

            _db.CajaMovimientos.Add(new CajaMovimiento
            {
                CajaAperturaId = apertura.Id,
                FechaUtc = now,
                Tipo = TipoMovimientoCaja.Cierre,
                Monto = apertura.MontoCierreDeclarado ?? 0m,
                Concepto = "Cierre de caja",
                Observaciones = apertura.ObservacionesCierre,
                UsuarioId = userId
            });

            await _db.SaveChangesAsync(ct);

            var conteo = apertura.MontoCierreDeclarado ?? 0m;
            var esperado = RoundMoney(resumen.Esperado);
            var diferencia = RoundMoney(conteo - esperado);

            return Ok(new
            {
                resumen.AperturaId,
                resumen.MontoInicial,
                resumen.Ingresos,
                resumen.Egresos,
                Esperado = esperado,
                Conteo = conteo,
                Diferencia = diferencia
            });
        }

        /* ================= Movimientos ================= */
        [HttpGet("movimientos")]
        public async Task<ActionResult<IEnumerable<CajaMovimientoDto>>> Movs(
            [FromQuery] int? aperturaId,
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] int? tipo,
            [FromQuery] string? q,
            CancellationToken ct = default)
        {
            var ap = aperturaId ?? await _db.CajaAperturas
                .Where(a => a.FechaCierreUtc == null)
                .OrderByDescending(a => a.Id)
                .Select(a => (int?)a.Id)
                .FirstOrDefaultAsync(ct);

            if (ap == null) return Ok(Array.Empty<CajaMovimientoDto>());

            var query = _db.CajaMovimientos.AsNoTracking().Where(m => m.CajaAperturaId == ap.Value);

            if (desde.HasValue) query = query.Where(m => m.FechaUtc >= desde.Value.ToUniversalTime());
            if (hasta.HasValue) query = query.Where(m => m.FechaUtc <= hasta.Value.ToUniversalTime());
            if (tipo.HasValue) query = query.Where(m => (int)m.Tipo == tipo.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(m =>
                    EF.Functions.Like(m.Concepto ?? "", $"%{term}%") ||
                    EF.Functions.Like(m.Observaciones ?? "", $"%{term}%") ||
                    EF.Functions.Like(m.Documento ?? "", $"%{term}%"));
            }

            var rows = await query
                .OrderByDescending(m => m.Id)
                .Select(m => new CajaMovimientoDto
                {
                    Id = m.Id,
                    CajaAperturaId = m.CajaAperturaId,
                    FechaUtc = DateTime.SpecifyKind(m.FechaUtc, DateTimeKind.Utc),
                    Tipo = (int)m.Tipo,
                    Monto = m.Monto,
                    Concepto = m.Concepto,
                    Observaciones = m.Observaciones,
                    Documento = m.Documento,
                    DocumentoId = m.DocumentoId,
                    UsuarioId = m.UsuarioId
                })
                .ToListAsync(ct);

            foreach (var m in rows)
            {
                m.TimeZoneId = AppTz.Id;
                m.FechaLocal = ToLocal(m.FechaUtc);
                m.FechaLocalIso = ToLocalIso(m.FechaUtc);
            }

            return Ok(rows);
        }

        [HttpPost("movimientos")]
        public async Task<ActionResult<CajaMovimientoDto>> CrearMov([FromBody] CajaMovimientoCreateDto dto, CancellationToken ct)
        {
            if (dto == null) return BadRequest("Solicitud inválida.");
            if (dto.Monto <= 0) return BadRequest("El monto debe ser mayor a cero.");

            dto.Monto = RoundMoney(dto.Monto);
            dto.Concepto = string.IsNullOrWhiteSpace(dto.Concepto) ? "Movimiento" : dto.Concepto!.Trim();
            dto.Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones!.Trim();
            dto.Documento = string.IsNullOrWhiteSpace(dto.Documento) ? null : dto.Documento!.Trim();

            var userId = GetUserId();

            try
            {
                var mov = await _caja.AddMovimientoEnCajaAbiertaAsync(dto, userId, ct);

                var res = new CajaMovimientoDto
                {
                    Id = mov.Id,
                    CajaAperturaId = mov.CajaAperturaId,
                    FechaUtc = DateTime.SpecifyKind(mov.FechaUtc, DateTimeKind.Utc),
                    Tipo = (int)mov.Tipo,
                    Monto = mov.Monto,
                    Concepto = mov.Concepto,
                    Observaciones = mov.Observaciones,
                    Documento = mov.Documento,
                    DocumentoId = mov.DocumentoId,
                    UsuarioId = mov.UsuarioId,
                    TimeZoneId = AppTz.Id,
                    FechaLocal = ToLocal(mov.FechaUtc),
                    FechaLocalIso = ToLocalIso(mov.FechaUtc)
                };

                return Ok(res);
            }
            catch (CajaFondosInsuficientesException ex)
            {
                return Conflict(new
                {
                    error = "Fondos insuficientes en caja",
                    disponible = ex.Disponible,
                    solicitado = ex.Solicitado
                });
            }
        }

        /* ================= Atajo: registrar GASTO ================= */
        public class CajaGastoDto
        {
            public decimal Monto { get; set; }
            public string? Concepto { get; set; }
            public string? Observaciones { get; set; }
        }

        [HttpPost("gastos")]
        public async Task<ActionResult<CajaMovimientoDto>> RegistrarGasto([FromBody] CajaGastoDto dto, CancellationToken ct)
        {
            if (dto == null) return BadRequest("Solicitud inválida.");
            if (dto.Monto <= 0) return BadRequest("El monto debe ser mayor a cero.");

            var userId = GetUserId();

            try
            {
                var mov = await _caja.AddMovimientoEnCajaAbiertaAsync(new CajaMovimientoCreateDto
                {
                    Tipo = (int)TipoMovimientoCaja.Egreso,
                    Monto = RoundMoney(dto.Monto),
                    Concepto = string.IsNullOrWhiteSpace(dto.Concepto) ? "Gasto" : dto.Concepto!.Trim(),
                    Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones!.Trim(),
                    Documento = "Gasto",
                    DocumentoId = null
                }, userId, ct);

                var res = new CajaMovimientoDto
                {
                    Id = mov.Id,
                    CajaAperturaId = mov.CajaAperturaId,
                    FechaUtc = DateTime.SpecifyKind(mov.FechaUtc, DateTimeKind.Utc),
                    Tipo = (int)mov.Tipo,
                    Monto = mov.Monto,
                    Concepto = mov.Concepto,
                    Observaciones = mov.Observaciones,
                    Documento = mov.Documento,
                    DocumentoId = mov.DocumentoId,
                    UsuarioId = mov.UsuarioId,
                    TimeZoneId = AppTz.Id,
                    FechaLocal = ToLocal(mov.FechaUtc),
                    FechaLocalIso = ToLocalIso(mov.FechaUtc)
                };

                return Ok(res);
            }
            catch (CajaFondosInsuficientesException ex)
            {
                return Conflict(new
                {
                    error = "Fondos insuficientes en caja",
                    disponible = ex.Disponible,
                    solicitado = ex.Solicitado
                });
            }
        }
    }
}

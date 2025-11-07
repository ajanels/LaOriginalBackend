using System;
using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos
{
    // ========= Apertura / Cierre =========
    public class CajaAbrirDto
    {
        [Range(0, double.MaxValue)]
        public decimal MontoInicial { get; set; }

        [StringLength(200)]
        public string? Observaciones { get; set; }

        [StringLength(120)]
        public string? CajeroNombre { get; set; }
    }

    public class CajaCerrarDto
    {
        [Required, Range(0, double.MaxValue)]
        public decimal MontoConteo { get; set; }

        [StringLength(200)]
        public string? Observaciones { get; set; }
    }

    // ========= Estado / Resumen =========
    public class CajaEstadoDto
    {
        public bool Abierta { get; set; }

        // Compatibilidad
        public int? AperturaId { get; set; }
        public DateTime? FechaAperturaUtc { get; set; }
        public decimal? MontoInicial { get; set; }

        // Usado por el front (UTC)
        public int? SesionId { get; set; }          // = AperturaId
        public string? Codigo { get; set; }         // Ej. A-0001
        public DateTime? Apertura { get; set; }     // UTC
        public string? CajeroNombre { get; set; }
        public decimal CapitalLiquido { get; set; }
        public decimal EfectivoInicial { get; set; }

        // NUEVO: Zona y valores locales
        public string? TimeZoneId { get; set; }     // p. ej. "America/Guatemala"
        public DateTime? AperturaLocal { get; set; }   // hora local
        public string? AperturaLocalIso { get; set; }  // ISO con offset (+00:00/-06:00)
    }

    public class CajaResumenDto
    {
        public int AperturaId { get; set; }
        public DateTime FechaAperturaUtc { get; set; }
        public DateTime? FechaCierreUtc { get; set; }

        public decimal MontoInicial { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Egresos { get; set; }
        public decimal Esperado { get; set; }

        public decimal? Conteo { get; set; }
        public decimal? Diferencia { get; set; }

        // NUEVO: Zona y valores locales
        public string? TimeZoneId { get; set; }
        public DateTime FechaAperturaLocal { get; set; }
        public DateTime? FechaCierreLocal { get; set; }
        public string? FechaAperturaLocalIso { get; set; }
        public string? FechaCierreLocalIso { get; set; }
    }

    // ========= Movimientos =========
    public class CajaMovimientoCreateDto
    {
        [Required] public int Tipo { get; set; }
        [Required, Range(0.0, double.MaxValue)]
        public decimal Monto { get; set; }

        [StringLength(120)]
        public string? Concepto { get; set; }

        [StringLength(200)]
        public string? Observaciones { get; set; }

        [StringLength(40)]
        public string? Documento { get; set; }
        public int? DocumentoId { get; set; }
    }

    public class CajaMovimientoDto
    {
        public int Id { get; set; }
        public int CajaAperturaId { get; set; }
        public DateTime FechaUtc { get; set; }
        public int Tipo { get; set; }
        public decimal Monto { get; set; }
        public string? Concepto { get; set; }
        public string? Observaciones { get; set; }
        public string? Documento { get; set; }
        public int? DocumentoId { get; set; }
        public int? UsuarioId { get; set; }

        // NUEVO: Local
        public string? TimeZoneId { get; set; }
        public DateTime FechaLocal { get; set; }
        public string FechaLocalIso { get; set; } = null!;
    }

    // ========= Auditoría: listado de sesiones =========
    public class CajaSesionListDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public DateTime Apertura { get; set; }      // UTC
        public string? CajeroNombre { get; set; }
        public string Estado { get; set; } = null!;

        // NUEVO: Local
        public string? TimeZoneId { get; set; }
        public DateTime AperturaLocal { get; set; }
        public string AperturaLocalIso { get; set; } = null!;
    }
}

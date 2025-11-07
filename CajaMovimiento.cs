using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;   // 🆕

namespace LaOriginalBackend.Models
{
    // Único enum oficial
    public enum TipoMovimientoCaja
    {
        Apertura = 0,
        Cierre = 1,
        Ingreso = 2,
        Egreso = 3,
        CobroVenta = 4,
        PagoProveedor = 5,
        Ajuste = 6
    }

    [Index(nameof(CajaAperturaId))]  // 🆕
    [Index(nameof(FechaUtc))]        // 🆕
    [Index(nameof(Tipo))]            // 🆕
    public class CajaMovimiento
    {
        public int Id { get; set; }

        public int CajaAperturaId { get; set; }
        public CajaApertura CajaApertura { get; set; } = null!;

        public DateTime FechaUtc { get; set; }

        public TipoMovimientoCaja Tipo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [StringLength(120)]
        public string? Concepto { get; set; }

        [StringLength(200)]
        public string? Observaciones { get; set; }

        [StringLength(40)]
        public string? Documento { get; set; }

        public int? DocumentoId { get; set; }

        public int? UsuarioId { get; set; }
    }
}

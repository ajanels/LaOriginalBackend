using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaOriginalBackend.Models
{
    public class CajaApertura
    {
        public int Id { get; set; }

        [StringLength(20)]
        public string? Codigo { get; set; }              // Ej. A-0001

        public DateTime FechaAperturaUtc { get; set; }
        public DateTime? FechaCierreUtc { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoInicial { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontoCierreDeclarado { get; set; }

        [StringLength(120)]
        public string? CajeroNombre { get; set; }

        [StringLength(200)]
        public string? ObservacionesApertura { get; set; }

        [StringLength(200)]
        public string? ObservacionesCierre { get; set; }

        public ICollection<CajaMovimiento> Movimientos { get; set; } = new List<CajaMovimiento>();
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaOriginalBackend.Models
{
    // Si prefieres que la tabla se llame "UnidadesMedida":
    [Table("UnidadesMedida")]
    public class UnidadMedida
    {
        public int Id { get; set; }

        // Ej: Metro, Kilogramo, Yarda, Madeja
        [Required, StringLength(50, MinimumLength = 2)]
        public string Nombre { get; set; } = null!;

        // Ej: m, kg, yd, mdj
        [Required, StringLength(10, MinimumLength = 1)]
        public string Simbolo { get; set; } = null!;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        // Activo/Inactivo
        public bool Activo { get; set; } = true;
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models
{
    [Index(nameof(NIT), IsUnique = true)]
    [Index(nameof(Nombre), IsUnique = true)]
    public class Proveedor
    {
        public int Id { get; set; }

        [Required, StringLength(120, MinimumLength = 3)]
        public string Nombre { get; set; } = null!;

        // NIT de Guatemala (permitimos letras y guion)
        [Required, StringLength(20)]
        [RegularExpression(@"^[A-Za-z0-9-]{3,20}$", ErrorMessage = "NIT inválido.")]
        public string NIT { get; set; } = null!;

        [StringLength(120)]
        public string? Contacto { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [EmailAddress, StringLength(120)]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Direccion { get; set; }

        [StringLength(200)]
        public string? Notas { get; set; }

        public bool Activo { get; set; } = true;
    }
}

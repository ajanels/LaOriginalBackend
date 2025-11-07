using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models
{
    [Index(nameof(Nombre), nameof(Telefono))]
    public class Cliente
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Nombre { get; set; } = null!; 

        [StringLength(9)]
        public string? NIT { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [StringLength(120), EmailAddress]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Direccion { get; set; }

        [StringLength(200)]
        public string? Notas { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Activo/Inactivo
        public bool Activo { get; set; } = true;
    }
}

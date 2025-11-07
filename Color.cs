using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models
{
    [Index(nameof(Nombre), IsUnique = true)]
    public class Color
    {
        public int Id { get; set; }

        [Required, StringLength(60, MinimumLength = 2)]
        public string Nombre { get; set; } = null!;

        [StringLength(7)]
        public string? Hex { get; set; }  

        public bool Activo { get; set; } = true;

        [StringLength(200)]
        public string? Notas { get; set; }
    }
}

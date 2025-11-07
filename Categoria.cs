using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models
{
    [Index(nameof(Nombre), IsUnique = true)]
    public class Categoria
    {
        public int Id { get; set; }

        [Required, StringLength(80, MinimumLength = 2)]
        public string Nombre { get; set; } = null!;

        // Prefijo para generar códigos (ej.: "A" para Telas). Si no hay, se usa la 1ra letra del nombre.
        [StringLength(2)]
        public string? Prefijo { get; set; }

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;
    }
}

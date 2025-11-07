using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models
{
    [Index(nameof(Nombre), IsUnique = true)]
    public class Rol
    {
        public int Id { get; set; }

        // Admin, Vendedor, Caja, etc.
        [Required, StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñ0-9 .\-\/]{3,50}$")]
        public string Nombre { get; set; } = null!;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        // <<-- Cambiamos de string a bool
        public bool Activo { get; set; } = true;

        [InverseProperty(nameof(Usuario.Rol))]
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}

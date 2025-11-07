using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Models
{
    public class Estado
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Tipo { get; set; } = default!;   

        [Required, MaxLength(80)]
        public string Nombre { get; set; } = default!; 

        public bool Activo { get; set; } = true;

        [MaxLength(200)]
        public string? Notas { get; set; }
    }
}

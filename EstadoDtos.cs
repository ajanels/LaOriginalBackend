using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos
{
    public class EstadoListDto
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = default!;
        public string Nombre { get; set; } = default!;
        public bool Activo { get; set; }
    }

    public class EstadoUpsertDto
    {
        [Required, MaxLength(50)]
        public string Tipo { get; set; } = default!;

        [Required, MaxLength(80)]
        public string Nombre { get; set; } = default!;

        public bool Activo { get; set; } = true;

        [MaxLength(200)]
        public string? Notas { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos
{
    public class UnidadMedidaListDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string Simbolo { get; set; } = null!;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
    }

    public class UnidadMedidaCreateDto
    {
        [Required, StringLength(50, MinimumLength = 2)]
        public string Nombre { get; set; } = null!;

        [Required, StringLength(10, MinimumLength = 1)]
        public string Simbolo { get; set; } = null!;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;
    }

    public class UnidadMedidaUpdateDto
    {
        [Required] public int Id { get; set; }

        [Required, StringLength(50, MinimumLength = 2)]
        public string Nombre { get; set; } = null!;

        [Required, StringLength(10, MinimumLength = 1)]
        public string Simbolo { get; set; } = null!;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;
    }

    public class UnidadMedidaToggleDto
    {
        public bool Activo { get; set; }
    }
}

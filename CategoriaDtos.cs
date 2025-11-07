using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos
{
    public class CategoriaListDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
    }

    public class CategoriaCreateDto
    {
        [Required, StringLength(80, MinimumLength = 2)]
        public string Nombre { get; set; } = null!;
        [StringLength(200)]
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class CategoriaUpdateDto
    {
        [Required] public int Id { get; set; }
        [Required, StringLength(80, MinimumLength = 2)]
        public string Nombre { get; set; } = null!;
        [StringLength(200)]
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
    }

    public class CategoriaToggleDto
    {
        public bool Activo { get; set; }
    }
}

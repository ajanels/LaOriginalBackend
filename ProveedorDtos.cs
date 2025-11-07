using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos
{
    public class ProveedorListDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string NIT { get; set; } = null!;
        public string? Contacto { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public bool Activo { get; set; }
    }

    public class ProveedorDetailDto : ProveedorListDto
    {
        public string? Direccion { get; set; }
        public string? Notas { get; set; }
    }

    public class ProveedorCreateDto
    {
        [Required, StringLength(120, MinimumLength = 3)]
        public string Nombre { get; set; } = null!;

        [Required, StringLength(20)]
        [RegularExpression(@"^[A-Za-z0-9-]{3,20}$")]
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

    public class ProveedorUpdateDto : ProveedorCreateDto
    {
        [Required] public int Id { get; set; }
    }

    public class ProveedorToggleDto
    {
        public bool Activo { get; set; }
    }
}

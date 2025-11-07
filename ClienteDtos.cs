using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos
{
    public class ClienteListDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string? NIT { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public bool Activo { get; set; }
    }

    public class ClienteCreateDto
    {
        [Required, StringLength(150)]
        public string Nombre { get; set; } = null!;

        [StringLength(20)]
        public string? NIT { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [StringLength(120), EmailAddress]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Direccion { get; set; }

        [StringLength(200)]
        public string? Notas { get; set; }

        public bool Activo { get; set; } = true;
    }

    public class ClienteUpdateDto
    {
        [Required] public int Id { get; set; }

        [Required, StringLength(150)]
        public string Nombre { get; set; } = null!;

        [StringLength(20)]
        public string? NIT { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [StringLength(120), EmailAddress]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Direccion { get; set; }

        [StringLength(200)]
        public string? Notas { get; set; }

        public bool Activo { get; set; } = true;
    }

    public class ClienteToggleDto
    {
        public bool Activo { get; set; }
    }
}

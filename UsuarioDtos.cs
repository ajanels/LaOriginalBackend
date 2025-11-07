using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LaOriginalBackend.Dtos
{
    public class RolMiniDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
    }

    public class UsuarioListDto
    {
        public int Id { get; set; }
        public string PrimerNombre { get; set; } = null!;
        public string? SegundoNombre { get; set; }
        public string PrimerApellido { get; set; } = null!;
        public string? SegundoApellido { get; set; }

        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Celular { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public string? FotoUrl { get; set; }

        public int RolId { get; set; }
        public RolMiniDto? Rol { get; set; }
    }

    public class UsuarioDetailDto : UsuarioListDto
    {
        public string NIT { get; set; } = null!;
        public string CUI { get; set; } = null!;
        public DateTime FechaNacimiento { get; set; }
        public DateTime FechaIngreso { get; set; }
        public string Genero { get; set; } = null!;
        public string Direccion { get; set; } = null!;
    }

    public class UsuarioCreateDto
    {
        [Required] public string PrimerNombre { get; set; } = null!;
        public string? SegundoNombre { get; set; }

        [Required] public string PrimerApellido { get; set; } = null!;
        public string? SegundoApellido { get; set; }

        [Required, RegularExpression(@"^\d{9}$")]
        public string NIT { get; set; } = null!;

        [Required, RegularExpression(@"^\d{13}$")]
        public string CUI { get; set; } = null!;

        [Required]
        public DateTime FechaNacimiento { get; set; }

        [Required]
        public DateTime FechaIngreso { get; set; }

        [Required, RegularExpression(@"^[2-7]\d{7}$")]
        public string Celular { get; set; } = null!;

        [Required]
        [RegularExpression("^(Masculino|Femenino|Otro|Prefiero no decir)$")]
        public string Genero { get; set; } = null!;

        [Required]
        [RegularExpression("^(Activo|Inactivo|Suspendido)$")]
        public string Estado { get; set; } = null!;

        [Required, StringLength(120, MinimumLength = 10)]
        public string Direccion { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public int RolId { get; set; }

        [Required]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,64}$")]
        public string Password { get; set; } = null!;

        public IFormFile? Foto { get; set; }
    }

    public class UsuarioUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required] public string PrimerNombre { get; set; } = null!;
        public string? SegundoNombre { get; set; }

        [Required] public string PrimerApellido { get; set; } = null!;
        public string? SegundoApellido { get; set; }

        [Required, RegularExpression(@"^\d{9}$")]
        public string NIT { get; set; } = null!;

        [Required, RegularExpression(@"^\d{13}$")]
        public string CUI { get; set; } = null!;

        [Required]
        public DateTime FechaNacimiento { get; set; }

        [Required]
        public DateTime FechaIngreso { get; set; }

        [Required, RegularExpression(@"^[2-7]\d{7}$")]
        public string Celular { get; set; } = null!;

        [Required]
        [RegularExpression("^(Masculino|Femenino)$")]
        public string Genero { get; set; } = null!;

        [Required]
        [RegularExpression("^(Activo|Inactivo|Suspendido)$")]
        public string Estado { get; set; } = null!;

        [Required, StringLength(120, MinimumLength = 10)]
        public string Direccion { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public int RolId { get; set; }

        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,64}$")]
        public string? Password { get; set; }
    }
}

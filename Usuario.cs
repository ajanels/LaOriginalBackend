// Models/Usuario.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models
{
    [Index(nameof(Username), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    [Index(nameof(NIT), IsUnique = true)]
    [Index(nameof(CUI), IsUnique = true)]
    public class Usuario
    {
        public int Id { get; set; }

        // Nombres y apellidos
        [Required, StringLength(50, MinimumLength = 2)]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñ'\s-]{2,50}$")]
        public string PrimerNombre { get; set; } = null!;

        [StringLength(50, MinimumLength = 2)]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñ'\s-]{2,50}$")]
        public string? SegundoNombre { get; set; }

        [Required, StringLength(50, MinimumLength = 2)]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñ'\s-]{2,50}$")]
        public string PrimerApellido { get; set; } = null!;

        [StringLength(50, MinimumLength = 2)]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñ'\s-]{2,50}$")]
        public string? SegundoApellido { get; set; }

        // Identificación (GT)
        [Required, RegularExpression(@"^\d{9}$")]
        public string NIT { get; set; } = null!;

        [Required, RegularExpression(@"^\d{13}$")]
        public string CUI { get; set; } = null!;

        // Fechas
        [Required] public DateTime FechaNacimiento { get; set; }
        [Required] public DateTime FechaIngreso { get; set; }

        // Contacto
        [Required, RegularExpression(@"^[2-7]\d{7}$")]
        public string Celular { get; set; } = null!;

        [Required]
        [RegularExpression("^(Masculino|Femenino|Otro|Prefiero no decir)$")]
        public string Genero { get; set; } = null!;

        [Required]
        [RegularExpression("^(Activo|Inactivo|Suspendido)$")]
        public string Estado { get; set; } = null!;

        [Required, StringLength(120, MinimumLength = 10)]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñ0-9 #\.,\-°\/]{10,120}$")]
        public string Direccion { get; set; } = null!;

        // Rol
        [Required] public int RolId { get; set; }
        [ForeignKey(nameof(RolId))] public Rol Rol { get; set; } = null!;

        // Cuenta
        [Required, EmailAddress]
        [RegularExpression(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$")]
        public string Email { get; set; } = null!;
        public string Username { get; set; } = null!;
        [Required] public string PasswordHash { get; set; } = null!;
        public string? FotoUrl { get; set; }

        // === Primer inicio de sesión ===
        public bool DebeCambiarPassword { get; set; } = true;
        public DateTime? PasswordChangedAtUtc { get; set; }
    }
}

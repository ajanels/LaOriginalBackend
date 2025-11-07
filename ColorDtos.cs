using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos
{
    public class ColorListDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Hex { get; set; }
        public bool Activo { get; set; }
        public string? Notas { get; set; }
    }

    public class ColorCreateDto
    {
        [Required, StringLength(60, MinimumLength = 2)]
        public string Nombre { get; set; } = null!;

        [RegularExpression("^#?[0-9A-Fa-f]{6}$", ErrorMessage = "Hex inválido (use 6 dígitos hexadecimales).")]
        public string? Hex { get; set; }

        public bool Activo { get; set; } = true;

        [StringLength(200)]
        public string? Notas { get; set; }
    }

    public class ColorUpdateDto : ColorCreateDto
    {
        [Required] public int Id { get; set; }
    }

    public class ColorToggleDto
    {
        public bool Activo { get; set; }
    }
}

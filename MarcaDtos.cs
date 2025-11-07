using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos;

public class MarcaListDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}

public class MarcaCreateDto
{
    [Required, StringLength(120)]
    public string Nombre { get; set; } = null!;
    [StringLength(200)]
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

public class MarcaUpdateDto : MarcaCreateDto
{
    [Required]
    public int Id { get; set; }
}

public class MarcaToggleDto
{
    public bool Activo { get; set; }
}

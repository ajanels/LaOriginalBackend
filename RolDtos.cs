// Dtos/RolDtos.cs
using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos;

public class RolListDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }

    // Útil para la UI (opcional)
    public string EstadoTexto => Activo ? "Activo" : "Inactivo";
}

public class RolUpsertDto
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Nombre { get; set; } = null!;

    [StringLength(200)]
    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models;

[Index(nameof(Nombre), IsUnique = true, Name = "IX_Marcas_Nombre")]
public class Marca
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Nombre { get; set; } = null!;

    [StringLength(200)]
    public string? Descripcion { get; set; }

    /// <summary>
    /// Activo = true -> disponible en catálogos; false -> oculto/inactivo
    /// </summary>
    public bool Activo { get; set; } = true;
}

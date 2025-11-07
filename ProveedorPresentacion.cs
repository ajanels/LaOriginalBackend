using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models;

[Index(nameof(ProveedorId), nameof(PresentacionId), IsUnique = true)]
public class ProveedorPresentacion
{
    public int Id { get; set; }

    [Required] public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    [Required] public int PresentacionId { get; set; }
    public Presentacion Presentacion { get; set; } = null!;

    [StringLength(60)] public string? CodigoProveedor { get; set; }
    [Precision(18, 2)] public decimal? PrecioLista { get; set; }
    [Precision(18, 2)] public decimal? PrecioUltimo { get; set; } 
    [StringLength(200)] public string? Notas { get; set; }
    public bool Activo { get; set; } = true;
}

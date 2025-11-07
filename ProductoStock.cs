using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models;

[Index(nameof(PresentacionId), IsUnique = true)]
public class ProductoStock
{
    public int Id { get; set; }

    [Required]
    public int PresentacionId { get; set; }

    [Precision(18, 2)]
    public decimal Cantidad { get; set; } = 0m;

    [Precision(18, 2)]
    public decimal? Minimo { get; set; }

    // ➕ nuevo: costo promedio
    [Precision(18, 2)]
    public decimal CostoPromedio { get; set; } = 0m;

    [ForeignKey(nameof(PresentacionId))]
    public Presentacion Presentacion { get; set; } = null!;
}

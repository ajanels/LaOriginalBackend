using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models;

public class VentaDetalle
{
    public int Id { get; set; }

    [Required]
    public int VentaId { get; set; }

    [Required]
    public int PresentacionId { get; set; }

    [Precision(18, 2)]
    public decimal Cantidad { get; set; }

    [Precision(18, 2)]
    public decimal PrecioUnitario { get; set; }

    [Precision(18, 2)]
    public decimal DescuentoUnitario { get; set; }

    [Precision(18, 2)]
    public decimal TotalLinea { get; set; }

    // NUEVO: costo “congelado” en el momento de vender
    [Precision(18, 2)]
    public decimal CostoUnitario { get; set; }  // <= este campo es clave

    [StringLength(200)]
    public string? Notas { get; set; }

    [ForeignKey(nameof(VentaId))]
    public Venta Venta { get; set; } = null!;

    [ForeignKey(nameof(PresentacionId))]
    public Presentacion Presentacion { get; set; } = null!;
}

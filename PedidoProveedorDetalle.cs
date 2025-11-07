// Models/PedidoProveedorDetalle.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models;

[Index(nameof(PresentacionId))]
public class PedidoProveedorDetalle
{
    public int Id { get; set; }

    [Required]
    public int PedidoProveedorId { get; set; }
    public PedidoProveedor Pedido { get; set; } = null!;

    // ✅ Alineado con tu esquema: operamos por Presentación
    [Required]
    public int PresentacionId { get; set; }
    public Presentacion Presentacion { get; set; } = null!;

    [Precision(18, 2)]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Cantidad { get; set; }

    [Precision(18, 2)]
    public decimal PrecioUnitario { get; set; }

    [Precision(18, 2)]
    public decimal Descuento { get; set; }

    [Precision(18, 2)]
    public decimal TotalLinea { get; set; }

    // Para recepciones parciales y trazabilidad
    [Precision(18, 2)]
    public decimal CantidadRecibida { get; set; } = 0m;

    [StringLength(200)]
    public string? Notas { get; set; }
}

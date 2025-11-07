// Models/CompraDetalle.cs
using LaOriginalBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

public class CompraDetalle
{
    public int Id { get; set; }
    [Required]
    public int CompraId { get; set; }
    [Required]
    public int PresentacionId { get; set; }

    // 🔗 Traza contra el pedido (permite parciales)
    public int? PedidoProveedorDetalleId { get; set; }
    public PedidoProveedorDetalle? PedidoDetalle { get; set; }

    [Precision(18, 2)]
    public decimal Cantidad { get; set; }

    [Precision(18, 2)]
    public decimal CostoUnitario { get; set; }

    [Precision(18, 2)]
    public decimal TotalLinea { get; set; }

    [StringLength(200)]
    public string? Notas { get; set; }

    public Compra Compra { get; set; } = null!;
    public Presentacion Presentacion { get; set; } = null!;
}

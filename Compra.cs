using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models;

[Index(nameof(Numero), IsUnique = false)]
[Index(nameof(Fecha))]           // 🆕 para filtros por fecha
[Index(nameof(ProveedorId))]     // 🆕 para "compras por proveedor"
public class Compra
{
    public int Id { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [Required]
    public int ProveedorId { get; set; }

    [StringLength(40)]
    public string? Numero { get; set; } // No. factura o correlativo interno

    [StringLength(200)]
    public string? Observaciones { get; set; }

    [Precision(18, 2)]
    public decimal Subtotal { get; set; }

    [Precision(18, 2)]
    public decimal Descuento { get; set; }

    [Precision(18, 2)]
    public decimal Total { get; set; }

    // Estado simple por ahora
    [Required, StringLength(20)]
    public string Estado { get; set; } = "Registrada"; // Registrada / Anulada

    public bool Anulada { get; set; } = false;

    // FK opcional a forma de pago
    public int? FormaPagoId { get; set; }

    // Auditoría ligera
    public int? UsuarioId { get; set; }

    // Nav
    [ForeignKey(nameof(ProveedorId))]
    public Proveedor Proveedor { get; set; } = null!;

    [ForeignKey(nameof(FormaPagoId))]
    public FormaPago? FormaPago { get; set; }

    public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
}

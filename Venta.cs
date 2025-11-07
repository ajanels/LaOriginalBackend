using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models;

[Index(nameof(Serie))]
[Index(nameof(Numero))]
[Index(nameof(Fecha))]         // 🆕 acelera reportes por fecha
[Index(nameof(UsuarioId))]     // 🆕 acelera "ventas por usuario"
[Index(nameof(ClienteId))]     // 🆕 acelera "top clientes"
public class Venta
{
    public int Id { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int? ClienteId { get; set; }

    [StringLength(10)]
    public string? Serie { get; set; }

    [StringLength(40)]
    public string? Numero { get; set; }

    [StringLength(200)]
    public string? Observaciones { get; set; }

    [Precision(18, 2)]
    public decimal Subtotal { get; set; }

    [Precision(18, 2)]
    public decimal Descuento { get; set; }

    [Precision(18, 2)]
    public decimal Total { get; set; }

    [Required, StringLength(20)]
    public string Estado { get; set; } = "Registrada"; // Registrada | Anulada

    public bool Anulada { get; set; } = false;

    public int? FormaPagoId { get; set; }
    public int? UsuarioId { get; set; }

    // Nav
    [ForeignKey(nameof(ClienteId))]
    public Cliente? Cliente { get; set; }

    [ForeignKey(nameof(FormaPagoId))]
    public FormaPago? FormaPago { get; set; }

    public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
}

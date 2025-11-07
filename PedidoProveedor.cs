// Models/PedidoProveedor.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models;

public enum EstadoPedidoProveedor
{
    Borrador = 0,
    Enviado = 1,
    Aprobado = 2,
    ParcialmenteRecibido = 3,
    Cerrado = 4,
    Cancelado = 5
}

// ✅ Hacemos único el número a nivel de modelo (puedes omitir este atributo si prefieres solo Fluent API)
[Index(nameof(Numero), IsUnique = true)]
public class PedidoProveedor
{
    public int Id { get; set; }

    [Required]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [StringLength(40)]
    public string? Numero { get; set; }      // OC-000123 u otro correlativo

    [Required]
    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    [StringLength(200)]
    public string? Observaciones { get; set; }

    [Precision(18, 2)]
    public decimal Subtotal { get; set; }

    [Precision(18, 2)]
    public decimal Descuento { get; set; }

    [Precision(18, 2)]
    public decimal Total { get; set; }

    // Flujo del pedido
    [Required]
    public EstadoPedidoProveedor Estado { get; set; } = EstadoPedidoProveedor.Borrador;

    // Auditoría mínima
    public int? UsuarioCreaId { get; set; }
    public Usuario? UsuarioCrea { get; set; }
    public int? UsuarioApruebaId { get; set; }
    public Usuario? UsuarioAprueba { get; set; }
    public DateTime? AprobadoEl { get; set; }

    public ICollection<PedidoProveedorDetalle> Detalles { get; set; } = new List<PedidoProveedorDetalle>();
}

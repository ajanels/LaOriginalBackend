using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Models;

public class DevolucionCompra
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    // Enlace a compra original (opcional)
    public int? CompraId { get; set; }
    public Compra? Compra { get; set; }

    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

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
    public string Estado { get; set; } = "Registrada";

    public bool Anulada { get; set; } = false;

    public int? FormaPagoId { get; set; }
    public FormaPago? FormaPago { get; set; }

    public int? UsuarioId { get; set; }

    public ICollection<DevolucionCompraDetalle> Detalles { get; set; } = new List<DevolucionCompraDetalle>();
}

public class DevolucionCompraDetalle
{
    public int Id { get; set; }

    public int DevolucionCompraId { get; set; }
    public DevolucionCompra Devolucion { get; set; } = null!;

    public int PresentacionId { get; set; }
    public Presentacion Presentacion { get; set; } = null!;

    [Precision(18, 2)]
    public decimal Cantidad { get; set; }

    [Precision(18, 2)]
    public decimal CostoUnitario { get; set; }

    [Precision(18, 2)]
    public decimal TotalLinea { get; set; }

    [StringLength(200)]
    public string? Notas { get; set; }
}

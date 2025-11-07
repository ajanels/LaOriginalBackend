using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Models;

public class DevolucionVenta
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    // Opcional: enlazar a la venta original si aplica
    public int? VentaId { get; set; }
    public Venta? Venta { get; set; }

    // También guardamos el cliente (aunque venga por la venta)
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

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

    public int? UsuarioId { get; set; } // quien registró
    public ICollection<DevolucionVentaDetalle> Detalles { get; set; } = new List<DevolucionVentaDetalle>();
}

public class DevolucionVentaDetalle
{
    public int Id { get; set; }

    public int DevolucionVentaId { get; set; }
    public DevolucionVenta Devolucion { get; set; } = null!;

    public int PresentacionId { get; set; }
    public Presentacion Presentacion { get; set; } = null!;

    [Precision(18, 2)]
    public decimal Cantidad { get; set; }

    [Precision(18, 2)]
    public decimal PrecioUnitario { get; set; }

    [Precision(18, 2)]
    public decimal DescuentoUnitario { get; set; }

    [Precision(18, 2)]
    public decimal TotalLinea { get; set; }

    [StringLength(200)]
    public string? Notas { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos.Devoluciones;

public class DVItemCreateDto
{
    [Required] public int PresentacionId { get; set; }

    [Range(0.0001, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [Range(0.0, double.MaxValue)]
    public decimal PrecioUnitario { get; set; }

    [Range(0.0, double.MaxValue)]
    public decimal DescuentoUnitario { get; set; } = 0m;

    [StringLength(200)]
    public string? Notas { get; set; }
}

public class DevolucionVentaCreateDto
{
    public int? VentaId { get; set; }
    public int? ClienteId { get; set; }
    public int? FormaPagoId { get; set; }
    public string? Numero { get; set; }
    public string? Observaciones { get; set; }

    [Required, MinLength(1)]
    public List<DVItemCreateDto> Detalles { get; set; } = new();
}

public class DVItemDto
{
    public int Id { get; set; }
    public int PresentacionId { get; set; }
    public string Producto { get; set; } = null!;
    public string Presentacion { get; set; } = null!;
    public string Unidad { get; set; } = null!;
    public string? Color { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescuentoUnitario { get; set; }
    public decimal TotalLinea { get; set; }
    public string? Notas { get; set; }
}

public class DevolucionVentaDetailDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }

    public int? VentaId { get; set; }
    public int? ClienteId { get; set; }
    public string? Cliente { get; set; }

    public string? Numero { get; set; }
    public string? Observaciones { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }

    public string Estado { get; set; } = null!;
    public bool Anulada { get; set; }

    public int? FormaPagoId { get; set; }
    public string? FormaPago { get; set; }

    public List<DVItemDto> Detalles { get; set; } = new();
}

public class DevolucionVentaListDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string? Numero { get; set; }
    public string? Cliente { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = null!;
    public bool Anulada { get; set; }
}

public class DVAnularDto
{
    public string? Motivo { get; set; }
}

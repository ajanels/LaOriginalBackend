using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos.Devoluciones;

public class DCItemCreateDto
{
    [Required] public int PresentacionId { get; set; }

    [Range(0.0001, double.MaxValue)]
    public decimal Cantidad { get; set; }

    [Range(0.0, double.MaxValue)]
    public decimal CostoUnitario { get; set; }

    public string? Notas { get; set; }
}

public class DevolucionCompraCreateDto
{
    public int? CompraId { get; set; }
    [Required] public int ProveedorId { get; set; }
    public int? FormaPagoId { get; set; }
    public string? Numero { get; set; }
    public string? Observaciones { get; set; }

    [Required, MinLength(1)]
    public List<DCItemCreateDto> Detalles { get; set; } = new();
}

public class DCItemDto
{
    public int Id { get; set; }
    public int PresentacionId { get; set; }
    public string Producto { get; set; } = null!;
    public string Presentacion { get; set; } = null!;
    public string Unidad { get; set; } = null!;
    public string? Color { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal TotalLinea { get; set; }
    public string? Notas { get; set; }
}

public class DevolucionCompraDetailDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public int? CompraId { get; set; }
    public int ProveedorId { get; set; }
    public string Proveedor { get; set; } = null!;
    public string? Numero { get; set; }
    public string? Observaciones { get; set; }
    public int? FormaPagoId { get; set; }
    public string? FormaPago { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = null!;
    public bool Anulada { get; set; }

    public List<DCItemDto> Detalles { get; set; } = new();
}

public class DevolucionCompraListDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Proveedor { get; set; } = null!;
    public string? Numero { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = null!;
    public bool Anulada { get; set; }
}

public class DCAnularDto
{
    public string? Motivo { get; set; }
}

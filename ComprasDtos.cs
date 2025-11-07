using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos;

public class CompraCreateDto
{
    [Required] public int ProveedorId { get; set; }
    public string? Numero { get; set; }
    public string? Observaciones { get; set; }
    public int? FormaPagoId { get; set; }
    public string? Referencia { get; set; }


    [Required, MinLength(1)]
    public List<CompraDetalleCreateDto> Detalles { get; set; } = new();
}

public class CompraDetalleCreateDto
{
    [Required] public int PresentacionId { get; set; }
    [Required] public decimal Cantidad { get; set; } // > 0
    [Required] public decimal CostoUnitario { get; set; } // >= 0
    public string? Notas { get; set; }
}

public class CompraListDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Proveedor { get; set; } = null!;
    public string? Numero { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = null!;
    public bool Anulada { get; set; }
}

public class CompraDetailDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
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

    public List<CompraDetalleDto> Detalles { get; set; } = new();
}

public class CompraDetalleDto
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

public class CompraAnularDto
{
    public string? Motivo { get; set; }
}

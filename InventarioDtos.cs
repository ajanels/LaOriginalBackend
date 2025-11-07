using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos;

public class StockListDto
{
    public int PresentacionId { get; set; }

    // producto
    public int ProductoId { get; set; }
    public string Producto { get; set; } = null!;
    public string? ProductoCodigo { get; set; }
    public string? FotoUrl { get; set; }

    // stock
    public decimal Cantidad { get; set; }
    public decimal? Minimo { get; set; }

    public bool BajoMinimo => Minimo.HasValue && Cantidad < Minimo.Value;
    public decimal? PrecioVenta { get; set; }

}

public class StockDetailDto : StockListDto { }

public class KardexItemDto
{
    public int Id { get; set; }
    public DateTime FechaUtc { get; set; }
    public string Tipo { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal? CostoUnitario { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public string? Documento { get; set; }
    public int? DocumentoId { get; set; }
    public string? Notas { get; set; }
}

public class AjusteInventarioDto
{
    [Required]
    public int PresentacionId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public decimal Cantidad { get; set; }   

    [Required]
    [RegularExpression("^(entrada|salida)$", ErrorMessage = "El tipo debe ser 'entrada' o 'salida'.")]
    public string Tipo { get; set; } = "entrada";  

    [StringLength(200)]
    public string? Motivo { get; set; }

    public decimal? CostoUnitario { get; set; }   
}


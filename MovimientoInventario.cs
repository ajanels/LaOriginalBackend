using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models;

public enum TipoMovimiento
{
    Entrada = 1,
    Salida = 2,
    Ajuste = 3
}

[Index(nameof(PresentacionId))]
[Index(nameof(FechaUtc))]
public class MovimientoInventario
{
    public int Id { get; set; }

    [Required]
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public int PresentacionId { get; set; }

    [Required]
    public TipoMovimiento Tipo { get; set; } = TipoMovimiento.Ajuste;

    // Cantidad > 0 para entradas, < 0 para salidas/ajustes negativos
    [Precision(18, 2)]
    public decimal Cantidad { get; set; }

    // Opcionales (útiles cuando integremos compras/ventas)
    [Precision(18, 2)]
    public decimal? CostoUnitario { get; set; }

    [Precision(18, 2)]
    public decimal? PrecioUnitario { get; set; }

    [StringLength(40)]
    public string? Documento { get; set; } // "Compra", "Venta", "Ajuste", etc.

    public int? DocumentoId { get; set; }

    [StringLength(200)]
    public string? Notas { get; set; }

    public int? UsuarioId { get; set; }

    // Nav
    [ForeignKey(nameof(PresentacionId))]
    public Presentacion Presentacion { get; set; } = null!;
}

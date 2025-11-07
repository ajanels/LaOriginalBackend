// Models/Presentacion.cs
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaOriginalBackend.Models;

public class Presentacion
{
    public int Id { get; set; }

    // Relación con producto (requerida)
    [Required]
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    [Required, StringLength(80)]
    public string Nombre { get; set; } = null!; // “Cono 500 m”, “Docena”, “Traje completo”, etc.

    // Unidad + factor (conversiones: docena=12, cono=1, etc.)
    [Required]
    public int UnidadMedidaId { get; set; }
    public UnidadMedida Unidad { get; set; } = null!;

    [Precision(12, 4)]
    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")] // > 0
    public decimal Factor { get; set; } = 1m; // cuántas unidades base contiene esta presentación

    // Variantes simples
    public int? ColorId { get; set; }
    public Color? Color { get; set; }

    // Identificadores/etiquetas comerciales
    [StringLength(60)]
    public string? SKU { get; set; }

    [StringLength(50)]
    public string? CodigoBarras { get; set; }

    // Precios por defecto (opcionales; pueden venir de listas de precios)
    [Precision(18, 2)]
    public decimal? PrecioCompraDefault { get; set; }

    [Precision(18, 2)]
    public decimal? PrecioVentaDefault { get; set; }

    // Umbrales de stock (opcional)
    [Precision(18, 2)]
    public decimal? StockMinimo { get; set; }

    public bool Activo { get; set; } = true;

    // Solo una presentación principal por producto (la que usa el POS)
    public bool EsPrincipal { get; set; } = false;
}

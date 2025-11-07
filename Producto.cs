// Models/Producto.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models;

public class Producto
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Nombre { get; set; } = null!;

    [StringLength(60)]
    public string? Codigo { get; set; }

    [StringLength(300)]
    public string? FotoUrl { get; set; }

    public bool Activo { get; set; } = true;

    // Categoria (nullable si tienes datos antiguos sin categoría)
    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    // ✅ NUEVO: precios por defecto a nivel de producto (nullable para no romper datos existentes)
    [Precision(18, 2)]
    public decimal? PrecioCompraDefault { get; set; }

    [Precision(18, 2)]
    public decimal? PrecioVentaDefault { get; set; }

    public ICollection<Presentacion> Presentaciones { get; set; } = new List<Presentacion>();
}

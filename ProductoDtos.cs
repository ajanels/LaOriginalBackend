using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos
{
    public class ProductoListDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Codigo { get; set; }
        public string? Categoria { get; set; }
        public bool Activo { get; set; }
        public int Presentaciones { get; set; }
        public string? FotoUrl { get; set; }
    }

    public class ProductoDetailDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Codigo { get; set; }
        public bool Activo { get; set; }
        public int? CategoriaId { get; set; }
        public string? Categoria { get; set; }
        public string? FotoUrl { get; set; }

        // Prellenado para edición (presentación principal)
        public int? ProveedorId { get; set; }
        public decimal? PrecioCompraDefault { get; set; }
        public decimal? PrecioVentaDefault { get; set; }
    }

    public class ProductoCreateDto
    {
        [Required, StringLength(120)]
        public string Nombre { get; set; } = null!;

        [Required] public int CategoriaId { get; set; }
        [Required] public int ProveedorId { get; set; }

        [Required, StringLength(300)]
        public string FotoUrl { get; set; } = null!;

        [Required] public decimal PrecioCompraDefault { get; set; }
        [Required] public decimal PrecioVentaDefault { get; set; }

        public bool Activo { get; set; } = true;
    }

    public class ProductoUpdateDto
    {
        [Required] public int Id { get; set; }

        [Required, StringLength(120)]
        public string Nombre { get; set; } = null!;

        [Required] public int CategoriaId { get; set; }

        [Required, StringLength(300)]
        public string FotoUrl { get; set; } = null!;

        public bool Activo { get; set; } = true;

        // NUEVO: actualización opcional de precios de la presentación principal
        // (si vienen null, no se tocan)
        public decimal? PrecioCompraDefault { get; set; }
        public decimal? PrecioVentaDefault { get; set; }
    }
}

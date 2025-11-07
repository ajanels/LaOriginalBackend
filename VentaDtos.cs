using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos.Ventas
{
    public class VentaItemCreateDto
    {
        [Required]
        public int PresentacionId { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "Cantidad debe ser mayor a 0")]
        public decimal Cantidad { get; set; }

        [Range(0.0, double.MaxValue, ErrorMessage = "Precio no puede ser negativo")]
        public decimal PrecioUnitario { get; set; }

        [Range(0.0, double.MaxValue, ErrorMessage = "Descuento no puede ser negativo")]
        public decimal DescuentoUnitario { get; set; } = 0m;

        [StringLength(200)]
        public string? Notas { get; set; }
    }

    public class VentaCreateDto
    {
        public int? ClienteId { get; set; }
        public int? FormaPagoId { get; set; }

        [StringLength(10)]
        public string? Serie { get; set; }

        [StringLength(40)]
        public string? Numero { get; set; }

        [StringLength(200)]
        public string? Observaciones { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe incluir al menos 1 ítem")]
        public List<VentaItemCreateDto> Items { get; set; } = new();
    }

    public class VentaItemDto
    {
        public int Id { get; set; }
        public int PresentacionId { get; set; }
        public string PresentacionNombre { get; set; } = null!;

        public string ProductoNombre { get; set; } = null!;

        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoUnitario { get; set; }
        public decimal TotalLinea { get; set; }
        public string? Notas { get; set; }
    }


    public class VentaDetailDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }

        public int? ClienteId { get; set; }
        public string? ClienteNombre { get; set; }

        public string? Serie { get; set; }
        public string? Numero { get; set; }
        public string? Observaciones { get; set; }

        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }

        public string Estado { get; set; } = null!;
        public bool Anulada { get; set; }

        public int? FormaPagoId { get; set; }
        public string? FormaPagoNombre { get; set; }

        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }

        public List<VentaItemDto> Items { get; set; } = new();
    }

    public class VentaListDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string? Serie { get; set; }
        public string? Numero { get; set; }
        public string? ClienteNombre { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = null!;
        public bool Anulada { get; set; }
        public string? FormaPagoNombre { get; set; }
    }

    public class VentaAnularDto
    {
        public string? Motivo { get; set; }
    }
}

// Dtos/ProductoPreciosDefaultUpdateDto.cs
using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos
{
    public class ProductoPreciosDefaultUpdateDto
    {
        [Required] public decimal PrecioCompraDefault { get; set; }
        [Required] public decimal PrecioVentaDefault { get; set; }
    }
}

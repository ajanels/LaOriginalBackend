using System.ComponentModel.DataAnnotations.Schema;

namespace LaOriginalBackend.Models
{
    public class Producto
    {
        public int Id { get; set; }  // PK

        public required string Nombre { get; set; }  // Obligatorio

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; } 

        public int Stock { get; set; }
    }
}

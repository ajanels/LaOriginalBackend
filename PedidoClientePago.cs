using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaOriginalBackend.Models
{
    public class PedidoClientePago
    {
        public int Id { get; set; }

        // FK al pedido
        public int PedidoClienteId { get; set; }
        public PedidoCliente? Pedido { get; set; }

        // Fecha de registro (UTC)
        public DateTime FechaUtc { get; set; } = DateTime.UtcNow;

        // Forma de pago usada
        public int FormaPagoId { get; set; }
        [StringLength(80)]
        public string FormaPagoNombre { get; set; } = null!; // snapshot del nombre

        // Monto POSITIVO (siempre)
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [StringLength(60)]
        public string? Referencia { get; set; }

        [StringLength(200)]
        public string? Notas { get; set; }

        public int? UsuarioId { get; set; }

        // === NUEVO: marca si es devolución (egreso en caja) ===
        public bool EsDevolucion { get; set; } = false;

        // (Opcional) vínculo con el pago original
        public int? PagoOriginalId { get; set; }
    }
}

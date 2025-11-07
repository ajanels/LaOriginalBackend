using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models
{
    public enum EstadoPedidoCliente
    {
        Borrador = 0,
        Confirmado = 1,
        EnPreparacion = 2,
        Listo = 3,
        Entregado = 4,
        Cancelado = 9
    }

    // 🧱 Tipo de valor (owned) para el diseño
    [Owned]
    public class DisenoPedidoCliente
    {
        public int Lienzos { get; set; }                 // 0 por defecto
        [StringLength(60)] public string? Color { get; set; }
        public bool Brich { get; set; }                  // false por defecto
        [StringLength(200)] public string? Otros { get; set; }
        public bool? Reportado { get; set; }             // null = no especificado
        [StringLength(200)] public string? Extra { get; set; }
    }

    [Index(nameof(ClienteId), nameof(Estado), nameof(FechaCreacionUtc))]
    public class PedidoCliente
    {
        public int Id { get; set; }
        public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;

        [Required] public int ClienteId { get; set; }
        [Required, StringLength(150)] public string ClienteNombre { get; set; } = null!;
        [StringLength(20)] public string? Telefono { get; set; }
        [StringLength(200)] public string? DireccionEntrega { get; set; }
        public DateTime? FechaEntregaCompromisoUtc { get; set; }

        [Required] public EstadoPedidoCliente Estado { get; set; } = EstadoPedidoCliente.Borrador;
        [Required] public TipoPedidoCliente Tipo { get; set; } = TipoPedidoCliente.Completo;

        // Texto legible (lista/búsqueda)
        [StringLength(400)] public string? Observaciones { get; set; }

        // Bloque estructurado (nunca lo dejamos en null: lo reseteamos)
        public DisenoPedidoCliente Diseno { get; set; } = new();

        [Precision(18, 2)] public decimal Subtotal { get; set; }
        [Precision(18, 2)] public decimal Descuento { get; set; }
        [Precision(18, 2)] public decimal Total { get; set; }

        public int? UsuarioId { get; set; }
        public int? VentaId { get; set; }

        public List<PedidoClienteDetalle> Detalles { get; set; } = new();
        public List<PedidoClienteReserva> Reservas { get; set; } = new();
    }

    [Index(nameof(PedidoClienteId), nameof(PresentacionId))]
    public class PedidoClienteDetalle
    {
        public int Id { get; set; }
        [Required] public int PedidoClienteId { get; set; }
        [Required] public int PresentacionId { get; set; }
        [StringLength(200)] public string? PresentacionNombre { get; set; }

        [Precision(18, 2)] public decimal Cantidad { get; set; }
        [Precision(18, 2)] public decimal PrecioUnitario { get; set; }
        [Precision(18, 2)] public decimal DescuentoUnitario { get; set; }
        [Precision(18, 2)] public decimal TotalLinea { get; set; }

        [StringLength(200)] public string? Notas { get; set; }

        public PedidoCliente? Pedido { get; set; }
    }

    [Index(nameof(PedidoClienteId), nameof(PresentacionId), IsUnique = true)]
    public class PedidoClienteReserva
    {
        public int Id { get; set; }
        [Required] public int PedidoClienteId { get; set; }
        [Required] public int PresentacionId { get; set; }
        [Precision(18, 2)] public decimal Cantidad { get; set; }

        public PedidoCliente? Pedido { get; set; }
    }
}

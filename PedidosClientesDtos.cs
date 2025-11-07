// Dtos/PedidosClientesDtos.cs
using System.ComponentModel.DataAnnotations;
using LaOriginalBackend.Models;

namespace LaOriginalBackend.Dtos
{
    public class PedidoClienteListDto
    {
        public int Id { get; set; }
        public DateTime FechaCreacionUtc { get; set; }
        public string Cliente { get; set; } = null!;
        public string? Descripcion { get; set; }
        public EstadoPedidoCliente Estado { get; set; }
        public TipoPedidoCliente Tipo { get; set; }
        public decimal Total { get; set; }
        public bool CuentaAlDia { get; set; } = true;
    }

    public class PedidoClienteDetalleDto
    {
        public int? Id { get; set; }
        [Required] public int PresentacionId { get; set; }
        [StringLength(200)] public string? PresentacionNombre { get; set; }
        [Range(0.01, 999999)] public decimal Cantidad { get; set; }
        [Range(0, 999999)] public decimal PrecioUnitario { get; set; }
        [Range(0, 999999)] public decimal DescuentoUnitario { get; set; }
        public decimal TotalLinea { get; set; }
        [StringLength(200)] public string? Notas { get; set; }
    }

    // Bloque estructurado (puede venir null en Create/Update)
    public class PedidoClienteDisenoDto
    {
        public int Lienzos { get; set; }
        [StringLength(60)] public string? Color { get; set; }
        public bool Brich { get; set; }
        [StringLength(200)] public string? Otros { get; set; }
        public bool? Reportado { get; set; }
        [StringLength(200)] public string? Extra { get; set; }
    }

    public class PedidoClienteCreateDto
    {
        [Required] public int ClienteId { get; set; }
        [Required, StringLength(150)] public string ClienteNombre { get; set; } = null!;
        [StringLength(20)] public string? Telefono { get; set; }
        [StringLength(200)] public string? DireccionEntrega { get; set; }
        public DateTime? FechaEntregaCompromisoUtc { get; set; }
        public EstadoPedidoCliente Estado { get; set; } = EstadoPedidoCliente.Borrador;
        [Required] public TipoPedidoCliente Tipo { get; set; } = TipoPedidoCliente.Completo;

        [StringLength(400)] public string? Observaciones { get; set; }   // opcional
        public PedidoClienteDisenoDto? Diseno { get; set; }               // opcional

        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }

        public List<PedidoClienteDetalleDto>? Detalles { get; set; } = new();
    }

    public class PedidoClienteUpdateDto : PedidoClienteCreateDto
    {
        [Required] public int Id { get; set; }
    }

    public class PedidoClientePagoDto
    {
        public int Id { get; set; }
        public DateTime FechaUtc { get; set; }
        public int FormaPagoId { get; set; }
        public string FormaPagoNombre { get; set; } = null!;
        public decimal Monto { get; set; }
        public string? Referencia { get; set; }
        public string? Notas { get; set; }

        public bool EsDevolucion { get; set; }
        public int? PagoOriginalId { get; set; }
    }

    public class PedidoClientePagoCreateDto
    {
        [Required] public int FormaPagoId { get; set; }
        [Range(0.01, 999999)] public decimal Monto { get; set; }
        [StringLength(60)] public string? Referencia { get; set; }
        [StringLength(200)] public string? Notas { get; set; }
        public DateTime? FechaUtc { get; set; }
    }

    public class PedidoClienteDevolucionCreateDto
    {
        [Required] public int FormaPagoId { get; set; }
        [Range(0.01, 999999)] public decimal Monto { get; set; }
        [StringLength(60)] public string? Referencia { get; set; }
        [StringLength(200)] public string? Notas { get; set; }
        public int? PagoOriginalId { get; set; }
        public DateTime? FechaUtc { get; set; }
    }

    public class PedidoClienteDetailDto : PedidoClienteUpdateDto
    {
        // agregado para el detalle (evita error en el front):
        public DateTime FechaCreacionUtc { get; set; }

        public decimal MontoPagado { get; set; }
        public decimal Saldo { get; set; }
        public List<PedidoClientePagoDto> Pagos { get; set; } = new();

        public decimal? TotalCobrado { get; set; }
        public decimal? TotalDevuelto { get; set; }
    }

    public class StockDisponibleDto
    {
        public int PresentacionId { get; set; }
        public decimal Stock { get; set; }
        public decimal Reservado { get; set; }
        public decimal Disponible { get; set; }
        public decimal? PrecioVenta { get; set; }
    }

    public class PedidoClienteEstadoDto
    {
        [Required] public EstadoPedidoCliente NuevoEstado { get; set; }
        [StringLength(200)] public string? Motivo { get; set; }
    }
}

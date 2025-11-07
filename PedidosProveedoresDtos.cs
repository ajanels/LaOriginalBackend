// Dtos/PedidosProveedoresDtos.cs
using System.ComponentModel.DataAnnotations;
using LaOriginalBackend.Models;

namespace LaOriginalBackend.Dtos
{
    // ===== Listado =====
    public class PedidoProveedorListItemDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string? Numero { get; set; }
        public int ProveedorId { get; set; }
        public string ProveedorNombre { get; set; } = null!;
        public EstadoPedidoProveedor Estado { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
    }

    // ===== Detalle =====
    public class PedidoProveedorDetalleDto
    {
        public int Id { get; set; }
        public int PresentacionId { get; set; }

        /// <summary>Nombre del producto al que pertenece la presentación.</summary>
        public string ProductoNombre { get; set; } = null!;   // NUEVO

        public string PresentacionNombre { get; set; } = null!;
        public string Unidad { get; set; } = null!;
        public string? SKU { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CantidadRecibida { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal TotalLinea { get; set; }
        public string? Notas { get; set; }
    }

    public class PedidoProveedorDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string? Numero { get; set; }
        public int ProveedorId { get; set; }
        public string ProveedorNombre { get; set; } = null!;
        public EstadoPedidoProveedor Estado { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public string? Observaciones { get; set; }

        /// <summary>Nombre de la forma de pago usada en la(s) recepción(es). Puede ser nulo si no hay recepciones.</summary>
        public string? FormaPago { get; set; }               // NUEVO

        public List<PedidoProveedorDetalleDto> Detalles { get; set; } = new();
    }

    // ===== Crear / Editar =====
    public class PedidoProveedorDetalleCreateDto
    {
        [Required] public int PresentacionId { get; set; }

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public string? Notas { get; set; }
    }

    public class PedidoProveedorCreateDto
    {
        [Required] public int ProveedorId { get; set; }
        public string? Numero { get; set; }
        public string? Observaciones { get; set; }

        [MinLength(1)]
        public List<PedidoProveedorDetalleCreateDto> Detalles { get; set; } = new();
    }

    public class PedidoProveedorUpdateDto
    {
        public string? Numero { get; set; }
        public string? Observaciones { get; set; }
    }

    // ===== Recepción =====
    public class PedidoRecepcionLineaDto
    {
        [Required] public int PedidoProveedorDetalleId { get; set; }

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Cantidad { get; set; }

        [Range(typeof(decimal), "0.00", "79228162514264337593543950335")]
        public decimal CostoUnitario { get; set; }

        public string? Notas { get; set; }
    }

    public class PedidoRecepcionCreateDto
    {
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public string? Numero { get; set; }            // correlativo de compra
        [Required] public int FormaPagoId { get; set; }
        public string? Referencia { get; set; }

        [MinLength(1)]
        public List<PedidoRecepcionLineaDto> Lineas { get; set; } = new();
    }
}

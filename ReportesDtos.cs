using System;
using System.Collections.Generic;

namespace LaOriginalBackend.Dtos.Reportes
{
    public class VentaDiariaDto
    {
        public string Fecha { get; set; } = null!; // yyyy-MM-dd
        public int Ventas { get; set; }
        public decimal Items { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
    }

    public class VentasPorUsuarioDto
    {
        public int? UsuarioId { get; set; }
        public string Usuario { get; set; } = null!;
        public int Ventas { get; set; }
        public decimal Total { get; set; }
        public decimal TicketPromedio { get; set; }
        public decimal? Utilidad { get; set; } // null si no se pidió
    }

    public class ClienteTopDto
    {
        public int ClienteId { get; set; }
        public string Cliente { get; set; } = null!;
        public int Compras { get; set; }
        public decimal Total { get; set; }
        public DateTime? UltimaCompra { get; set; }
    }

    public class VentasPorProductoDto
    {
        public int PresentacionId { get; set; }
        public string Producto { get; set; } = null!;
        public string Presentacion { get; set; } = null!;
        public string Categoria { get; set; } = null!;
        public decimal CantidadVendida { get; set; }
        public decimal Total { get; set; }
    }

    public class VentasPorCategoriaDto
    {
        public int? CategoriaId { get; set; }
        public string Categoria { get; set; } = "";
        public decimal CantidadVendida { get; set; }
        public decimal Total { get; set; }
    }

    public class GananciaPorProductoDto
    {
        public int PresentacionId { get; set; }
        public string Producto { get; set; } = null!;
        public string Presentacion { get; set; } = null!;
        public string Categoria { get; set; } = null!;
        public decimal Cantidad { get; set; }
        public decimal Venta { get; set; }
        public decimal Costo { get; set; }
        public decimal Utilidad { get; set; }
        public decimal MargenPct { get; set; } // NUEVO: margen % = Utilidad / Venta * 100
    }

    // Ventas por forma de pago
    public class VentasPorFormaPagoDto
    {
        public int? FormaPagoId { get; set; }
        public string FormaPago { get; set; } = "Sin forma de pago";
        public int Ventas { get; set; }
        public decimal Total { get; set; }
        public decimal TicketPromedio { get; set; }
    }

    // Compras por proveedor
    public class ComprasPorProveedorDto
    {
        public int ProveedorId { get; set; }
        public string Proveedor { get; set; } = null!;
        public int Documentos { get; set; }
        public decimal Total { get; set; }
        public DateTime? UltimaCompra { get; set; }
    }

    // Caja: ingresos/egresos diarios
    public class CajaDiariaDto
    {
        public string Fecha { get; set; } = null!; // yyyy-MM-dd
        public decimal Ingresos { get; set; }
        public decimal Egresos { get; set; }
        public decimal Neto => Ingresos - Egresos;
    }

    // ======================================================================
    // =====================  DTOs PARA PEDIDOS  ============================
    // ======================================================================

    // Cobros por forma de pago (agregado) — pedidos
    public class ReporteCobrosFormaPagoRowDto
    {
        public int FormaPagoId { get; set; }
        public string FormaPago { get; set; } = null!;
        public decimal Cobros { get; set; }
        public decimal Devoluciones { get; set; }
        public decimal Neto { get; set; }
        public int CantCobros { get; set; }
        public int CantDevoluciones { get; set; }
        public DateTime? FechaMin { get; set; }
        public DateTime? FechaMax { get; set; }
    }

    public class ReporteCobrosFormaPagoResponseDto
    {
        public DateTime? DesdeUtc { get; set; }
        public DateTime? HastaUtc { get; set; }
        public decimal TotalCobros { get; set; }
        public decimal TotalDevoluciones { get; set; }
        public decimal TotalNeto { get; set; }
        public List<ReporteCobrosFormaPagoRowDto> Filas { get; set; } = new();
    }

    // Detalle de cobros / devoluciones — pedidos
    public class ReporteCobrosDetalleDto
    {
        public int PagoId { get; set; }
        public int PedidoId { get; set; }
        public DateTime FechaUtc { get; set; }
        public bool EsDevolucion { get; set; }
        public decimal Monto { get; set; }
        public int FormaPagoId { get; set; }
        public string FormaPago { get; set; } = null!;
        public string? Referencia { get; set; }
        public string? Notas { get; set; }
        public int ClienteId { get; set; }
        public string Cliente { get; set; } = null!;
        public LaOriginalBackend.Models.EstadoPedidoCliente EstadoPedido { get; set; }
    }

    // Resumen de pedidos por estado
    public class ReportePedidosPorEstadoRowDto
    {
        public LaOriginalBackend.Models.EstadoPedidoCliente Estado { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
        public decimal PagadoNeto { get; set; }
        public decimal Saldo { get; set; }
    }

    // Top productos por pedidos
    public class ReporteTopProductoRowDto
    {
        public int PresentacionId { get; set; }
        public string? Presentacion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Importe { get; set; }
    }

    public class UsuariosPorRolDto
    {
        public int RolId { get; set; }
        public string Rol { get; set; } = "(Sin rol)";
        public int Total { get; set; }
        public int Activos { get; set; }
        public int Inactivos { get; set; }
        public int Suspendidos { get; set; }
    }

    public class AltasPorMesDto
    {
        public int Anio { get; set; }
        public int Mes { get; set; }                     // 1..12
        public int Cantidad { get; set; }
        public string Periodo => $"{Anio}-{Mes:00}";
    }

    public class CumplesPorMesDto
    {
        public int Mes { get; set; }                     // 1..12
        public int Cantidad { get; set; }
    }

    public class ReporteUsuariosResumenDto
    {
        public DateTime? DesdeUtc { get; set; }
        public DateTime? HastaUtc { get; set; }

        public int Total { get; set; }
        public int Activos { get; set; }
        public int Inactivos { get; set; }
        public int Suspendidos { get; set; }

        public List<UsuariosPorRolDto> PorRol { get; set; } = new();
        public List<AltasPorMesDto> AltasPorMes { get; set; } = new();
        public List<CumplesPorMesDto> CumplesPorMes { get; set; } = new();
    }

    // ===================== NUEVO DTO: Sesiones de caja cerradas =====================
    public class CajaSesionCerradaDto
    {
        public int AperturaId { get; set; }
        public string Codigo { get; set; } = "";
        public string? CajeroNombre { get; set; }
        public DateTime FechaAperturaUtc { get; set; }
        public DateTime FechaCierreUtc { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Egresos { get; set; }
        public decimal Neto { get; set; }                 // Inicial + Ingresos - Egresos
        public string CierreDia { get; set; } = "";       // yyyy-MM-dd (para agrupar en UI)
    }

    // ===================== NUEVO DTO: Resumen de ganancia ==========================
    public class GananciaResumenDto
    {
        public DateTime? DesdeUtc { get; set; }
        public DateTime? HastaUtc { get; set; }
        public decimal Venta { get; set; }
        public decimal Costo { get; set; }
        public decimal Utilidad { get; set; }
        public decimal MargenPct { get; set; }
    }
}

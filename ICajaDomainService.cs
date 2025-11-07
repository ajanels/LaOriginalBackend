// Services/ICajaDomainService.cs
using LaOriginalBackend.Dtos;
using LaOriginalBackend.Models;

namespace LaOriginalBackend.Services
{
    public interface ICajaDomainService
    {
        Task<CajaEstadoDto> EstadoAsync(CancellationToken ct = default);
        Task<CajaResumenDto> ResumenAsync(int? aperturaId = null, CancellationToken ct = default);

        /// <summary>
        /// Agrega un movimiento a la caja abierta.
        /// Lanza InvalidOperationException si no hay caja abierta.
        /// Lanza CajaFondosInsuficientesException si un egreso excede el saldo disponible.
        /// Usa la transacción existente si ya hay una activa.
        /// </summary>
        Task<CajaMovimiento> AddMovimientoEnCajaAbiertaAsync(
            CajaMovimientoCreateDto dto,
            int? usuarioId = null,
            CancellationToken ct = default);
    }
}

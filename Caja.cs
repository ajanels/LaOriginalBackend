using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models;

public class Caja
{
    public int Id { get; set; }

    public DateTime FechaAperturaUtc { get; set; }
    public DateTime? FechaCierreUtc { get; set; }

    [StringLength(20)]
    public string Estado { get; set; } = "Abierta"; // Abierta | Cerrada

    [Precision(18, 2)]
    public decimal SaldoApertura { get; set; }

    [Precision(18, 2)]
    public decimal? SaldoCierreContado { get; set; }

    [StringLength(200)]
    public string? ObservacionesApertura { get; set; }

    [StringLength(200)]
    public string? ObservacionesCierre { get; set; }

    public int? UsuarioAperturaId { get; set; }
    public int? UsuarioCierreId { get; set; }

    public ICollection<CajaMovimiento> Movimientos { get; set; } = new List<CajaMovimiento>();
    public ICollection<CajaArqueo> Arqueos { get; set; } = new List<CajaArqueo>();
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Models;

public class CajaArqueo
{
    public int Id { get; set; }

    public int CajaId { get; set; }
    [ForeignKey(nameof(CajaId))]
    public Caja Caja { get; set; } = null!;

    public DateTime FechaUtc { get; set; }

    [Precision(18, 2)]
    public decimal EfectivoContado { get; set; }

    [Precision(18, 2)]
    public decimal Diferencia { get; set; }

    [StringLength(200)]
    public string? Observaciones { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos;

public class PresentacionListDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = null!;
    public int UnidadMedidaId { get; set; }
    public string Unidad { get; set; } = null!;
    public decimal Factor { get; set; }
    public string? SKU { get; set; }
    public string? CodigoBarras { get; set; }
    public decimal? PrecioCompraDefault { get; set; }
    public decimal? PrecioVentaDefault { get; set; }
    public int? ColorId { get; set; }
    public string? Color { get; set; }
    public bool Activo { get; set; }
    public bool EsPrincipal { get; set; }
}

public class PresentacionCreateDto
{
    [Required] public int ProductoId { get; set; }
    [Required, StringLength(80)] public string Nombre { get; set; } = null!;
    [Required] public int UnidadMedidaId { get; set; }

    [Range(typeof(decimal), "0.0001", "9999999999")]
    public decimal Factor { get; set; } = 1m;

    [StringLength(60)] public string? SKU { get; set; }
    [StringLength(50)] public string? CodigoBarras { get; set; }

    [Range(typeof(decimal), "0", "9999999999")] public decimal? PrecioCompraDefault { get; set; }
    [Range(typeof(decimal), "0", "9999999999")] public decimal? PrecioVentaDefault { get; set; }

    public int? ColorId { get; set; }
    public bool Activo { get; set; } = true;
    public bool EsPrincipal { get; set; } = false;
}

public class PresentacionUpdateDto : PresentacionCreateDto
{
    [Required] public int Id { get; set; }
}

public class PresentacionToggleDto
{
    public bool Activo { get; set; }
}

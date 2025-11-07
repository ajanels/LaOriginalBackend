using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Dtos
{
    public class FormaPagoListDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public bool RequiereReferencia { get; set; }
        public bool AfectaCaja { get; set; }
        public bool AfectaBanco { get; set; }
        public bool EsCredito { get; set; }
    }

    public class FormaPagoCreateDto
    {
        [Required, StringLength(60)]
        public string Nombre { get; set; } = null!;
        [StringLength(120)]
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
        public bool RequiereReferencia { get; set; }
        public bool AfectaCaja { get; set; }
        public bool AfectaBanco { get; set; }
        public bool EsCredito { get; set; }
    }

    public class FormaPagoUpdateDto : FormaPagoCreateDto
    {
        [Required] public int Id { get; set; }
    }

    public class FormaPagoToggleDto
    {
        public bool Activo { get; set; }
    }
}

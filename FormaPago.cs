using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Models
{
    public class FormaPago
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        public string Nombre { get; set; } = null!;

        [StringLength(120)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        /// <summary>Si requiere un número de referencia (depósito/transferencia, etc.)</summary>
        public bool RequiereReferencia { get; set; }

        /// <summary>Si esta FP genera un egreso/ingreso directo de la caja (efectivo, depósito desde caja).</summary>
        public bool AfectaCaja { get; set; }

        /// <summary>Si esta FP afecta un módulo de banco (depósito/transferencia). Para futuro.</summary>
        public bool AfectaBanco { get; set; }

        /// <summary>Si representa financiamiento/crédito.</summary>
        public bool EsCredito { get; set; }
    }
}

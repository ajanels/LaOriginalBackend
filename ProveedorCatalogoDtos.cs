namespace LaOriginalBackend.Dtos
{
    public class ProveedorCatalogoItemDto
    {
        public int PresentacionId { get; set; }
        public int ProductoId { get; set; }
        public string? ProductoCodigo { get; set; }             // ✅ nuevo
        public string ProductoNombre { get; set; } = null!;
        public int? ProductoCategoriaId { get; set; }           // ✅ nuevo
        public string? ProductoCategoria { get; set; }          // ✅ nuevo

        public string PresentacionNombre { get; set; } = null!;
        public string Unidad { get; set; } = null!;
        public string? Color { get; set; }
        public string? SKU { get; set; }
        public string? CodigoBarras { get; set; }
        public string? CodigoProveedor { get; set; }

        public string? FotoUrl { get; set; }                    // ✅ nuevo
        public decimal? Disponible { get; set; }                // ✅ nuevo

        /// <summary>
        /// Precio sugerido que verá el front en el catálogo de COMPRAS:
        /// Orden de preferencia: PrecioUltimo / PrecioLista / PrecioCompraDefault (presentación) / PrecioCompraDefault (producto).
        /// </summary>
        public decimal? PrecioSugerido { get; set; }

        public bool Activo { get; set; }
    }

    public class ProveedorCatalogoCreateDto
    {
        public int PresentacionId { get; set; }
        public string? CodigoProveedor { get; set; }
        public decimal? PrecioLista { get; set; }
        public string? Notas { get; set; }
    }

    /// <summary>
    /// DTO para PATCH. Solo se actualiza lo que venga con valor.
    /// </summary>
    public class ProveedorCatalogoUpdateDto
    {
        public string? CodigoProveedor { get; set; }
        public decimal? PrecioLista { get; set; }
        public decimal? PrecioUltimo { get; set; }
        public bool? Activo { get; set; }
        public string? Notas { get; set; }
    }
}

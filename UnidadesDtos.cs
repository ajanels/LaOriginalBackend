namespace LaOriginalBackend.Dtos
{
    public class UnidadListDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Simbolo { get; set; } = "";
        public bool Activo { get; set; }
    }

    public class UnidadUpsertDto
    {
        public int? Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Simbolo { get; set; } = "";
        public bool Activo { get; set; } = true;
    }
}

namespace checklistWs.Models.Subcategorias
{
    public class ListasPreguntasSubcategorias
    {
        public string Nombre { get; set; }
        public DateTime? Fecha { get; set; }
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public bool? Borrado { get; set; }
        public string Notas { get; set; }
    }
}

namespace checklistWs.Models.ListaParaReporteListado
{
    public class ListaParaReporteListado
    {
        public string Curso { get; set; }
        public string Evaluacion { get; set; }
        public string Periodo { get; set; }
        public string Instructor { get; set; }
        public string Fecha { get; set; }
        public string nombreUsuario { get; set; }
        public string nombreSucursal { get; set; }
        public string idUsuario { get; set; }
        public Guid? Evento { get; set; }
        public Guid? idLista { get; set; }
        public Guid? idSucursal { get; set; }
    }
}

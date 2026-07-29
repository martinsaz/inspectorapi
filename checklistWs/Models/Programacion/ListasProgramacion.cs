namespace checklistWs.Models.Programacion
{
    public class ListasProgramacion
    {
        public Guid? id { get; set; }
        public Guid idEmpresa { get; set; }
        public Guid idPrograma { get; set; }
        public Guid idusuario { get; set; }
        public string Nombre { get; set; }
        public DateTime? fechaProgramacion { get; set; }
        public Guid? idLista { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }
        public string Usuario { get; set; }
        public string Lista { get; set; }
    }
}

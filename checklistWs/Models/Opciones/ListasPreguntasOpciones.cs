namespace checklistWs.Models.Opciones
{
    public class ListasPreguntasOpciones
    {
        public Guid? id { get; set; }
        public Guid idEmpresa { get; set; }
        public Guid idLista { get; set; }
        public string opcion { get; set; }
        public Guid? idPregunta { get; set; }
        public string Lista { get; set; }
        public string Pregunta { get; set; }
        public string tipoPregunta { get; set; }
    }
}

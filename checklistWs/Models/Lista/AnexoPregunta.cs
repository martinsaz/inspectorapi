namespace checklistWs.Models.Lista
{
    public class AnexoPregunta
    {
        public Guid id { get; set; }
        public string url { get; set; }
        public int tipo_anexo { get; set; }
        public DateTime? fecha { get; set; }
        public Guid? idListaRespuesta { get; set; }
    }
}

namespace checklistWs.Models.Mislistas
{
    public class Mislistas
    {

        public Guid? Id { get; set; }
        public string Lista { get; set; }
        public string FechaCreacion { get; set; }
        public string Notas { get; set; }
        public string Status { get; set; }
        public string latitud { get; set; }
        public string longitud { get; set; }
        public string Creador { get; set; }
        public string Preguntas { get; set; }
        public string Veces { get; set; }
      
    }
}

namespace checklistWs.Models.Lista
{
    public class DetalleEvaluacion
    {

        public Guid? id { get; set; }

        public string Evaluacion { get; set; }
        public string Usuario { get; set; }
        public string Pregunta { get; set; }

        public string Explicacion { get; set; }
        public string Tipo { get; set; }

        public decimal Valor { get; set; }

        public string Obligatorio { get; set; }
        public bool StatusLista { get; set; }
    }
}

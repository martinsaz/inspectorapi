namespace checklistWs.Models.Lista
{
	public class ListasRespuestasDetalle
	{
        public Guid? id { get; set; }
        public Guid? idPregunta { get; set; }
		public string Evaluacion { get; set; }
		public string Pregunta { get; set; }
		public string RespuestaOpciones { get; set; }
		public string Respuesta { get; set; }
		public string Tipo { get; set; }

		public string categoria { get; set; }

		public string subcategoria { get; set; }
        public string urlAnexo { get; set; }
        public string notas { get; set; }
    }
}

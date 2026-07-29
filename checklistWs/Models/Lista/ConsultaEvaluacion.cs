namespace checklistWs.Models.Lista
{

    public class ConsultaEvaluacion
    {

        public string Curso { get; set; }

        public string Evaluacion { get; set; }

		public string Lista { get; set; }
		public string Periodo { get; set; }
        public string Instructor { get; set; }
        public string Fecha { get; set; }
        public string nombreUsuario { get; set; }
        public string nombreSucursal { get; set; }
        public string idUsuario { get; set; }
        public string latitud { get; set; }
        public string longitud { get; set; }



        public Guid? Evento { get; set; }
    }
}

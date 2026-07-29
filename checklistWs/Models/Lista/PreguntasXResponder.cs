namespace checklistWs.Models.Lista
{
    public class PreguntasXResponder
    {
        public Guid? idLista { get; set; }
        public Guid? id { get; set; }
        public string pregunta { get; set; }
        public string explicacion { get; set; }
        public string tipo { get; set; }
        public string valor { get; set; }
        public bool obligatorio { get; set; }
        public string RespuestaCorrecta { get; set; }
        public Guid? idCategoria { get; set; }
        public Guid? idSubcategoria { get; set; }
        public string categoria { get; set; }
        public string subcategoria { get; set; }
        public string notas { get; set; }
    }
}

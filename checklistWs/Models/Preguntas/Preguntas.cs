namespace checklistWs.Models.Preguntas
{
    public class Pregunta
    {
        public Guid? id { get; set; }
        public Guid idEmpresa { get; set; }
        public Guid idLista { get; set; }
        public string pregunta { get; set; }
        public string Explicacion { get; set; }
        public decimal? Tipo { get; set; }
        public decimal? Valor { get; set; }
        public bool? Obligatorio { get; set; }
        public bool? Status { get; set; }
        public DateTime? fecha { get; set; }
        public string Lista { get; set; }
        public string RespuestaCorrecta { get; set; }
        public Guid? idCategoria { get; set; }

        public string Categoria { get; set; }
        public Guid? idSubcategoria { get; set; }

        public string Subcategoria { get; set; }
    }
}

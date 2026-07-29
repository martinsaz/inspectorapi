namespace checklistWs.Models.Lista
{
    public class GuardarInspeccionBl26Request
    {
        public Guid idEmpresa { get; set; }
        public Guid idLista { get; set; }
        public Guid idSucursal { get; set; }
        public Guid idUsuarioResponsable { get; set; }
        public Guid idAlumno { get; set; }
        public Guid? idActivo { get; set; }
        public Guid? idProgramacion { get; set; }
        public Guid? eventoLegacy { get; set; }
        public List<GuardarInspeccionBl26RespuestaItem> respuestas { get; set; } = new();
    }

    public class GuardarInspeccionBl26RespuestaItem
    {
        public Guid idPregunta { get; set; }
        public Guid? idPrograma { get; set; }
        public decimal? idTipoPregunta { get; set; }
        public string RespuestaValor { get; set; }
        public string Notas { get; set; }
        public string Explicacion { get; set; }
        public decimal? Valor { get; set; }
        public decimal? Calificacion { get; set; }
        public bool? obligatoria { get; set; }
        public string RespuestaCorrecta { get; set; }
        public string latitud { get; set; }
        public string longitud { get; set; }
        public string stamp { get; set; }
        public List<string> urlVideos { get; set; } = new();
        public List<string> urlFotos { get; set; } = new();
    }

    public class GuardarInspeccionBl26Response
    {
        public Guid idInspeccion { get; set; }
        public Guid? eventoLegacy { get; set; }
        public int respuestasGuardadas { get; set; }
    }
}

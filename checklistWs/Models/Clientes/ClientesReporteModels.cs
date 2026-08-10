namespace checklistWs.Models.Clientes
{
    public static class ClienteReporteIds
    {
        public const string RankingFrecuencia = "ranking-frecuencia";
        public const string AntiguedadRecencia = "antiguedad-recencia";
        public const string NuevosPeriodo = "nuevos-periodo";
        public const string ComparativoPeriodos = "comparativo-periodos";
        public const string RankingMonto = "ranking-monto";
        public const string PreferenciasCompra = "preferencias-compra";
    }

    public sealed class ClienteReporteConfiguracionResponse
    {
        public List<ClienteReporteDefinicionDto> Reportes { get; set; } = new List<ClienteReporteDefinicionDto>();
        public List<ClienteReporteClasificacionDto> Clasificaciones { get; set; } = new List<ClienteReporteClasificacionDto>();
    }

    public sealed class ClienteReporteDefinicionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = "ready";
        public string Motivo { get; set; } = string.Empty;
    }

    public sealed class ClienteReporteClasificacionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public sealed class ClienteReporteResponse
    {
        public string ReporteId { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = "ready";
        public string Mensaje { get; set; } = string.Empty;
        public string Rango { get; set; } = string.Empty;
        public List<ClienteReporteIndicadorDto> Indicadores { get; set; } = new List<ClienteReporteIndicadorDto>();
        public List<ClienteReporteColumnaDto> Columnas { get; set; } = new List<ClienteReporteColumnaDto>();
        public List<ClienteReporteFilaDto> Filas { get; set; } = new List<ClienteReporteFilaDto>();
    }

    public sealed class ClienteReporteIndicadorDto
    {
        public string Etiqueta { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }

    public sealed class ClienteReporteColumnaDto
    {
        public string Titulo { get; set; } = string.Empty;
        public string Tipo { get; set; } = "text";
    }

    public sealed class ClienteReporteFilaDto
    {
        public Guid IdCliente { get; set; }
        public string Principal { get; set; } = string.Empty;
        public string Secundario { get; set; } = string.Empty;
        public string AccionTexto { get; set; } = string.Empty;
        public string AccionUrl { get; set; } = string.Empty;
        public List<string> Celdas { get; set; } = new List<string>();
    }
}

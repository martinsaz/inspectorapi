namespace checklistWs.Models.Activos
{
    public class ActivoListadoDto
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string IdentityKey { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public Guid IdTipoActivo { get; set; }
        public string TipoActivo { get; set; } = string.Empty;
        public Guid IdEstadoOperativo { get; set; }
        public string EstadoOperativo { get; set; } = string.Empty;
        public Guid IdSucursal { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public Guid? IdMarca { get; set; }
        public string Marca { get; set; } = string.Empty;
        public Guid? IdProveedor { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string NumeroSerie { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int CantidadFotos { get; set; }
        public int CantidadVideos { get; set; }
        public int CantidadDocumentos { get; set; }
        public bool Activo { get; set; }
        public DateTime? FechaArchivado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }

    public class ActivoDetalleDto : ActivoListadoDto
    {
        public List<ActivoMultimediaDto> Multimedia { get; set; } = new List<ActivoMultimediaDto>();
    }

    public class ActivoGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public Guid IdTipoActivo { get; set; }
        public Guid IdEstadoOperativo { get; set; }
        public Guid IdSucursal { get; set; }
        public Guid IdMarca { get; set; }
        public Guid IdProveedor { get; set; }
        public string Tag { get; set; } = string.Empty;
        public string NumeroSerie { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public List<ActivoMultimediaGuardarRequest> Multimedia { get; set; } = new List<ActivoMultimediaGuardarRequest>();
    }

    public class TipoActivoDto
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }

    public class MarcaActivoDto : TipoActivoDto
    {
    }

    public class ProveedorActivoDto : TipoActivoDto
    {
    }

    public class EstadoOperativoDto
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool PermiteOperacion { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }

    public class EstadoOperativoGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool PermiteOperacion { get; set; }
        public int? Orden { get; set; }
    }

    public class TipoActivoGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }

    public class MarcaActivoGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }

    public class ProveedorActivoGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }

    public class CatalogoActivoDto
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public Guid? RelacionId { get; set; }
    }

    public class ActivoMultimediaDto
    {
        public Guid Id { get; set; }
        public Guid IdActivo { get; set; }
        public string TipoMultimedia { get; set; } = string.Empty;
        public bool Foto { get; set; }
        public bool Video { get; set; }
        public bool Documento { get; set; }
        public string NombreOriginal { get; set; } = string.Empty;
        public string NombreAlmacenado { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string UrlFirebase { get; set; } = string.Empty;
        public long PesoBytes { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }

    public class ActivoMultimediaGuardarRequest
    {
        public Guid? Id { get; set; }
        public string TipoMultimedia { get; set; } = string.Empty;
        public string NombreOriginal { get; set; } = string.Empty;
        public string NombreAlmacenado { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string UrlFirebase { get; set; } = string.Empty;
        public long PesoBytes { get; set; }
        public int Orden { get; set; }
        public string TemporalToken { get; set; } = string.Empty;
    }

    public class ActivoOperacionResponse
    {
        public string Mensaje { get; set; } = string.Empty;
    }

    public class ActivoMultimediaTemporalResponse : ActivoOperacionResponse
    {
        public ActivoMultimediaTemporalDto? Archivo { get; set; }
    }

    public class ActivoMultimediaTemporalDto
    {
        public string TemporalToken { get; set; } = string.Empty;
        public string TipoMultimedia { get; set; } = string.Empty;
        public string NombreOriginal { get; set; } = string.Empty;
        public string NombreAlmacenado { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string UrlFirebase { get; set; } = string.Empty;
        public long PesoBytes { get; set; }
    }

    public class ActivoMultimediaTemporalCleanupRequest
    {
        public List<string> Tokens { get; set; } = new List<string>();
    }
}

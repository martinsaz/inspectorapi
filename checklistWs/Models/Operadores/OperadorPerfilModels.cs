namespace checklistWs.Models.Operadores
{
    public class OperadorListadoDto
    {
        public Guid IdOperador { get; set; }
        public Guid IdEmpresa { get; set; }
        public string IdFirebase { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Sucursales { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public byte Estatus { get; set; }
        public bool? CorreoVerificado { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaAlta { get; set; }
        public DateTime? FechaSuspension { get; set; }
        public string VersionRow { get; set; } = string.Empty;
        public List<OperadorSucursalDto> SucursalesDetalle { get; set; } = new();
    }

    public class OperadorDetalleDto
    {
        public Guid IdOperador { get; set; }
        public Guid IdEmpresa { get; set; }
        public string IdFirebase { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public byte Estatus { get; set; }
        public bool? CorreoVerificado { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaAlta { get; set; }
        public DateTime? FechaSuspension { get; set; }
        public string VersionRow { get; set; } = string.Empty;
        public List<OperadorSucursalDto> Sucursales { get; set; } = new();
        public List<OperadorSucursalDto> SucursalesDetalle => Sucursales;
    }

    public class OperadorSucursalDto
    {
        public Guid IdSucursal { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class OperadorAccesoDto
    {
        public bool TieneAcceso { get; set; }
        public bool OperadorActivo { get; set; }
        public bool CuentaActiva { get; set; }
        public Guid IdOperador { get; set; }
        public Guid IdEmpresa { get; set; }
        public string IdFirebase { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string VersionRow { get; set; } = string.Empty;
        public List<OperadorSucursalDto> Sucursales { get; set; } = new();
    }

    public class CrearOperadorRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public List<Guid> Sucursales { get; set; } = new();
        public string CorreoActor { get; set; } = string.Empty;
    }

    public class VincularOperadorExistenteRequest
    {
        public string Correo { get; set; } = string.Empty;
        public List<Guid> Sucursales { get; set; } = new();
        public string CorreoActor { get; set; } = string.Empty;
    }

    public class ActualizarOperadorRequest
    {
        public Guid IdOperador { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public List<Guid> Sucursales { get; set; } = new();
        public string VersionRow { get; set; } = string.Empty;
        public string CorreoActor { get; set; } = string.Empty;
    }

    public class EstadoOperadorRequest
    {
        public Guid IdOperador { get; set; }
        public string VersionRow { get; set; } = string.Empty;
        public string CorreoActor { get; set; } = string.Empty;
    }

    public class RecuperacionOperadorRequest
    {
        public Guid IdOperador { get; set; }
    }

    public class OperadorOperacionResponse
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Advertencia { get; set; } = string.Empty;
        public string VersionRow { get; set; } = string.Empty;
    }

    public class OperadorIdentidadDualCandidatoDto
    {
        public Guid IdUsuario { get; set; }
        public Guid IdEmpresa { get; set; }
        public string IdFirebase { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public bool UsuarioActivo { get; set; }
        public bool YaEsOperador { get; set; }
        public Guid IdOperador { get; set; }
        public bool IdentidadValida { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}

namespace checklistWs.Models.Clientes
{
    public static class ClienteTipos
    {
        public const byte Particular = 1;
        public const byte Empresa = 2;
    }

    public sealed class ClienteListadoResponse
    {
        public ClienteResumenDto Resumen { get; set; } = new ClienteResumenDto();
        public List<ClienteListadoItemDto> Items { get; set; } = new List<ClienteListadoItemDto>();
    }

    public sealed class ClienteResumenDto
    {
        public int Total { get; set; }
        public int Particulares { get; set; }
        public int Empresas { get; set; }
        public int ConTelefono { get; set; }
        public int ConCorreo { get; set; }
    }

    public sealed class ClienteListadoItemDto
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdentityKey { get; set; }
        public byte TipoCliente { get; set; }
        public string TipoClienteNombre { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public decimal Descuento { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public DateTime? FechaArchivado { get; set; }
    }

    public sealed class ClienteCatalogoItemDto
    {
        public int Id { get; set; }
        public string Clave { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public sealed class ClienteAvanzadoDto
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdentityKey { get; set; }
        public byte TipoCliente { get; set; }
        public string TipoClienteNombre { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string TelefonoFijo { get; set; } = string.Empty;
        public string FechaNacimiento { get; set; } = string.Empty;
        public string Cbarras { get; set; } = string.Empty;
        public string Calle { get; set; } = string.Empty;
        public string NumeroExt { get; set; } = string.Empty;
        public string NumeroInt { get; set; } = string.Empty;
        public string Colonia { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string CodigoPostal { get; set; } = string.Empty;
        public string Rfc { get; set; } = string.Empty;
        public string RegimenFiscal { get; set; } = string.Empty;
        public string EntreCalles { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string NombreAval { get; set; } = string.Empty;
        public string DireccionAval { get; set; } = string.Empty;
        public decimal LimiteCredito { get; set; }
        public int PlazoDias { get; set; }
        public decimal Descuento { get; set; }
        public int Pagos { get; set; }
        public decimal Interes { get; set; }
        public string Observaciones { get; set; } = string.Empty;
        public int IdNivel { get; set; } = 1;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public DateTime? FechaArchivado { get; set; }
    }

    public sealed class ClienteOperacionResponse
    {
        public string Mensaje { get; set; } = string.Empty;
        public Guid? IdCliente { get; set; }
        public bool RequiereConfirmacionDuplicados { get; set; }
        public List<ClienteDuplicadoItemDto> Coincidencias { get; set; } = new List<ClienteDuplicadoItemDto>();
    }

    public sealed class ClienteGuardarRequest
    {
        public Guid? Id { get; set; }
        public byte TipoCliente { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public bool OmitirAdvertenciaDuplicados { get; set; }
    }

    public sealed class ClienteAvanzadoGuardarRequest
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string TelefonoFijo { get; set; } = string.Empty;
        public string FechaNacimiento { get; set; } = string.Empty;
        public string Cbarras { get; set; } = string.Empty;
        public string Calle { get; set; } = string.Empty;
        public string NumeroExt { get; set; } = string.Empty;
        public string NumeroInt { get; set; } = string.Empty;
        public string Colonia { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string CodigoPostal { get; set; } = string.Empty;
        public string Rfc { get; set; } = string.Empty;
        public string RegimenFiscal { get; set; } = string.Empty;
        public string EntreCalles { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string NombreAval { get; set; } = string.Empty;
        public string DireccionAval { get; set; } = string.Empty;
        public decimal LimiteCredito { get; set; }
        public int PlazoDias { get; set; }
        public decimal Descuento { get; set; }
        public int Pagos { get; set; }
        public decimal Interes { get; set; }
        public string Observaciones { get; set; } = string.Empty;
        public int IdNivel { get; set; } = 1;
    }

    public sealed class ClienteDuplicadosRequest
    {
        public Guid? IdCliente { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
    }

    public sealed class ClienteDuplicadosResponse
    {
        public bool HayCoincidencias { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public List<ClienteDuplicadoItemDto> Coincidencias { get; set; } = new List<ClienteDuplicadoItemDto>();
    }

    public sealed class ClienteDuplicadoItemDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public string TipoClienteNombre { get; set; } = string.Empty;
        public string CoincidenciaEn { get; set; } = string.Empty;
    }

    public sealed class ClienteNotaGuardarRequest
    {
        public Guid IdCliente { get; set; }
        public string Texto { get; set; } = string.Empty;
        public bool EsTarea { get; set; }
        public string FechaTarea { get; set; } = string.Empty;
        public string HoraTarea { get; set; } = string.Empty;
    }

    public sealed class ClienteCompletarTareaRequest
    {
        public Guid IdNota { get; set; }
        public bool Completada { get; set; }
    }

    public sealed class ClienteNotaItemDto
    {
        public Guid Id { get; set; }
        public Guid IdCliente { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdentityKey { get; set; }
        public string Texto { get; set; } = string.Empty;
        public bool EsTarea { get; set; }
        public string FechaTarea { get; set; } = string.Empty;
        public string HoraTarea { get; set; } = string.Empty;
        public bool Completada { get; set; }
        public string FechaCompletada { get; set; } = string.Empty;
        public string FechaCreacion { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}

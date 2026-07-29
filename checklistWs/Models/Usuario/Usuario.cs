namespace checklistWs.Models.Usuario
{
    public class Usuarios
    {
        public Guid? Id { get; set; }
        public string Nombre { get; set; }
		//public Guid? ApellidoPaterno { get; set; }
		//public Guid? ApellidoMaterno { get; set; }
		public string APaterno { get; set; }
		
		public string AMaterno { get; set; }
		public DateTime? FechaNacimiento { get; set; }
        public string Numero { get; set; }
        public string TelefonoMovil { get; set; }
        public string TelefonoFijo { get; set; }
        public string CorreoInstitucional { get; set; }
        public string CorreoPersonal { get; set; }
        public Guid? IdSucursal { get; set; }
        public Guid? IdDepartamento { get; set; }
        public Guid? IdPuesto { get; set; }
        public bool? Estado { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public bool? Estatus { get; set; }
        public string Notas { get; set; }
        public bool? borrado { get; set; }
        public DateTime? FechaAlta { get; set; }
        public string FotoLink { get; set; }
        public string IdFirebase { get; set; }
        public Guid? IdEmpresa { get; set; }
        public Guid? idRol { get; set; }
       // public string NombreRol { get; set; }
    }
}

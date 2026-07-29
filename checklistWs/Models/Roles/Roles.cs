namespace checklistWs.Models.Roles
{
	public class Roles
	{
		public Guid? id { get; set; }
		public Guid? idEmpresa { get; set; }
		public string NombreRol { get; set; }
		public string Permisos { get; set; }
	}
}

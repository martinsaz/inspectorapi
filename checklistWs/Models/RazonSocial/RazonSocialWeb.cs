namespace checklistWs.Models.RazonSocial
{
	public class RazonSocialWeb
	{
		public Guid? IdEmpresa { get; set; }
		public string Nombre { get; set; }
		public string Representante { get; set; }
		public string RFC { get; set; }
		public string Direccion { get; set; }
		public string Colonia { get; set; }
		public string CodigoPostal { get; set; }
		public string Ciudad { get; set; }
		public string Estado { get; set; }
		public string Pais { get; set; }
		public string Telefono { get; set; }
		public string Regimen1 { get; set; }
		public string NombreRegimen1 { get; set; }

		public DateTime? Fecha { get; set; }
		public string IMGFIREBASE { get; set; }
		public Guid? Id { get; set; }
		public bool? borrado { get; set; }
		public string Notas { get; set; }
	}
}

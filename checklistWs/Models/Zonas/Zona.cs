namespace checklistWs.Models.Zonas
{
	public class Zona
	{
		public Guid? Id { get; set; }
		public Guid? IdEmpresa { get; set; }
		public string Nombre { get; set; }
		public string Notas { get; set; }
		public DateTime? Fecha { get; set; }
		public bool? borrado { get; set; }

	}
}

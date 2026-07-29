namespace checklistWs.Models.Sucursal
{
    public class Sucursales
    {
        public Guid? Id { get; set; }
        public Guid? IdEmpresa { get; set; }
        public string Nombre { get; set; }
        public string Direccion { get; set; }
        public string Ciudad { get; set; }
        public string Telefono { get; set; }
        public string Numero { get; set; }
        public string Correo { get; set; }
        public string Pais { get; set; }
        public Guid? IdTitular { get; set; }
        public Guid? IdRazonSocial { get; set; }
        public Guid? IdZona { get; set; }
        public Guid? IdSucursalTipo { get; set; }
        public string Notas { get; set; }
        public bool? borrado { get; set; }
        public DateTime? Fecha { get; set; }
        public string LinkImagen { get; set; }

    }
}

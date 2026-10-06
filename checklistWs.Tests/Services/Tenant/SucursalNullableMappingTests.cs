using System.Data;
using System.Reflection;
using checklistWs.Controllers.Sucursal;
using checklistWs.Models.Sucursal;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class SucursalNullableMappingTests
    {
        [Fact]
        public void ObtenerSucursales_MapsNullableNotasWithoutInventingDefault()
        {
            DataTable table = BuildSucursalTable();
            table.Rows.Add(
                Guid.NewGuid(), Guid.NewGuid(), "Sucursal QA",
                "Calle QA", "Ciudad QA", "5550000000", "10", "qa@example.test", "MX",
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), false, DateTime.UtcNow,
                DBNull.Value, "https://example.test/sucursal.png");

            using DataTableReader reader = table.CreateDataReader();
            Assert.True(reader.Read());

            MethodInfo mapper = typeof(SucursalController).GetMethod(
                "MapSucursal",
                BindingFlags.Static | BindingFlags.NonPublic)!;
            Sucursales result = Assert.IsType<Sucursales>(mapper.Invoke(null, new object[] { reader }));

            Assert.Null(result.Notas);
            Assert.Equal("Calle QA", result.Direccion);
            Assert.Equal("https://example.test/sucursal.png", result.LinkImagen);
        }

        private static DataTable BuildSucursalTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("IdEmpresa", typeof(Guid));
            table.Columns.Add("Nombre", typeof(string));
            table.Columns.Add("Direccion", typeof(string));
            table.Columns.Add("Ciudad", typeof(string));
            table.Columns.Add("Telefono", typeof(string));
            table.Columns.Add("Numero", typeof(string));
            table.Columns.Add("Correo", typeof(string));
            table.Columns.Add("Pais", typeof(string));
            table.Columns.Add("IdTitular", typeof(Guid));
            table.Columns.Add("IdRazonSocial", typeof(Guid));
            table.Columns.Add("IdZona", typeof(Guid));
            table.Columns.Add("IdSucursaltipo", typeof(Guid));
            table.Columns.Add("borrado", typeof(bool));
            table.Columns.Add("Fecha", typeof(DateTime));
            table.Columns.Add("Notas", typeof(string));
            table.Columns.Add("LinkImagen", typeof(string));
            return table;
        }
    }
}

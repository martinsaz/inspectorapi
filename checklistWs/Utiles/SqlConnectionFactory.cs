using System.Data.SqlClient;

namespace checklistWs.Utiles
{
    public class SqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CadenaConexionSQLServer");
            //ConfigurationManager.ConnectionStrings["conLogins"].ConnectionString
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}

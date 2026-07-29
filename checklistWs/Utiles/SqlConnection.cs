using Microsoft.Extensions.Configuration;
using System;

namespace checklistWs.Utiles
{
    /*  public class SqlConnectionFactory
      {
          private readonly string _connectionString = null;

          public SqlConnectionFactory(IConfiguration configuration)
          {
              _connectionString = configuration.GetConnectionString("CadenaConexionSQLServer");
          }

          public static string ObtenerCadenaConexion(IConfiguration configuration)
          {
              try
              {
                  string cadena = configuration.GetConnectionString("CadenaConexionSQLServer");
                  return cadena;
              }
              catch (Exception ex)
              {
                  Console.WriteLine($"Error al obtener cadena conexión: {ex.Message}");
                  return null;
              }
          }
      }*/
}

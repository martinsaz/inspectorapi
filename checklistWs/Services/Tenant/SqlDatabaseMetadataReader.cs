using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public sealed class SqlDatabaseMetadataReader : IDatabaseMetadataReader
    {
        private readonly ITenantSqlConnectionFactory _connectionFactory;

        public SqlDatabaseMetadataReader(ITenantSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<DatabaseMetadata> ReadMetadataAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
                await connection.OpenAsync(cancellationToken);

                using SqlCommand command = new SqlCommand(@"
SELECT
    CAST(SERVERPROPERTY('ServerName') AS nvarchar(256)) AS ServerName,
    CAST(SERVERPROPERTY('ComputerNamePhysicalNetBIOS') AS nvarchar(256)) AS PhysicalServerName,
    ISNULL(CAST(SERVERPROPERTY('InstanceName') AS nvarchar(256)), N'') AS InstanceName,
    DB_NAME() AS DatabaseName,
    DB_ID() AS DatabaseId,
    CONVERT(nvarchar(36), drs.database_guid) AS DatabaseGuid
FROM sys.database_recovery_status drs
WHERE drs.database_id = DB_ID();", connection);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode.DatabaseIdentityUnverifiable);
                }

                return new DatabaseMetadata
                {
                    ServerName = ReadString(reader, "ServerName"),
                    PhysicalServerName = ReadString(reader, "PhysicalServerName"),
                    InstanceName = ReadString(reader, "InstanceName"),
                    DatabaseName = ReadString(reader, "DatabaseName"),
                    DatabaseId = ReadInt(reader, "DatabaseId"),
                    DatabaseGuid = ReadString(reader, "DatabaseGuid")
                };
            }
            catch (DatabaseIdentityResolutionException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                throw new DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode.DatabaseIdentityUnavailable, ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode.DatabaseIdentityUnavailable, ex);
            }
        }

        private static string ReadString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal).Trim();
        }

        private static int ReadInt(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }
    }
}

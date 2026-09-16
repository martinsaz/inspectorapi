using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public sealed class SchemaProvisionExecutor : ISchemaProvisionExecutor
    {
        private readonly ITenantSqlConnectionFactory _connectionFactory;
        private readonly ISchemaContractSqlGenerator _sqlGenerator;

        public SchemaProvisionExecutor(
            ITenantSqlConnectionFactory connectionFactory,
            ISchemaContractSqlGenerator sqlGenerator)
        {
            _connectionFactory = connectionFactory;
            _sqlGenerator = sqlGenerator;
        }

        public async Task<SchemaProvisionExecutionResult> ProvisionAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            SchemaContract contract,
            SchemaManifest manifest,
            CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                foreach (string sql in _sqlGenerator.CreateProvisioningCommands(contract))
                {
                    using SqlCommand command = new SqlCommand(sql, connection, transaction);
                    command.CommandTimeout = 60;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                SchemaContractValidationResult validation = await SchemaContractPhysicalValidator.ValidateOpenConnectionAsync(
                    connection,
                    contract,
                    cancellationToken,
                    transaction);

                if (!validation.IsValid)
                {
                    transaction.Rollback();
                    return new SchemaProvisionExecutionResult
                    {
                        Succeeded = false,
                        ReasonCode = "PROVISION_VALIDATION_FAILED",
                        Discrepancies = validation.Discrepancies.ToArray()
                    };
                }

                transaction.Commit();
                return new SchemaProvisionExecutionResult
                {
                    Succeeded = true,
                    ReasonCode = "PROVISION_VALIDATED",
                    TablesCreated = contract.Tables.Count,
                    ColumnsCreated = contract.Tables.Sum(table => table.Columns.Count),
                    IndexesCreated = contract.Tables.Sum(table => table.Indexes.Count),
                    ForeignKeysCreated = contract.Tables.Sum(table => table.ForeignKeys.Count),
                    ChecksCreated = contract.Tables.Sum(table => table.CheckConstraints.Count),
                    Evidence = new[] { $"Schema {contract.Scope} V{contract.ContractVersion} creado y validado antes de confirmar State." }
                };
            }
            catch
            {
                try
                {
                    transaction.Rollback();
                }
                catch (InvalidOperationException)
                {
                }

                throw;
            }
        }
    }
}

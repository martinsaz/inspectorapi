using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public sealed class SchemaMigrationSqlExecutor : ISchemaMigrationSqlExecutor
    {
        private readonly ITenantSqlConnectionFactory _connectionFactory;
        private readonly ISchemaDriftValidator? _driftValidator;

        public SchemaMigrationSqlExecutor(ITenantSqlConnectionFactory connectionFactory)
            : this(connectionFactory, null)
        {
        }

        public SchemaMigrationSqlExecutor(ITenantSqlConnectionFactory connectionFactory, ISchemaDriftValidator? driftValidator)
        {
            _connectionFactory = connectionFactory;
            _driftValidator = driftValidator;
        }

        public async Task ExecuteAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            SchemaMigrationDefinition migration,
            Func<SchemaMigrationDefinition, CancellationToken, Task<SchemaContractValidationResult>> validateTargetAsync,
            CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand xactAbort = new SqlCommand("SET XACT_ABORT ON;", connection);
            await xactAbort.ExecuteNonQueryAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                foreach (string precondition in migration.Preconditions)
                {
                    using SqlCommand command = new SqlCommand(precondition, connection, transaction);
                    command.CommandTimeout = (int)Math.Ceiling(migration.Timeout.TotalSeconds);
                    object? result = await command.ExecuteScalarAsync(cancellationToken);
                    if (result != null && result != DBNull.Value && Convert.ToInt32(result) == 0)
                    {
                        throw new InvalidOperationException("MIGRATION_PRECONDITION_FAILED");
                    }
                }

                using (SqlCommand command = new SqlCommand(migration.UpSql, connection, transaction))
                {
                    command.CommandTimeout = (int)Math.Ceiling(migration.Timeout.TotalSeconds);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                IReadOnlyCollection<string> discrepancies;
                if (_driftValidator != null)
                {
                    SchemaDriftReport drift = await _driftValidator.ValidateAsync(
                        connection,
                        transaction,
                        identity,
                        migration.Scope,
                        migration.ToVersion,
                        migration.TargetManifestHash,
                        cancellationToken);
                    discrepancies = drift.Items.Select(item => $"{item.DifferenceType} {item.ObjectName}").ToArray();
                }
                else
                {
                    SchemaContractValidationResult validation = await SchemaContractPhysicalValidator.ValidateOpenConnectionAsync(
                        connection,
                        migration.TargetContract,
                        cancellationToken,
                        transaction);
                    discrepancies = validation.Discrepancies;
                }

                if (discrepancies.Count > 0)
                {
                    throw new SchemaMigrationValidationException(discrepancies);
                }

                transaction.Commit();
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

    public sealed class SchemaMigrationValidationException : Exception
    {
        public SchemaMigrationValidationException(IReadOnlyCollection<string> discrepancies)
            : base("MIGRATION_TARGET_VALIDATION_FAILED")
        {
            Discrepancies = discrepancies;
        }

        public IReadOnlyCollection<string> Discrepancies { get; }
    }
}

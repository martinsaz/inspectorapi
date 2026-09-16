using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace checklistWs.Services.Tenant
{
    public sealed class SchemaVersionRepository : ISchemaVersionRepository
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> RequiredColumns =
            new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["CheckAppSchemaState"] = new[]
                {
                    "DatabaseIdentityKey", "Scope", "CurrentVersion", "BaselineId", "BaselineVersion",
                    "ManifestHash", "LastValidatedAtUtc", "LastMigratedAtUtc", "LastResult",
                    "CreatedAtUtc", "UpdatedAtUtc"
                },
                ["CheckAppSchemaHistory"] = new[]
                {
                    "HistoryId", "DatabaseIdentityKey", "Scope", "EventType", "FromVersion", "ToVersion",
                    "OperationId", "StartedAtUtc", "CompletedAtUtc", "Result", "Details", "CreatedAtUtc"
                },
                ["CheckAppSchemaAttempts"] = new[]
                {
                    "AttemptId", "DatabaseIdentityKey", "Scope", "OperationType", "FromVersion", "ToVersion",
                    "StartedAtUtc", "CompletedAtUtc", "Result", "ReasonCode", "Error", "CreatedAtUtc", "UpdatedAtUtc"
                }
            };

        private readonly ITenantSqlConnectionFactory _connectionFactory;

        public SchemaVersionRepository(ITenantSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<SchemaControlInfrastructureResult> EnsureSchemaControlInfrastructureAsync(
            TenantDatabaseDescriptor descriptor,
            CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);

            List<string> created = new();
            List<string> verified = new();
            List<string> conflicts = new();

            foreach (string tableName in RequiredColumns.Keys)
            {
                bool exists = await TableExistsAsync(connection, tableName, cancellationToken);
                if (!exists)
                {
                    await ExecuteNonQueryAsync(connection, GetCreateTableSql(tableName), cancellationToken);
                    created.Add($"dbo.{tableName}");
                }

                IReadOnlyCollection<string> missingColumns = await GetMissingColumnsAsync(connection, tableName, RequiredColumns[tableName], cancellationToken);
                if (missingColumns.Count > 0)
                {
                    conflicts.Add($"dbo.{tableName}: columnas faltantes {string.Join(',', missingColumns)}");
                }
                else
                {
                    verified.Add($"dbo.{tableName}");
                }
            }

            if (conflicts.Count > 0)
            {
                return new SchemaControlInfrastructureResult
                {
                    Status = SchemaControlInfrastructureStatus.Conflict,
                    CreatedObjects = created.ToArray(),
                    VerifiedObjects = verified.ToArray(),
                    Conflicts = conflicts.ToArray()
                };
            }

            return new SchemaControlInfrastructureResult
            {
                Status = SchemaControlInfrastructureStatus.Ready,
                CreatedObjects = created.ToArray(),
                VerifiedObjects = verified.ToArray()
            };
        }

        public async Task<SchemaControlState?> GetStateAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);

            if (!await TableExistsAsync(connection, "CheckAppSchemaState", cancellationToken))
            {
                return null;
            }

            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    DatabaseIdentityKey,
    Scope,
    CurrentVersion,
    BaselineId,
    BaselineVersion,
    ManifestHash,
    LastValidatedAtUtc,
    LastMigratedAtUtc,
    LastResult,
    CreatedAtUtc,
    UpdatedAtUtc
FROM dbo.CheckAppSchemaState
WHERE DatabaseIdentityKey = @DatabaseIdentityKey
  AND Scope = @Scope;", connection);
            AddIdentityScopeParameters(command, identity, scope);

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new SchemaControlState
            {
                DatabaseIdentityKey = ReadString(reader, "DatabaseIdentityKey"),
                Scope = ReadString(reader, "Scope"),
                CurrentVersion = ReadNullableInt(reader, "CurrentVersion"),
                BaselineId = ReadNullableString(reader, "BaselineId"),
                BaselineVersion = ReadNullableInt(reader, "BaselineVersion"),
                ManifestHash = ReadNullableString(reader, "ManifestHash"),
                LastValidatedAtUtc = ReadNullableDateTime(reader, "LastValidatedAtUtc"),
                LastMigratedAtUtc = ReadNullableDateTime(reader, "LastMigratedAtUtc"),
                LastResult = ReadString(reader, "LastResult"),
                CreatedAtUtc = ReadDateTime(reader, "CreatedAtUtc"),
                UpdatedAtUtc = ReadDateTime(reader, "UpdatedAtUtc")
            };
        }

        public async Task<IReadOnlyCollection<SchemaControlHistory>> GetHistoryAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);

            if (!await TableExistsAsync(connection, "CheckAppSchemaHistory", cancellationToken))
            {
                return Array.Empty<SchemaControlHistory>();
            }

            using SqlCommand command = new SqlCommand(@"
SELECT HistoryId, DatabaseIdentityKey, Scope, EventType, FromVersion, ToVersion, OperationId,
       StartedAtUtc, CompletedAtUtc, Result, Details
FROM dbo.CheckAppSchemaHistory
WHERE DatabaseIdentityKey = @DatabaseIdentityKey
  AND Scope = @Scope
ORDER BY StartedAtUtc DESC, CreatedAtUtc DESC;", connection);
            AddIdentityScopeParameters(command, identity, scope);

            List<SchemaControlHistory> history = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                history.Add(new SchemaControlHistory
                {
                    HistoryId = reader.GetGuid(reader.GetOrdinal("HistoryId")),
                    DatabaseIdentityKey = ReadString(reader, "DatabaseIdentityKey"),
                    Scope = ReadString(reader, "Scope"),
                    EventType = ReadString(reader, "EventType"),
                    FromVersion = ReadNullableInt(reader, "FromVersion"),
                    ToVersion = ReadNullableInt(reader, "ToVersion"),
                    OperationId = ReadNullableString(reader, "OperationId"),
                    StartedAtUtc = ReadDateTime(reader, "StartedAtUtc"),
                    CompletedAtUtc = ReadDateTime(reader, "CompletedAtUtc"),
                    Result = ReadString(reader, "Result"),
                    Details = ReadNullableString(reader, "Details")
                });
            }

            return history.ToArray();
        }

        public async Task<IReadOnlyCollection<SchemaControlAttempt>> GetAttemptsAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);

            if (!await TableExistsAsync(connection, "CheckAppSchemaAttempts", cancellationToken))
            {
                return Array.Empty<SchemaControlAttempt>();
            }

            using SqlCommand command = new SqlCommand(@"
SELECT AttemptId, DatabaseIdentityKey, Scope, OperationType, FromVersion, ToVersion,
       StartedAtUtc, CompletedAtUtc, Result, ReasonCode, Error
FROM dbo.CheckAppSchemaAttempts
WHERE DatabaseIdentityKey = @DatabaseIdentityKey
  AND Scope = @Scope
ORDER BY StartedAtUtc DESC, CreatedAtUtc DESC;", connection);
            AddIdentityScopeParameters(command, identity, scope);

            List<SchemaControlAttempt> attempts = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                attempts.Add(new SchemaControlAttempt
                {
                    AttemptId = reader.GetGuid(reader.GetOrdinal("AttemptId")),
                    DatabaseIdentityKey = ReadString(reader, "DatabaseIdentityKey"),
                    Scope = ReadString(reader, "Scope"),
                    OperationType = ReadString(reader, "OperationType"),
                    FromVersion = ReadNullableInt(reader, "FromVersion"),
                    ToVersion = ReadNullableInt(reader, "ToVersion"),
                    StartedAtUtc = ReadDateTime(reader, "StartedAtUtc"),
                    CompletedAtUtc = ReadNullableDateTime(reader, "CompletedAtUtc"),
                    Result = ReadString(reader, "Result"),
                    ReasonCode = ReadNullableString(reader, "ReasonCode"),
                    Error = ReadNullableString(reader, "Error")
                });
            }

            return attempts.ToArray();
        }

        public async Task<Guid> BeginAttemptAsync(
            TenantDatabaseDescriptor descriptor,
            SchemaControlAttempt attempt,
            CancellationToken cancellationToken = default)
        {
            Guid attemptId = attempt.AttemptId == Guid.Empty ? Guid.NewGuid() : attempt.AttemptId;
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);

            using SqlCommand command = new SqlCommand(@"
INSERT INTO dbo.CheckAppSchemaAttempts
    (AttemptId, DatabaseIdentityKey, Scope, OperationType, FromVersion, ToVersion, StartedAtUtc, Result, ReasonCode, Error)
VALUES
    (@AttemptId, @DatabaseIdentityKey, @Scope, @OperationType, @FromVersion, @ToVersion, @StartedAtUtc, @Result, @ReasonCode, @Error);", connection);
            AddAttemptParameters(command, new SchemaControlAttempt
            {
                AttemptId = attemptId,
                DatabaseIdentityKey = attempt.DatabaseIdentityKey,
                Scope = attempt.Scope,
                OperationType = attempt.OperationType,
                FromVersion = attempt.FromVersion,
                ToVersion = attempt.ToVersion,
                StartedAtUtc = attempt.StartedAtUtc,
                CompletedAtUtc = attempt.CompletedAtUtc,
                Result = attempt.Result,
                ReasonCode = Sanitize(attempt.ReasonCode),
                Error = Sanitize(attempt.Error)
            });
            await command.ExecuteNonQueryAsync(cancellationToken);
            return attemptId;
        }

        public async Task CompleteAttemptAsync(
            TenantDatabaseDescriptor descriptor,
            Guid attemptId,
            string result,
            string reasonCode,
            string? error,
            CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);

            using SqlCommand command = new SqlCommand(@"
UPDATE dbo.CheckAppSchemaAttempts
SET CompletedAtUtc = SYSUTCDATETIME(),
    Result = @Result,
    ReasonCode = @ReasonCode,
    Error = @Error,
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE AttemptId = @AttemptId;", connection);
            command.Parameters.AddWithValue("@AttemptId", attemptId);
            command.Parameters.AddWithValue("@Result", NormalizeCode(result, "UNKNOWN"));
            command.Parameters.AddWithValue("@ReasonCode", (object?)NormalizeCode(reasonCode, "UNSPECIFIED") ?? DBNull.Value);
            command.Parameters.AddWithValue("@Error", (object?)Sanitize(error) ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task RecordHistoryAsync(
            TenantDatabaseDescriptor descriptor,
            SchemaControlHistory history,
            CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);

            using SqlCommand command = new SqlCommand(@"
INSERT INTO dbo.CheckAppSchemaHistory
    (HistoryId, DatabaseIdentityKey, Scope, EventType, FromVersion, ToVersion, OperationId, StartedAtUtc, CompletedAtUtc, Result, Details)
VALUES
    (@HistoryId, @DatabaseIdentityKey, @Scope, @EventType, @FromVersion, @ToVersion, @OperationId, @StartedAtUtc, @CompletedAtUtc, @Result, @Details);", connection);
            command.Parameters.AddWithValue("@HistoryId", history.HistoryId == Guid.Empty ? Guid.NewGuid() : history.HistoryId);
            command.Parameters.AddWithValue("@DatabaseIdentityKey", NormalizeKey(history.DatabaseIdentityKey));
            command.Parameters.AddWithValue("@Scope", NormalizeScope(history.Scope));
            command.Parameters.AddWithValue("@EventType", NormalizeCode(history.EventType, "UNKNOWN"));
            command.Parameters.AddWithValue("@FromVersion", (object?)history.FromVersion ?? DBNull.Value);
            command.Parameters.AddWithValue("@ToVersion", (object?)history.ToVersion ?? DBNull.Value);
            command.Parameters.AddWithValue("@OperationId", (object?)Sanitize(history.OperationId) ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartedAtUtc", history.StartedAtUtc == default ? DateTime.UtcNow : history.StartedAtUtc);
            command.Parameters.AddWithValue("@CompletedAtUtc", history.CompletedAtUtc == default ? DateTime.UtcNow : history.CompletedAtUtc);
            command.Parameters.AddWithValue("@Result", NormalizeCode(history.Result, "UNKNOWN"));
            command.Parameters.AddWithValue("@Details", (object?)Sanitize(history.Details) ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task SetConfirmedStateAsync(
            TenantDatabaseDescriptor descriptor,
            SchemaControlState state,
            CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);

            using SqlCommand command = new SqlCommand(@"
UPDATE dbo.CheckAppSchemaState
SET CurrentVersion = @CurrentVersion,
    BaselineId = @BaselineId,
    BaselineVersion = @BaselineVersion,
    ManifestHash = @ManifestHash,
    LastValidatedAtUtc = @LastValidatedAtUtc,
    LastMigratedAtUtc = @LastMigratedAtUtc,
    LastResult = @LastResult,
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE DatabaseIdentityKey = @DatabaseIdentityKey
  AND Scope = @Scope;

IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO dbo.CheckAppSchemaState
        (DatabaseIdentityKey, Scope, CurrentVersion, BaselineId, BaselineVersion, ManifestHash, LastValidatedAtUtc, LastMigratedAtUtc, LastResult)
    VALUES
        (@DatabaseIdentityKey, @Scope, @CurrentVersion, @BaselineId, @BaselineVersion, @ManifestHash, @LastValidatedAtUtc, @LastMigratedAtUtc, @LastResult);
END", connection);
            AddStateParameters(command, state);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = N'dbo' AND t.name = @TableName;", connection);
            command.Parameters.AddWithValue("@TableName", tableName);
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }

        private static async Task<IReadOnlyCollection<string>> GetMissingColumnsAsync(
            SqlConnection connection,
            string tableName,
            IReadOnlyCollection<string> requiredColumns,
            CancellationToken cancellationToken)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT c.name
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = N'dbo' AND t.name = @TableName;", connection);
            command.Parameters.AddWithValue("@TableName", tableName);

            HashSet<string> existing = new(StringComparer.OrdinalIgnoreCase);
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                existing.Add(reader.GetString(0));
            }

            return requiredColumns
                .Where(column => !existing.Contains(column))
                .ToArray();
        }

        private static async Task ExecuteNonQueryAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
        {
            using SqlCommand command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static string GetCreateTableSql(string tableName)
        {
            return tableName switch
            {
                "CheckAppSchemaState" => @"
CREATE TABLE dbo.CheckAppSchemaState
(
    DatabaseIdentityKey nvarchar(512) NOT NULL,
    Scope nvarchar(128) NOT NULL,
    CurrentVersion int NULL,
    BaselineId nvarchar(128) NULL,
    BaselineVersion int NULL,
    ManifestHash nvarchar(128) NULL,
    LastValidatedAtUtc datetime2(3) NULL,
    LastMigratedAtUtc datetime2(3) NULL,
    LastResult nvarchar(64) NOT NULL CONSTRAINT DF_CheckAppSchemaState_LastResult DEFAULT (N'NO_CONFIRMED_VERSION'),
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_CheckAppSchemaState_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    UpdatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_CheckAppSchemaState_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_CheckAppSchemaState PRIMARY KEY (DatabaseIdentityKey, Scope),
    CONSTRAINT CK_CheckAppSchemaState_CurrentVersion_Positive CHECK (CurrentVersion IS NULL OR CurrentVersion > 0),
    CONSTRAINT CK_CheckAppSchemaState_BaselineVersion_Positive CHECK (BaselineVersion IS NULL OR BaselineVersion > 0)
);",
                "CheckAppSchemaHistory" => @"
CREATE TABLE dbo.CheckAppSchemaHistory
(
    HistoryId uniqueidentifier NOT NULL CONSTRAINT DF_CheckAppSchemaHistory_HistoryId DEFAULT (NEWID()),
    DatabaseIdentityKey nvarchar(512) NOT NULL,
    Scope nvarchar(128) NOT NULL,
    EventType nvarchar(32) NOT NULL,
    FromVersion int NULL,
    ToVersion int NULL,
    OperationId nvarchar(128) NULL,
    StartedAtUtc datetime2(3) NOT NULL,
    CompletedAtUtc datetime2(3) NOT NULL,
    Result nvarchar(64) NOT NULL,
    Details nvarchar(2000) NULL,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_CheckAppSchemaHistory_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_CheckAppSchemaHistory PRIMARY KEY (HistoryId)
);
CREATE INDEX IX_CheckAppSchemaHistory_DatabaseScopeStarted
ON dbo.CheckAppSchemaHistory (DatabaseIdentityKey, Scope, StartedAtUtc DESC);",
                "CheckAppSchemaAttempts" => @"
CREATE TABLE dbo.CheckAppSchemaAttempts
(
    AttemptId uniqueidentifier NOT NULL CONSTRAINT DF_CheckAppSchemaAttempts_AttemptId DEFAULT (NEWID()),
    DatabaseIdentityKey nvarchar(512) NOT NULL,
    Scope nvarchar(128) NOT NULL,
    OperationType nvarchar(32) NOT NULL,
    FromVersion int NULL,
    ToVersion int NULL,
    StartedAtUtc datetime2(3) NOT NULL,
    CompletedAtUtc datetime2(3) NULL,
    Result nvarchar(64) NOT NULL,
    ReasonCode nvarchar(128) NULL,
    Error nvarchar(2000) NULL,
    CreatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_CheckAppSchemaAttempts_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    UpdatedAtUtc datetime2(3) NOT NULL CONSTRAINT DF_CheckAppSchemaAttempts_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_CheckAppSchemaAttempts PRIMARY KEY (AttemptId)
);
CREATE INDEX IX_CheckAppSchemaAttempts_DatabaseScopeStarted
ON dbo.CheckAppSchemaAttempts (DatabaseIdentityKey, Scope, StartedAtUtc DESC);",
                _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, null)
            };
        }

        private static void AddIdentityScopeParameters(SqlCommand command, DatabaseIdentity identity, string scope)
        {
            command.Parameters.AddWithValue("@DatabaseIdentityKey", NormalizeKey(identity.Fingerprint));
            command.Parameters.AddWithValue("@Scope", NormalizeScope(scope));
        }

        private static void AddStateParameters(SqlCommand command, SchemaControlState state)
        {
            command.Parameters.AddWithValue("@DatabaseIdentityKey", NormalizeKey(state.DatabaseIdentityKey));
            command.Parameters.AddWithValue("@Scope", NormalizeScope(state.Scope));
            command.Parameters.AddWithValue("@CurrentVersion", (object?)state.CurrentVersion ?? DBNull.Value);
            command.Parameters.AddWithValue("@BaselineId", (object?)Sanitize(state.BaselineId) ?? DBNull.Value);
            command.Parameters.AddWithValue("@BaselineVersion", (object?)state.BaselineVersion ?? DBNull.Value);
            command.Parameters.AddWithValue("@ManifestHash", (object?)Sanitize(state.ManifestHash) ?? DBNull.Value);
            command.Parameters.AddWithValue("@LastValidatedAtUtc", (object?)state.LastValidatedAtUtc ?? DBNull.Value);
            command.Parameters.AddWithValue("@LastMigratedAtUtc", (object?)state.LastMigratedAtUtc ?? DBNull.Value);
            command.Parameters.AddWithValue("@LastResult", NormalizeCode(state.LastResult, "UNKNOWN"));
        }

        private static void AddAttemptParameters(SqlCommand command, SchemaControlAttempt attempt)
        {
            command.Parameters.AddWithValue("@AttemptId", attempt.AttemptId);
            command.Parameters.AddWithValue("@DatabaseIdentityKey", NormalizeKey(attempt.DatabaseIdentityKey));
            command.Parameters.AddWithValue("@Scope", NormalizeScope(attempt.Scope));
            command.Parameters.AddWithValue("@OperationType", NormalizeCode(attempt.OperationType, "UNKNOWN"));
            command.Parameters.AddWithValue("@FromVersion", (object?)attempt.FromVersion ?? DBNull.Value);
            command.Parameters.AddWithValue("@ToVersion", (object?)attempt.ToVersion ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartedAtUtc", attempt.StartedAtUtc == default ? DateTime.UtcNow : attempt.StartedAtUtc);
            command.Parameters.AddWithValue("@Result", NormalizeCode(attempt.Result, "STARTED"));
            command.Parameters.AddWithValue("@ReasonCode", (object?)Sanitize(attempt.ReasonCode) ?? DBNull.Value);
            command.Parameters.AddWithValue("@Error", (object?)Sanitize(attempt.Error) ?? DBNull.Value);
        }

        private static string NormalizeKey(string value)
        {
            return Sanitize(value)?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        private static string NormalizeScope(string value)
        {
            return (Sanitize(value) ?? string.Empty).Trim();
        }

        private static string NormalizeCode(string value, string fallback)
        {
            string? sanitized = Sanitize(value);
            return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized.Trim().ToUpperInvariant();
        }

        private static string? Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string sanitized = Regex.Replace(value, "(Password|Pwd|User Id|UID|Token|Secret)\\s*=\\s*[^;\\s]+", "$1=***", RegexOptions.IgnoreCase);
            return sanitized.Length > 2000 ? sanitized[..2000] : sanitized;
        }

        private static string ReadString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static string? ReadNullableString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        private static int? ReadNullableInt(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static DateTime ReadDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.GetDateTime(ordinal);
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }
    }
}

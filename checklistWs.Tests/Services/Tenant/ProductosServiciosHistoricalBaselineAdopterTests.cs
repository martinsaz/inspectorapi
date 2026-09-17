using System.Data.SqlClient;
using checklistWs.Services.Tenant;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class ProductosServiciosHistoricalBaselineAdopterTests
    {
        private static readonly TenantDatabaseDescriptor Descriptor = new()
        {
            EmpresaKey = "163",
            IdEmpresa = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ConnectionString = "Server=sql-qa;Database=CheckAppErp;"
        };

        private static readonly DatabaseIdentity Identity = new("SQL5111", "SQL5111", string.Empty, "DB_A883C3_CHECKLIST", "e398abab-6416-4e68-8084-7ff7cb232ef5");

        [Fact]
        public async Task MissingEvidenceWithExactV1Baseline_RecordsAdoptedStateHistoryAndAttempt()
        {
            Harness harness = new();

            HistoricalBaselineAdoptionResult result = await harness.Adopter.AdoptIfEligibleAsync(Descriptor, Identity);

            Assert.Equal(HistoricalBaselineAdoptionStatus.Adopted, result.Status);
            Assert.Equal("ADOPTED", result.ReasonCode);
            Assert.True(result.StateModified);
            Assert.True(result.HistoryModified);
            Assert.True(result.AttemptsModified);
            Assert.Equal(1, result.CurrentVersion);
            Assert.Equal(harness.ExpectedManifestHash, result.ManifestHash);
            Assert.Equal(SchemaValidationGlobalResult.SchemaOk, result.SchemaResult);
            Assert.Equal(0, result.DriftCount);

            Assert.NotNull(harness.Repository.State);
            Assert.Equal(1, harness.Repository.State!.CurrentVersion);
            Assert.Equal(ProductosServiciosHistoricalBaselineAdopter.BaselineId, harness.Repository.State.BaselineId);
            Assert.Equal(1, harness.Repository.State.BaselineVersion);
            Assert.Equal(harness.ExpectedManifestHash, harness.Repository.State.ManifestHash);
            Assert.Equal("ADOPTED", harness.Repository.State.LastResult);
            Assert.Null(harness.Repository.State.LastMigratedAtUtc);

            SchemaControlHistory history = Assert.Single(harness.Repository.History);
            Assert.Equal("ADOPTED", history.EventType);
            Assert.Equal("PASS", history.Result);
            Assert.Null(history.FromVersion);
            Assert.Equal(1, history.ToVersion);
            Assert.Contains("DriftCount=0", history.Details);

            SchemaControlAttempt attempt = Assert.Single(harness.Repository.Attempts);
            Assert.Equal("ADOPT_BASELINE", attempt.OperationType);
            Assert.Equal("PASS", harness.Repository.CompletedAttempts[attempt.AttemptId].Result);
            Assert.Equal("ADOPTED", harness.Repository.CompletedAttempts[attempt.AttemptId].ReasonCode);
            Assert.Equal(1, harness.Lock.Calls);
        }

        [Fact]
        public async Task ExistingConfirmedState_DoesNotMutate()
        {
            Harness harness = new();
            harness.Repository.State = new SchemaControlState
            {
                DatabaseIdentityKey = Identity.Fingerprint,
                Scope = DatabaseScopes.ProductosServicios,
                CurrentVersion = 1,
                ManifestHash = harness.ExpectedManifestHash,
                LastResult = "PASS"
            };

            HistoricalBaselineAdoptionResult result = await harness.Adopter.AdoptIfEligibleAsync(Descriptor, Identity);

            Assert.Equal(HistoricalBaselineAdoptionStatus.NoAdoption, result.Status);
            Assert.Equal("STATE_ALREADY_CONFIRMED", result.ReasonCode);
            Assert.Equal(0, harness.Repository.Mutations);
            Assert.Equal(0, harness.DriftValidator.Calls);
        }

        [Fact]
        public async Task IncompleteScope_DoesNotValidateOrMutate()
        {
            Harness harness = new();
            harness.ExistingTableCount = 19;

            HistoricalBaselineAdoptionResult result = await harness.Adopter.AdoptIfEligibleAsync(Descriptor, Identity);

            Assert.Equal(HistoricalBaselineAdoptionStatus.NoAdoption, result.Status);
            Assert.Equal("PHYSICAL_SCOPE_NOT_COMPLETE", result.ReasonCode);
            Assert.Equal(0, harness.Repository.Mutations);
            Assert.Equal(0, harness.DriftValidator.Calls);
        }

        [Fact]
        public async Task DriftBlocksAdoptionWithoutStateOrHistory()
        {
            Harness harness = new();
            harness.DriftValidator.Report = harness.CreateDriftReport(SchemaValidationGlobalResult.SchemaDrift, includeItem: true);

            HistoricalBaselineAdoptionResult result = await harness.Adopter.AdoptIfEligibleAsync(Descriptor, Identity);

            Assert.Equal(HistoricalBaselineAdoptionStatus.Blocked, result.Status);
            Assert.Equal("PHYSICAL_VALIDATION_NOT_SCHEMA_OK", result.ReasonCode);
            Assert.Equal(SchemaValidationGlobalResult.SchemaDrift, result.SchemaResult);
            Assert.Equal(1, result.DriftCount);
            Assert.Null(harness.Repository.State);
            Assert.Empty(harness.Repository.History);
            Assert.Empty(harness.Repository.Attempts);
        }

        [Fact]
        public async Task ExistingBaselineHistory_DoesNotMutate()
        {
            Harness harness = new();
            harness.Repository.History.Add(new SchemaControlHistory
            {
                DatabaseIdentityKey = Identity.Fingerprint,
                Scope = DatabaseScopes.ProductosServicios,
                EventType = "ADOPTED",
                Result = "PASS"
            });

            HistoricalBaselineAdoptionResult result = await harness.Adopter.AdoptIfEligibleAsync(Descriptor, Identity);

            Assert.Equal(HistoricalBaselineAdoptionStatus.NoAdoption, result.Status);
            Assert.Equal("BASELINE_ALREADY_RECORDED", result.ReasonCode);
            Assert.Equal(0, harness.Repository.Mutations);
            Assert.Equal(0, harness.DriftValidator.Calls);
        }

        private sealed class Harness
        {
            public Harness()
            {
                ExpectedManifestHash = new SchemaManifestProvider().CreateManifest(new ProductosServiciosSchemaContractProvider().GetContract(DatabaseScopes.ProductosServicios, 1)).ManifestHash;
                Classifier = new FakeClassifier(this);
                Repository = new FakeRepository();
                VersionProvider = new KnownSchemaVersionProvider();
                ContractProvider = new ProductosServiciosSchemaContractProvider();
                ManifestProvider = new SchemaManifestProvider();
                DriftValidator = new FakeDriftValidator(this) { Report = CreateDriftReport(SchemaValidationGlobalResult.SchemaOk, includeItem: false) };
                Lock = new FakeLock();
                Adopter = new ProductosServiciosHistoricalBaselineAdopter(
                    Classifier,
                    Repository,
                    VersionProvider,
                    ContractProvider,
                    ManifestProvider,
                    DriftValidator,
                    Lock,
                    NullLogger<ProductosServiciosHistoricalBaselineAdopter>.Instance);
            }

            public ProductosServiciosHistoricalBaselineAdopter Adopter { get; }
            public FakeClassifier Classifier { get; }
            public FakeRepository Repository { get; }
            public IKnownSchemaVersionProvider VersionProvider { get; }
            public ISchemaContractProvider ContractProvider { get; }
            public SchemaManifestProvider ManifestProvider { get; }
            public FakeDriftValidator DriftValidator { get; }
            public FakeLock Lock { get; }
            public string ExpectedManifestHash { get; }
            public DatabaseStructureState ClassificationState { get; set; } = DatabaseStructureState.Unknown;
            public string ClassificationReasonCode { get; set; } = "VERSION_EVIDENCE_MISSING";
            public int ExpectedTableCount { get; set; } = 20;
            public int ExistingTableCount { get; set; } = 20;

            public SchemaDriftReport CreateDriftReport(SchemaValidationGlobalResult globalResult, bool includeItem) => new()
            {
                SanitizedIdentity = Identity.ToSanitizedString(),
                Scope = DatabaseScopes.ProductosServicios,
                DeclaredVersion = 1,
                ExpectedManifestHash = ExpectedManifestHash,
                PersistedManifestHash = ExpectedManifestHash,
                GlobalResult = globalResult,
                Items = includeItem
                    ? new[] { new SchemaDriftItem(Identity.ToSanitizedString(), DatabaseScopes.ProductosServicios, 1, ExpectedManifestHash, "TABLE", "dbo.ProductosServicios", "TABLE_MISSING", "TABLE", null, SchemaDriftSeverity.Critical, "YES", "TABLE_MISSING") }
                    : Array.Empty<SchemaDriftItem>(),
                DetectedTables = 20,
                DetectedColumns = 255,
                DetectedIndexes = 50,
                DetectedForeignKeys = 24,
                DetectedChecks = 14,
                ChangedState = false,
                ExecutedDdl = false
            };
        }

        private sealed class FakeClassifier : IDatabaseStateClassifier
        {
            private readonly Harness _harness;
            public FakeClassifier(Harness harness) => _harness = harness;
            public Task<DatabaseClassificationResult> ClassifyAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new DatabaseClassificationResult
                {
                    Identity = identity,
                    SanitizedIdentity = identity.ToSanitizedString(),
                    Scope = scope,
                    State = _harness.ClassificationState,
                    ReasonCode = _harness.ClassificationReasonCode,
                    ExpectedScopeTableCount = _harness.ExpectedTableCount,
                    ExistingScopeTableCount = _harness.ExistingTableCount,
                    IsAvailable = true
                });
            }
        }

        private sealed class FakeRepository : ISchemaVersionRepository
        {
            public SchemaControlState? State { get; set; }
            public List<SchemaControlHistory> History { get; } = new();
            public List<SchemaControlAttempt> Attempts { get; } = new();
            public Dictionary<Guid, (string Result, string ReasonCode, string? Error)> CompletedAttempts { get; } = new();
            public int Mutations { get; private set; }

            public Task<SchemaControlInfrastructureResult> EnsureSchemaControlInfrastructureAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default) => Task.FromResult(new SchemaControlInfrastructureResult { Status = SchemaControlInfrastructureStatus.Ready });
            public Task<SchemaControlState?> GetStateAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default) => Task.FromResult(State);
            public Task<IReadOnlyCollection<SchemaControlHistory>> GetHistoryAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<SchemaControlHistory>>(History.ToArray());
            public Task<IReadOnlyCollection<SchemaControlAttempt>> GetAttemptsAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<SchemaControlAttempt>>(Attempts.ToArray());
            public Task<Guid> BeginAttemptAsync(TenantDatabaseDescriptor descriptor, SchemaControlAttempt attempt, CancellationToken cancellationToken = default) { Mutations++; Attempts.Add(attempt); return Task.FromResult(attempt.AttemptId); }
            public Task CompleteAttemptAsync(TenantDatabaseDescriptor descriptor, Guid attemptId, string result, string reasonCode, string? error, CancellationToken cancellationToken = default) { Mutations++; CompletedAttempts[attemptId] = (result, reasonCode, error); return Task.CompletedTask; }
            public Task RecordHistoryAsync(TenantDatabaseDescriptor descriptor, SchemaControlHistory history, CancellationToken cancellationToken = default) { Mutations++; History.Add(history); return Task.CompletedTask; }
            public Task SetConfirmedStateAsync(TenantDatabaseDescriptor descriptor, SchemaControlState state, CancellationToken cancellationToken = default) { Mutations++; State = state; return Task.CompletedTask; }
        }

        private sealed class FakeDriftValidator : ISchemaDriftValidator
        {
            private readonly Harness _harness;
            public FakeDriftValidator(Harness harness) => _harness = harness;
            public SchemaDriftReport Report { get; set; } = null!;
            public int Calls { get; private set; }
            public Task<SchemaDriftReport> ValidateAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, int? declaredVersion, string? persistedManifestHash, CancellationToken cancellationToken = default)
            {
                Calls++;
                Assert.Equal(1, declaredVersion);
                Assert.Equal(_harness.ExpectedManifestHash, persistedManifestHash);
                return Task.FromResult(Report);
            }
            public Task<SchemaDriftReport> ValidateAsync(SqlConnection connection, SqlTransaction? transaction, DatabaseIdentity identity, string scope, int? declaredVersion, string? persistedManifestHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class FakeLock : ISchemaOperationLock
        {
            public int Calls { get; private set; }
            public string BuildResource(DatabaseIdentity identity, string scope, SchemaOperationLockKind kind) => $"{kind}:{identity.Fingerprint}:{scope}";
            public Task<SchemaOperationLockHandle> AcquireAsync(SqlConnection connection, SchemaOperationLockRequest request, CancellationToken cancellationToken = default, SqlTransaction? transaction = null) => throw new NotSupportedException();
            public Task<T> ExecuteWithControlAndScopeAsync<T>(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
            {
                Calls++;
                return action(cancellationToken);
            }
        }
    }
}

using System.Data.SqlClient;
using checklistWs.Services.Tenant;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class ProductosServiciosCompatibilityGateTests
    {
        private static readonly TenantDatabaseDescriptor Descriptor = new()
        {
            EmpresaKey = "empresa-qa",
            IdEmpresa = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ConnectionString = "Server=sql-qa;Database=CheckAppErp;"
        };

        private static readonly DatabaseIdentity Identity = new("SQL-QA", "SQL-QA", string.Empty, "CHECKAPPERP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        [Fact]
        public async Task CompatibleDatabase_AllowsCrudWithSanitizedDecision()
        {
            Harness harness = new();

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            Assert.True(decision.IsAllowed);
            Assert.Equal("COMPATIBLE", decision.ReasonCode);
            Assert.Equal(DatabaseScopes.ProductosServicios, decision.Scope);
            Assert.Equal(1, decision.CurrentVersion);
            Assert.Equal(1, decision.LatestSupportedVersion);
            Assert.Equal(SchemaValidationGlobalResult.SchemaOk, decision.SchemaResult);
            Assert.Equal(Identity.ToSanitizedString(), decision.SanitizedIdentity);
            Assert.False(decision.Retryable);
            Assert.False(string.IsNullOrWhiteSpace(decision.ReferenceId));
            Assert.True(decision.CheckedAtUtc > DateTime.UtcNow.AddMinutes(-1));
            Assert.Equal(1, harness.DriftValidator.Calls);
            Assert.Equal(0, harness.Repository.Mutations);
        }

        [Theory]
        [InlineData(DatabaseStructureState.Empty, "SCHEMA_EMPTY")]
        [InlineData(DatabaseStructureState.Partial, "SCHEMA_PARTIAL")]
        [InlineData(DatabaseStructureState.Outdated, "SCHEMA_OUTDATED")]
        [InlineData(DatabaseStructureState.Future, "SCHEMA_FUTURE")]
        [InlineData(DatabaseStructureState.Unknown, "SCHEMA_UNKNOWN")]
        public async Task StructuralStates_BlockBeforeDrift(DatabaseStructureState state, string expectedReason)
        {
            Harness harness = new() { ClassificationState = state };

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            AssertBlocked(decision, expectedReason);
            Assert.Equal(0, harness.DriftValidator.Calls);
            Assert.Equal(0, harness.Repository.Mutations);
        }

        [Fact]
        public async Task UnavailableClassifier_BlocksWithRetryableUnavailable()
        {
            Harness harness = new() { IsAvailable = false };

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            AssertBlocked(decision, "SCHEMA_UNAVAILABLE", retryable: true);
            Assert.Equal(0, harness.DriftValidator.Calls);
        }

        [Fact]
        public async Task MissingVersionEvidence_BlocksBeforeDrift()
        {
            Harness harness = new();
            harness.Repository.State = null;

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            AssertBlocked(decision, "VERSION_EVIDENCE_MISSING");
            Assert.Equal(0, harness.DriftValidator.Calls);
        }

        [Theory]
        [InlineData(0, "SCHEMA_OUTDATED")]
        [InlineData(2, "SCHEMA_FUTURE")]
        public async Task UnsupportedVersion_BlocksBeforeContractAndDrift(int currentVersion, string expectedReason)
        {
            Harness harness = new();
            harness.Repository.State = harness.CreateState(currentVersion, harness.ExpectedManifestHash);

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            AssertBlocked(decision, expectedReason);
            Assert.Equal(currentVersion, decision.CurrentVersion);
            Assert.Equal(0, harness.DriftValidator.Calls);
        }

        [Fact]
        public async Task UnknownCurrentVersionFromApplication_BlocksAsVersionIncompatible()
        {
            Harness harness = new() { LatestSupportedVersion = null };

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            AssertBlocked(decision, "VERSION_INCOMPATIBLE");
            Assert.Equal(0, harness.Classifier.Calls);
            Assert.Equal(0, harness.DriftValidator.Calls);
        }

        [Fact]
        public async Task MissingContractForCurrentVersion_BlocksAsVersionIncompatible()
        {
            Harness harness = new() { ContractThrows = true };

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            AssertBlocked(decision, "VERSION_INCOMPATIBLE");
            Assert.Equal(0, harness.DriftValidator.Calls);
        }

        [Fact]
        public async Task ManifestHashMismatch_BlocksBeforeDrift()
        {
            Harness harness = new();
            harness.Repository.State = harness.CreateState(1, "bad-hash");

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            AssertBlocked(decision, "MANIFEST_HASH_MISMATCH");
            Assert.Equal(0, harness.DriftValidator.Calls);
        }

        [Theory]
        [InlineData(SchemaValidationGlobalResult.SchemaDrift, "SCHEMA_DRIFT", false)]
        [InlineData(SchemaValidationGlobalResult.SchemaDriftCritico, "SCHEMA_DRIFT_CRITICAL", false)]
        [InlineData(SchemaValidationGlobalResult.ValidacionNoConcluyente, "VALIDATION_INCONCLUSIVE", true)]
        [InlineData(SchemaValidationGlobalResult.VersionIncompatible, "VERSION_INCOMPATIBLE", false)]
        [InlineData(SchemaValidationGlobalResult.RequiereRevision, "REQUIRES_REVIEW", true)]
        public async Task DriftResults_BlockWithControlledReason(SchemaValidationGlobalResult globalResult, string expectedReason, bool retryable)
        {
            Harness harness = new();
            harness.DriftValidator.Report = harness.CreateDriftReport(globalResult, includeItem: true);

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            AssertBlocked(decision, expectedReason, retryable);
            Assert.Equal(globalResult, decision.SchemaResult);
            Assert.Equal(1, harness.DriftValidator.Calls);
        }

        [Fact]
        public async Task SchemaOkWithUnexpectedItems_FailsClosed()
        {
            Harness harness = new();
            harness.DriftValidator.Report = harness.CreateDriftReport(SchemaValidationGlobalResult.SchemaOk, includeItem: true);

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            AssertBlocked(decision, "REQUIRES_REVIEW", retryable: true);
        }

        [Theory]
        [InlineData("PROVISION", "SCHEMA_PREPARING")]
        [InlineData("MIGRATION", "MIGRATION_IN_PROGRESS")]
        public async Task ActiveStartedAttempt_BlocksAsPreparingOrMigrating(string operationType, string expectedReason)
        {
            Harness harness = new();
            harness.Repository.Attempts.Add(new SchemaControlAttempt
            {
                DatabaseIdentityKey = Identity.Fingerprint,
                Scope = DatabaseScopes.ProductosServicios,
                OperationType = operationType,
                Result = "STARTED",
                StartedAtUtc = DateTime.UtcNow.AddSeconds(-30)
            });

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            AssertBlocked(decision, expectedReason, retryable: true);
            Assert.Equal(0, harness.DriftValidator.Calls);
        }

        [Fact]
        public async Task CompletedAttempts_DoNotBlock()
        {
            Harness harness = new();
            harness.Repository.Attempts.Add(new SchemaControlAttempt
            {
                DatabaseIdentityKey = Identity.Fingerprint,
                Scope = DatabaseScopes.ProductosServicios,
                OperationType = "MIGRATION",
                Result = "STARTED",
                StartedAtUtc = DateTime.UtcNow.AddMinutes(-1),
                CompletedAtUtc = DateTime.UtcNow,
                ReasonCode = "PASS"
            });

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            Assert.True(decision.IsAllowed);
            Assert.Equal("COMPATIBLE", decision.ReasonCode);
        }

        [Fact]
        public async Task TenantContextInvalid_BlocksWithoutResolverOrDdl()
        {
            Harness harness = new();

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(new TenantDatabaseDescriptor { EmpresaKey = "x", ConnectionString = "" });

            AssertBlocked(decision, "TENANT_CONTEXT_INVALID");
            Assert.Equal(0, harness.IdentityResolver.Calls);
            Assert.Equal(0, harness.Repository.Mutations);
        }

        [Fact]
        public async Task UnsupportedScope_BlocksTenantContextInvalid()
        {
            Harness harness = new();

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor, "OtroScope");

            AssertBlocked(decision, "TENANT_CONTEXT_INVALID");
            Assert.Equal("OtroScope", decision.Scope);
            Assert.Equal(0, harness.IdentityResolver.Calls);
        }

        [Fact]
        public async Task IdentityResolutionFailure_BlocksWithoutSecrets()
        {
            Harness harness = new();
            harness.IdentityResolver.Exception = new DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode.DatabaseIdentityUnavailable);

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            AssertBlocked(decision, "DATABASE_IDENTITY_INVALID", retryable: true);
            Assert.DoesNotContain("Server=", decision.SanitizedIdentity, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UnexpectedFailure_FailsClosedWithReferenceId()
        {
            Harness harness = new();
            harness.Repository.Exception = new InvalidOperationException("boom");

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            AssertBlocked(decision, "REQUIRES_REVIEW", retryable: true);
            Assert.False(string.IsNullOrWhiteSpace(decision.ReferenceId));
            Assert.DoesNotContain("boom", decision.ReasonCode, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task NoCompatibilityCache_ReevaluatesDriftEveryTime()
        {
            Harness harness = new();

            CompatibilityDecision first = await harness.Gate.EvaluateAsync(Descriptor);
            harness.DriftValidator.Report = harness.CreateDriftReport(SchemaValidationGlobalResult.SchemaDrift, includeItem: true);
            CompatibilityDecision second = await harness.Gate.EvaluateAsync(Descriptor);

            Assert.True(first.IsAllowed);
            AssertBlocked(second, "SCHEMA_DRIFT");
            Assert.Equal(2, harness.DriftValidator.Calls);
        }

        [Fact]
        public async Task GateDoesNotPersistStateHistoryOrAttempts()
        {
            Harness harness = new();
            SchemaControlState original = harness.Repository.State!;

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(Descriptor);

            Assert.True(decision.IsAllowed);
            Assert.Same(original, harness.Repository.State);
            Assert.Empty(harness.Repository.History);
            Assert.Empty(harness.Repository.Attempts);
            Assert.Equal(0, harness.Repository.Mutations);
        }

        [Fact]
        public async Task ClientSuppliedDescriptorIdentityCannotOverrideResolvedIdentity()
        {
            Harness harness = new();
            TenantDatabaseDescriptor descriptorWithClientNoise = new()
            {
                EmpresaKey = "client-claims-v99",
                IdEmpresa = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ConnectionString = Descriptor.ConnectionString
            };

            CompatibilityDecision decision = await harness.Gate.EvaluateAsync(descriptorWithClientNoise);

            Assert.True(decision.IsAllowed);
            Assert.Equal(Identity.ToSanitizedString(), decision.SanitizedIdentity);
            Assert.Equal(1, decision.CurrentVersion);
        }

        private static void AssertBlocked(CompatibilityDecision decision, string reason, bool? retryable = null)
        {
            Assert.False(decision.IsAllowed);
            Assert.Equal(reason, decision.ReasonCode);
            Assert.False(string.IsNullOrWhiteSpace(decision.ReferenceId));
            if (retryable.HasValue)
            {
                Assert.Equal(retryable.Value, decision.Retryable);
            }
        }

        private sealed class Harness
        {
            public Harness()
            {
                ExpectedManifestHash = new SchemaManifestProvider().CreateManifest(new ProductosServiciosSchemaContractProvider().GetContract(DatabaseScopes.ProductosServicios, 1)).ManifestHash;
                Repository = new FakeRepository(this) { State = CreateState(1, ExpectedManifestHash) };
                IdentityResolver = new FakeIdentityResolver(this);
                Classifier = new FakeClassifier(this);
                VersionProvider = new FakeVersionProvider(this);
                ContractProvider = new FakeContractProvider(this);
                ManifestProvider = new SchemaManifestProvider();
                DriftValidator = new FakeDriftValidator(this) { Report = CreateDriftReport(SchemaValidationGlobalResult.SchemaOk, includeItem: false) };
                Gate = new ProductosServiciosCompatibilityGate(IdentityResolver, Classifier, Repository, VersionProvider, ContractProvider, ManifestProvider, DriftValidator, NullLogger<ProductosServiciosCompatibilityGate>.Instance);
            }

            public ProductosServiciosCompatibilityGate Gate { get; }
            public FakeRepository Repository { get; }
            public FakeIdentityResolver IdentityResolver { get; }
            public FakeClassifier Classifier { get; }
            public FakeVersionProvider VersionProvider { get; }
            public FakeContractProvider ContractProvider { get; }
            public SchemaManifestProvider ManifestProvider { get; }
            public FakeDriftValidator DriftValidator { get; }
            public string ExpectedManifestHash { get; }
            public DatabaseStructureState ClassificationState { get; init; } = DatabaseStructureState.Current;
            public bool IsAvailable { get; init; } = true;
            public int? LatestSupportedVersion { get; init; } = 1;
            public bool ContractThrows { get; init; }

            public SchemaControlState CreateState(int? currentVersion, string? manifestHash) => new()
            {
                DatabaseIdentityKey = Identity.Fingerprint,
                Scope = DatabaseScopes.ProductosServicios,
                CurrentVersion = currentVersion,
                ManifestHash = manifestHash,
                LastResult = "PASS",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-1)
            };

            public SchemaDriftReport CreateDriftReport(SchemaValidationGlobalResult globalResult, bool includeItem) => new()
            {
                SanitizedIdentity = Identity.ToSanitizedString(),
                Scope = DatabaseScopes.ProductosServicios,
                DeclaredVersion = 1,
                ExpectedManifestHash = ExpectedManifestHash,
                PersistedManifestHash = ExpectedManifestHash,
                GlobalResult = globalResult,
                Items = includeItem
                    ? new[] { new SchemaDriftItem(Identity.ToSanitizedString(), DatabaseScopes.ProductosServicios, 1, ExpectedManifestHash, "INDEX", "IX_Test", "INDEX_MISSING", "expected", null, SchemaDriftSeverity.Error, "NO", "INDEX_MISSING") }
                    : Array.Empty<SchemaDriftItem>(),
                ChangedState = false,
                ExecutedDdl = false
            };
        }

        private sealed class FakeIdentityResolver : IDatabaseIdentityResolver
        {
            private readonly Harness _harness;
            public FakeIdentityResolver(Harness harness) => _harness = harness;
            public int Calls { get; private set; }
            public Exception? Exception { get; set; }
            public Task<DatabaseIdentity> ResolveAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default)
            {
                Calls++;
                if (Exception != null) throw Exception;
                return Task.FromResult(Identity);
            }
        }

        private sealed class FakeClassifier : IDatabaseStateClassifier
        {
            private readonly Harness _harness;
            public FakeClassifier(Harness harness) => _harness = harness;
            public int Calls { get; private set; }
            public Task<DatabaseClassificationResult> ClassifyAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                Calls++;
                return Task.FromResult(new DatabaseClassificationResult
                {
                    Identity = identity,
                    SanitizedIdentity = identity.ToSanitizedString(),
                    Scope = scope,
                    State = _harness.ClassificationState,
                    IsAvailable = _harness.IsAvailable,
                    ReasonCode = _harness.IsAvailable ? "CURRENT" : "UNAVAILABLE"
                });
            }
        }

        private sealed class FakeVersionProvider : IKnownSchemaVersionProvider
        {
            private readonly Harness _harness;
            public FakeVersionProvider(Harness harness) => _harness = harness;
            public int? GetKnownCurrentVersion(string scope) => _harness.LatestSupportedVersion;
        }

        private sealed class FakeContractProvider : ISchemaContractProvider
        {
            private readonly Harness _harness;
            private readonly ProductosServiciosSchemaContractProvider _inner = new();
            public FakeContractProvider(Harness harness) => _harness = harness;
            public SchemaContract GetContract(string scope, int? version)
            {
                if (_harness.ContractThrows) throw new InvalidOperationException("unsupported version");
                return _inner.GetContract(scope, version);
            }
        }

        private sealed class FakeRepository : ISchemaVersionRepository
        {
            private readonly Harness _harness;
            public FakeRepository(Harness harness) => _harness = harness;
            public SchemaControlState? State { get; set; }
            public List<SchemaControlAttempt> Attempts { get; } = new();
            public List<SchemaControlHistory> History { get; } = new();
            public Exception? Exception { get; set; }
            public int Mutations { get; private set; }
            public Task<SchemaControlInfrastructureResult> EnsureSchemaControlInfrastructureAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default) { Mutations++; return Task.FromResult(new SchemaControlInfrastructureResult { Status = SchemaControlInfrastructureStatus.Ready }); }
            public Task<SchemaControlState?> GetStateAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                if (Exception != null) throw Exception;
                return Task.FromResult(State != null && State.DatabaseIdentityKey == identity.Fingerprint && State.Scope == scope ? State : null);
            }
            public Task<IReadOnlyCollection<SchemaControlHistory>> GetHistoryAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<SchemaControlHistory>>(History.ToArray());
            public Task<IReadOnlyCollection<SchemaControlAttempt>> GetAttemptsAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<SchemaControlAttempt>>(Attempts.Where(a => a.DatabaseIdentityKey == identity.Fingerprint && a.Scope == scope).ToArray());
            public Task<Guid> BeginAttemptAsync(TenantDatabaseDescriptor descriptor, SchemaControlAttempt attempt, CancellationToken cancellationToken = default) { Mutations++; Attempts.Add(attempt); return Task.FromResult(attempt.AttemptId); }
            public Task CompleteAttemptAsync(TenantDatabaseDescriptor descriptor, Guid attemptId, string result, string reasonCode, string? error, CancellationToken cancellationToken = default) { Mutations++; return Task.CompletedTask; }
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
                Assert.Equal(Identity, identity);
                Assert.Equal(DatabaseScopes.ProductosServicios, scope);
                Assert.Equal(1, declaredVersion);
                Assert.Equal(_harness.ExpectedManifestHash, persistedManifestHash);
                return Task.FromResult(Report);
            }
            public Task<SchemaDriftReport> ValidateAsync(SqlConnection connection, SqlTransaction? transaction, DatabaseIdentity identity, string scope, int? declaredVersion, string? persistedManifestHash, CancellationToken cancellationToken = default) => throw new NotSupportedException("Gate must not use transactional drift validation");
        }
    }
}

using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class SchemaVersionControlTests
    {
        private static readonly TenantDatabaseDescriptor Descriptor = new TenantDatabaseDescriptor
        {
            EmpresaKey = "163",
            IdEmpresa = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ConnectionString = "Server=alias;Database=checkapp;TrustServerCertificate=True"
        };

        private static readonly DatabaseIdentity IdentityA = new DatabaseIdentity("SERVER-A", "SERVER-A", string.Empty, "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly DatabaseIdentity IdentityB = new DatabaseIdentity("SERVER-B", "SERVER-B", string.Empty, "CHECKAPP", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        [Fact]
        public async Task EnsureSchemaControlInfrastructureAsync_WhenMissing_CreatesThreeControlTables()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();

            SchemaControlInfrastructureResult result = await repository.EnsureSchemaControlInfrastructureAsync(Descriptor);

            Assert.Equal(SchemaControlInfrastructureStatus.Ready, result.Status);
            Assert.Equal(3, result.CreatedObjects.Count);
            Assert.Contains("dbo.CheckAppSchemaState", result.CreatedObjects);
            Assert.Contains("dbo.CheckAppSchemaHistory", result.CreatedObjects);
            Assert.Contains("dbo.CheckAppSchemaAttempts", result.CreatedObjects);
        }

        [Fact]
        public async Task EnsureSchemaControlInfrastructureAsync_SecondRun_IsIdempotent()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();
            await repository.EnsureSchemaControlInfrastructureAsync(Descriptor);

            SchemaControlInfrastructureResult result = await repository.EnsureSchemaControlInfrastructureAsync(Descriptor);

            Assert.Equal(SchemaControlInfrastructureStatus.Ready, result.Status);
            Assert.Empty(result.CreatedObjects);
            Assert.Equal(3, result.VerifiedObjects.Count);
        }

        [Fact]
        public async Task VersionEvidenceReader_EmptyState_ReturnsMissing()
        {
            DatabaseVersionEvidence evidence = await CreateEvidenceReader(new InMemorySchemaVersionRepository())
                .ReadVersionEvidenceAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseVersionEvidenceState.None, evidence.State);
            Assert.Equal("VERSION_EVIDENCE_MISSING", evidence.ReasonCode);
        }

        [Fact]
        public async Task VersionEvidenceReader_ConfirmedCurrentVersion_ReturnsCurrent()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();
            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityA, DatabaseScopes.ProductosServicios, 1, "VALIDATED"));

            DatabaseVersionEvidence evidence = await CreateEvidenceReader(repository)
                .ReadVersionEvidenceAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseVersionEvidenceState.Current, evidence.State);
        }

        [Fact]
        public async Task Repository_TwoScopesSameDatabase_KeepSeparateStates()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();

            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityA, DatabaseScopes.ProductosServicios, 1, "VALIDATED"));
            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityA, "Activos", 2, "VALIDATED"));

            SchemaControlState? productos = await repository.GetStateAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios);
            SchemaControlState? activos = await repository.GetStateAsync(Descriptor, IdentityA, "Activos");

            Assert.Equal(1, productos?.CurrentVersion);
            Assert.Equal(2, activos?.CurrentVersion);
        }

        [Fact]
        public async Task Repository_TwoDatabaseIdentities_KeepSeparateStates()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();

            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityA, DatabaseScopes.ProductosServicios, 1, "VALIDATED"));
            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityB, DatabaseScopes.ProductosServicios, 2, "VALIDATED"));

            SchemaControlState? first = await repository.GetStateAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios);
            SchemaControlState? second = await repository.GetStateAsync(Descriptor, IdentityB, DatabaseScopes.ProductosServicios);

            Assert.Equal(1, first?.CurrentVersion);
            Assert.Equal(2, second?.CurrentVersion);
        }

        [Fact]
        public async Task SameDatabaseWithMultipleTenants_UsesOneStateNotTenantState()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();

            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityA, DatabaseScopes.ProductosServicios, 1, "VALIDATED"));
            SchemaControlState? fromTenantA = await repository.GetStateAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios);
            SchemaControlState? fromTenantB = await repository.GetStateAsync(new TenantDatabaseDescriptor
            {
                EmpresaKey = "164",
                IdEmpresa = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ConnectionString = "Server=other-alias;Database=checkapp;TrustServerCertificate=True"
            }, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(fromTenantA?.DatabaseIdentityKey, fromTenantB?.DatabaseIdentityKey);
            Assert.Single(repository.States);
        }

        [Fact]
        public async Task BeginAttemptAsync_RegistersAttempt()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();

            Guid attemptId = await repository.BeginAttemptAsync(Descriptor, Attempt(IdentityA, DatabaseScopes.ProductosServicios, "VALIDATE"));

            IReadOnlyCollection<SchemaControlAttempt> attempts = await repository.GetAttemptsAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios);
            Assert.Equal(attemptId, attempts.Single().AttemptId);
            Assert.Equal("STARTED", attempts.Single().Result);
        }

        [Fact]
        public async Task CompleteAttemptAsync_Fail_CompletesAttempt()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();
            Guid attemptId = await repository.BeginAttemptAsync(Descriptor, Attempt(IdentityA, DatabaseScopes.ProductosServicios, "MIGRATE"));

            await repository.CompleteAttemptAsync(Descriptor, attemptId, "FAIL", "SIMULATED_FAILURE", "Password=secret; Token=abc");

            SchemaControlAttempt attempt = (await repository.GetAttemptsAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios)).Single();
            Assert.Equal("FAIL", attempt.Result);
            Assert.Equal("SIMULATED_FAILURE", attempt.ReasonCode);
            Assert.DoesNotContain("secret", attempt.Error, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("abc", attempt.Error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task FailedAttempt_DoesNotAdvanceState()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();
            Guid attemptId = await repository.BeginAttemptAsync(Descriptor, Attempt(IdentityA, DatabaseScopes.ProductosServicios, "MIGRATE"));

            await repository.CompleteAttemptAsync(Descriptor, attemptId, "FAIL", "SIMULATED_FAILURE", null);

            SchemaControlState? state = await repository.GetStateAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios);
            Assert.Null(state);
        }

        [Fact]
        public async Task RecordHistoryAsync_RegistersConfirmedEvent()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();

            await repository.RecordHistoryAsync(Descriptor, History(IdentityA, DatabaseScopes.ProductosServicios, "VALIDATED", 1));

            IReadOnlyCollection<SchemaControlHistory> history = await repository.GetHistoryAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios);
            Assert.Single(history);
            Assert.Equal("VALIDATED", history.Single().EventType);
        }

        [Fact]
        public async Task History_DoesNotConfuseAttempts()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();

            await repository.BeginAttemptAsync(Descriptor, Attempt(IdentityA, DatabaseScopes.ProductosServicios, "VALIDATE"));
            await repository.RecordHistoryAsync(Descriptor, History(IdentityA, DatabaseScopes.ProductosServicios, "VALIDATED", 1));

            Assert.Single(await repository.GetAttemptsAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios));
            Assert.Single(await repository.GetHistoryAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios));
        }

        [Fact]
        public async Task SetConfirmedStateAsync_OnlyAffectsMatchingDatabaseAndScope()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();
            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityA, DatabaseScopes.ProductosServicios, 1, "VALIDATED"));
            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityA, "Activos", 3, "VALIDATED"));

            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityA, DatabaseScopes.ProductosServicios, 2, "VALIDATED"));

            Assert.Equal(2, (await repository.GetStateAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios))?.CurrentVersion);
            Assert.Equal(3, (await repository.GetStateAsync(Descriptor, IdentityA, "Activos"))?.CurrentVersion);
        }

        [Fact]
        public async Task EnsureSchemaControlInfrastructureAsync_IncompatibleTable_ReturnsConflict()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository(conflict: true);

            SchemaControlInfrastructureResult result = await repository.EnsureSchemaControlInfrastructureAsync(Descriptor);

            Assert.Equal(SchemaControlInfrastructureStatus.Conflict, result.Status);
            Assert.Contains(result.Conflicts, conflict => conflict.Contains("CONTROL_SCHEMA_CONFLICT", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task EnsureSchemaControlInfrastructureAsync_ConcurrentRuns_DoNotDuplicateStateTables()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();

            await Task.WhenAll(
                repository.EnsureSchemaControlInfrastructureAsync(Descriptor),
                repository.EnsureSchemaControlInfrastructureAsync(Descriptor));

            Assert.Equal(3, repository.ControlObjects.Count);
        }

        [Fact]
        public async Task Persistence_DoesNotStoreSecrets()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();

            await repository.RecordHistoryAsync(Descriptor, History(IdentityA, DatabaseScopes.ProductosServicios, "VALIDATED", 1, "Password=secret;User Id=admin;Token=abc"));

            string? details = (await repository.GetHistoryAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios)).Single().Details;
            Assert.DoesNotContain("secret", details, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("admin", details, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("abc", details, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task T13_ConsumesRealVersionEvidence_FromT14Repository()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();
            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityA, DatabaseScopes.ProductosServicios, 1, "VALIDATED"));

            DatabaseStateClassifier classifier = new DatabaseStateClassifier(
                new CompleteProductScopeProbe(),
                CreateEvidenceReader(repository));

            DatabaseClassificationResult result = await classifier.ClassifyAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Current, result.State);
        }

        [Fact]
        public async Task T13_WithoutState_DoesNotInventCurrent()
        {
            DatabaseStateClassifier classifier = new DatabaseStateClassifier(
                new CompleteProductScopeProbe(),
                CreateEvidenceReader(new InMemorySchemaVersionRepository()));

            DatabaseClassificationResult result = await classifier.ClassifyAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Unknown, result.State);
            Assert.Equal("VERSION_EVIDENCE_MISSING", result.ReasonCode);
        }

        [Fact]
        public async Task VersionEvidenceReader_ScopeA_DoesNotContaminateScopeB()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();
            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityA, DatabaseScopes.ProductosServicios, 1, "VALIDATED"));

            DatabaseVersionEvidence evidence = await CreateEvidenceReader(repository)
                .ReadVersionEvidenceAsync(Descriptor, IdentityA, "Activos");

            Assert.Equal(DatabaseVersionEvidenceState.None, evidence.State);
            Assert.Equal("VERSION_EVIDENCE_MISSING", evidence.ReasonCode);
        }

        [Fact]
        public async Task EnsureSchemaControlInfrastructureAsync_DoesNotTouchFunctionalTables()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();

            await repository.EnsureSchemaControlInfrastructureAsync(Descriptor);

            Assert.DoesNotContain(repository.ControlObjects, item => item.Contains("ProductosServicios", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task VersionEvidenceReader_OutdatedAndFuture_AreComparable()
        {
            InMemorySchemaVersionRepository repository = new InMemorySchemaVersionRepository();
            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityA, DatabaseScopes.ProductosServicios, 0, "VALIDATED"));
            await repository.SetConfirmedStateAsync(Descriptor, State(IdentityB, DatabaseScopes.ProductosServicios, 2, "VALIDATED"));

            DatabaseVersionEvidence outdated = await CreateEvidenceReader(repository)
                .ReadVersionEvidenceAsync(Descriptor, IdentityA, DatabaseScopes.ProductosServicios);
            DatabaseVersionEvidence future = await CreateEvidenceReader(repository)
                .ReadVersionEvidenceAsync(Descriptor, IdentityB, DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseVersionEvidenceState.Outdated, outdated.State);
            Assert.Equal(DatabaseVersionEvidenceState.Future, future.State);
        }

        private static DatabaseVersionEvidenceReader CreateEvidenceReader(ISchemaVersionRepository repository)
        {
            return new DatabaseVersionEvidenceReader(repository, new KnownSchemaVersionProvider());
        }

        private static SchemaControlState State(DatabaseIdentity identity, string scope, int version, string result)
        {
            DateTime now = DateTime.UtcNow;
            return new SchemaControlState
            {
                DatabaseIdentityKey = identity.Fingerprint,
                Scope = scope,
                CurrentVersion = version,
                LastResult = result,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
        }

        private static SchemaControlAttempt Attempt(DatabaseIdentity identity, string scope, string operationType)
        {
            return new SchemaControlAttempt
            {
                DatabaseIdentityKey = identity.Fingerprint,
                Scope = scope,
                OperationType = operationType,
                StartedAtUtc = DateTime.UtcNow,
                Result = "STARTED"
            };
        }

        private static SchemaControlHistory History(DatabaseIdentity identity, string scope, string eventType, int version, string? details = null)
        {
            DateTime now = DateTime.UtcNow;
            return new SchemaControlHistory
            {
                DatabaseIdentityKey = identity.Fingerprint,
                Scope = scope,
                EventType = eventType,
                ToVersion = version,
                StartedAtUtc = now,
                CompletedAtUtc = now,
                Result = "PASS",
                Details = details
            };
        }

        private sealed class CompleteProductScopeProbe : IDatabaseSchemaProbe
        {
            public Task<DatabaseSchemaProbeResult> ProbeAsync(
                TenantDatabaseDescriptor descriptor,
                string scope,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyCollection<string> expectedTables = new ProductScopeInventory().GetExpectedTables(DatabaseScopes.ProductosServicios);
                return Task.FromResult(new DatabaseSchemaProbeResult
                {
                    Scope = scope,
                    ExpectedTables = expectedTables,
                    ExistingScopeTables = expectedTables,
                    Evidence = new[] { "Scope completo simulado." }
                });
            }
        }

        private sealed class InMemorySchemaVersionRepository : ISchemaVersionRepository
        {
            private readonly object _sync = new();
            private readonly bool _conflict;
            private readonly Dictionary<(string DatabaseIdentityKey, string Scope), SchemaControlState> _states = new();
            private readonly List<SchemaControlHistory> _history = new();
            private readonly List<SchemaControlAttempt> _attempts = new();
            private readonly HashSet<string> _controlObjects = new(StringComparer.OrdinalIgnoreCase);

            public InMemorySchemaVersionRepository(bool conflict = false)
            {
                _conflict = conflict;
            }

            public IReadOnlyCollection<SchemaControlState> States => _states.Values.ToArray();
            public IReadOnlyCollection<string> ControlObjects => _controlObjects.ToArray();

            public Task<SchemaControlInfrastructureResult> EnsureSchemaControlInfrastructureAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    if (_conflict)
                    {
                        return Task.FromResult(new SchemaControlInfrastructureResult
                        {
                            Status = SchemaControlInfrastructureStatus.Conflict,
                            Conflicts = new[] { "CONTROL_SCHEMA_CONFLICT: dbo.CheckAppSchemaState incompatible" }
                        });
                    }

                    string[] required = { "dbo.CheckAppSchemaState", "dbo.CheckAppSchemaHistory", "dbo.CheckAppSchemaAttempts" };
                    string[] created = required.Where(item => _controlObjects.Add(item)).ToArray();
                    string[] verified = required.Where(item => !created.Contains(item, StringComparer.OrdinalIgnoreCase)).ToArray();

                    return Task.FromResult(new SchemaControlInfrastructureResult
                    {
                        Status = SchemaControlInfrastructureStatus.Ready,
                        CreatedObjects = created,
                        VerifiedObjects = verified
                    });
                }
            }

            public Task<SchemaControlState?> GetStateAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    _states.TryGetValue((identity.Fingerprint.ToUpperInvariant(), scope), out SchemaControlState? state);
                    return Task.FromResult(state);
                }
            }

            public Task<IReadOnlyCollection<SchemaControlHistory>> GetHistoryAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    return Task.FromResult<IReadOnlyCollection<SchemaControlHistory>>(_history
                        .Where(item => item.DatabaseIdentityKey == identity.Fingerprint.ToUpperInvariant() && item.Scope == scope)
                        .ToArray());
                }
            }

            public Task<IReadOnlyCollection<SchemaControlAttempt>> GetAttemptsAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    return Task.FromResult<IReadOnlyCollection<SchemaControlAttempt>>(_attempts
                        .Where(item => item.DatabaseIdentityKey == identity.Fingerprint.ToUpperInvariant() && item.Scope == scope)
                        .ToArray());
                }
            }

            public Task<Guid> BeginAttemptAsync(TenantDatabaseDescriptor descriptor, SchemaControlAttempt attempt, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    Guid id = attempt.AttemptId == Guid.Empty ? Guid.NewGuid() : attempt.AttemptId;
                    _attempts.Add(new SchemaControlAttempt
                    {
                        AttemptId = id,
                        DatabaseIdentityKey = attempt.DatabaseIdentityKey.ToUpperInvariant(),
                        Scope = attempt.Scope,
                        OperationType = attempt.OperationType,
                        FromVersion = attempt.FromVersion,
                        ToVersion = attempt.ToVersion,
                        StartedAtUtc = attempt.StartedAtUtc,
                        Result = attempt.Result
                    });
                    return Task.FromResult(id);
                }
            }

            public Task CompleteAttemptAsync(TenantDatabaseDescriptor descriptor, Guid attemptId, string result, string reasonCode, string? error, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    int index = _attempts.FindIndex(item => item.AttemptId == attemptId);
                    SchemaControlAttempt original = _attempts[index];
                    _attempts[index] = new SchemaControlAttempt
                    {
                        AttemptId = original.AttemptId,
                        DatabaseIdentityKey = original.DatabaseIdentityKey,
                        Scope = original.Scope,
                        OperationType = original.OperationType,
                        FromVersion = original.FromVersion,
                        ToVersion = original.ToVersion,
                        StartedAtUtc = original.StartedAtUtc,
                        CompletedAtUtc = DateTime.UtcNow,
                        Result = result,
                        ReasonCode = reasonCode,
                        Error = Sanitize(error)
                    };
                    return Task.CompletedTask;
                }
            }

            public Task RecordHistoryAsync(TenantDatabaseDescriptor descriptor, SchemaControlHistory history, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    _history.Add(new SchemaControlHistory
                    {
                        HistoryId = history.HistoryId,
                        DatabaseIdentityKey = history.DatabaseIdentityKey.ToUpperInvariant(),
                        Scope = history.Scope,
                        EventType = history.EventType,
                        FromVersion = history.FromVersion,
                        ToVersion = history.ToVersion,
                        OperationId = history.OperationId,
                        StartedAtUtc = history.StartedAtUtc,
                        CompletedAtUtc = history.CompletedAtUtc,
                        Result = history.Result,
                        Details = Sanitize(history.Details)
                    });
                    return Task.CompletedTask;
                }
            }

            public Task SetConfirmedStateAsync(TenantDatabaseDescriptor descriptor, SchemaControlState state, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    _states[(state.DatabaseIdentityKey.ToUpperInvariant(), state.Scope)] = new SchemaControlState
                    {
                        DatabaseIdentityKey = state.DatabaseIdentityKey.ToUpperInvariant(),
                        Scope = state.Scope,
                        CurrentVersion = state.CurrentVersion,
                        BaselineId = state.BaselineId,
                        BaselineVersion = state.BaselineVersion,
                        ManifestHash = state.ManifestHash,
                        LastValidatedAtUtc = state.LastValidatedAtUtc,
                        LastMigratedAtUtc = state.LastMigratedAtUtc,
                        LastResult = state.LastResult,
                        CreatedAtUtc = state.CreatedAtUtc,
                        UpdatedAtUtc = DateTime.UtcNow
                    };
                    return Task.CompletedTask;
                }
            }

            private static string? Sanitize(string? value)
            {
                return value?
                    .Replace("secret", "***", StringComparison.OrdinalIgnoreCase)
                    .Replace("admin", "***", StringComparison.OrdinalIgnoreCase)
                    .Replace("abc", "***", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}

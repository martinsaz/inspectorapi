using System.Reflection;
using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class ProductosServiciosSchemaBootstrapperTests
    {
        private static readonly TenantDatabaseDescriptor DescriptorA = new()
        {
            EmpresaKey = "163",
            IdEmpresa = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ConnectionString = "Server=alias;Database=checkapp;TrustServerCertificate=True"
        };

        private static readonly TenantDatabaseDescriptor DescriptorB = new()
        {
            EmpresaKey = "164",
            IdEmpresa = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ConnectionString = "Server=alias;Database=checkapp;TrustServerCertificate=True"
        };

        private static readonly DatabaseIdentity IdentityA = new("SERVER-A", "SERVER-A", string.Empty, "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly DatabaseIdentity IdentityB = new("SERVER-B", "SERVER-B", string.Empty, "CHECKAPP", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        [Fact]
        public async Task EmptyScope_ProvisionsLatestPass()
        {
            Harness harness = new();

            SchemaProvisionResult result = await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(SchemaProvisionResultStatus.Provisioned, result.Status);
            Assert.Equal("PROVISIONED", result.ReasonCode);
            Assert.Equal(ProductosServiciosSchemaContractProvider.LatestVersion, result.ContractVersion);
        }

        [Fact]
        public async Task EmptyScope_CreatesTwentyTables()
        {
            SchemaProvisionResult result = await new Harness().Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(20, result.TablesCreated);
        }

        [Fact]
        public async Task EmptyScope_CreatesTwoHundredFiftyFiveColumns()
        {
            SchemaProvisionResult result = await new Harness().Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(255, result.ColumnsCreated);
        }

        [Fact]
        public async Task EmptyScope_CreatesFiftyIndexes()
        {
            SchemaProvisionResult result = await new Harness().Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(50, result.IndexesCreated);
        }

        [Fact]
        public async Task EmptyScope_CreatesTwentyFourForeignKeys()
        {
            SchemaProvisionResult result = await new Harness().Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(24, result.ForeignKeysCreated);
        }

        [Fact]
        public async Task EmptyScope_CreatesFourteenChecks()
        {
            SchemaProvisionResult result = await new Harness().Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(14, result.ChecksCreated);
        }

        [Fact]
        public void GeneratedDdl_PreservesTypesLengthsPrecisionNullabilityAndDefaults()
        {
            IReadOnlyCollection<string> sql = new SchemaProvisionSqlGenerator().CreateProvisioningCommands(Contract());
            string joined = string.Join('\n', sql);

            Assert.Contains("[Codigo] NVARCHAR(50) NOT NULL", joined);
            Assert.Contains("[PrecioPublico] DECIMAL(18,2) NOT NULL", joined);
            Assert.Contains("[FactorConversion] DECIMAL(28,12) NULL", joined);
            Assert.Contains("DEFAULT ((1))", joined);
            Assert.Contains("[NombreNormalizado] AS UPPER(LTRIM(RTRIM(Nombre)))", joined);
        }

        [Fact]
        public void GeneratedDdl_PreservesUniqueForeignKeysChecksAndFilteredIndexes()
        {
            IReadOnlyCollection<string> sql = new SchemaProvisionSqlGenerator().CreateProvisioningCommands(Contract());
            string joined = string.Join('\n', sql);

            Assert.Contains("CREATE UNIQUE NONCLUSTERED INDEX [UX_ProductosServicios_Empresa_Codigo]", joined);
            Assert.Contains("FOREIGN KEY ([idEmpresa], [idProductoServicio]) REFERENCES [dbo].[ProductosServicios] ([idEmpresa], [id])", joined);
            Assert.Contains("CONSTRAINT [CK_ProductosServicios_Tipo] CHECK (Tipo IN (1, 2))", joined);
            Assert.Contains("WHERE Activo = 1 AND EsPredeterminada = 1", joined);
        }

        [Fact]
        public void PhysicalValidator_NormalizesSqlServerParenthesesInDefinitions()
        {
            var method = typeof(SchemaContractPhysicalValidator).GetMethod("SameDefinition", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);
            Assert.True((bool)method!.Invoke(null, new object[] { "((0))", "(0)" })!);
            Assert.True((bool)method.Invoke(null, new object[] { "([Activo]=(1) AND [EsPredeterminada]=(1))", "Activo = 1 AND EsPredeterminada = 1" })!);
            Assert.True((bool)method.Invoke(null, new object[] { "([ClaveSistema] IS NOT NULL)", "ClaveSistema IS NOT NULL" })!);
        }

        [Fact]
        public async Task FinalHash_MatchesLatestContract()
        {
            SchemaProvisionResult result = await new Harness().Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(new SchemaManifestProvider().CreateManifest(Contract()).ManifestHash, result.ManifestHash);
        }

        [Fact]
        public async Task StateLatest_IsWrittenOnlyAfterValidation()
        {
            Harness harness = new();

            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.True(harness.Repository.StateWrittenAfterExecutor);
        }

        [Fact]
        public async Task HistoryProvisioned_IsWrittenOnlyOnPass()
        {
            Harness harness = new();

            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Single(harness.Repository.History, item => item.EventType == "PROVISIONED" && item.Result == "PASS");
        }

        [Fact]
        public async Task AttemptPass_IsRecorded()
        {
            Harness harness = new();

            SchemaProvisionResult result = await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal("PASS", Assert.Single(harness.Repository.Attempts).Result);
            Assert.Equal(result.AttemptId, harness.Repository.Attempts.Single().AttemptId);
        }

        [Fact]
        public async Task FailureBeforeComplete_RecordsAttemptFail()
        {
            Harness harness = new(new FakeExecutorMode { Throw = true });

            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal("FAIL", Assert.Single(harness.Repository.Attempts).Result);
        }

        [Fact]
        public async Task Failure_DoesNotConfirmStateV1()
        {
            Harness harness = new(new FakeExecutorMode { ValidationFails = true });

            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Null(harness.Repository.States.SingleOrDefault()?.CurrentVersion);
        }

        [Fact]
        public async Task Failure_DoesNotWriteProvisionedPassHistory()
        {
            Harness harness = new(new FakeExecutorMode { ValidationFails = true });

            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.DoesNotContain(harness.Repository.History, item => item.EventType == "PROVISIONED" && item.Result == "PASS");
        }

        [Fact]
        public async Task SecondExecution_DoesNotDuplicateObjects()
        {
            Harness harness = new();

            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);
            SchemaProvisionResult second = await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(SchemaProvisionResultStatus.NoProvision, second.Status);
            Assert.Equal("ALREADY_PROVISIONED", second.ReasonCode);
            Assert.Equal(1, harness.Executor.ProvisionCalls);
            Assert.Single(harness.Repository.History);
        }

        [Fact]
        public async Task PartialScope_IsRejected()
        {
            Harness harness = new(initialState: DatabaseStructureState.Partial);

            SchemaProvisionResult result = await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal("PROVISION_NOT_ALLOWED_PARTIAL", result.ReasonCode);
            Assert.Equal(0, harness.Executor.ProvisionCalls);
        }

        [Fact]
        public async Task CurrentScope_IsRejectedAsAlreadyProvisioned()
        {
            Harness harness = new(initialState: DatabaseStructureState.Current);

            SchemaProvisionResult result = await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal("ALREADY_PROVISIONED", result.ReasonCode);
            Assert.Equal(0, harness.Executor.ProvisionCalls);
        }

        [Theory]
        [InlineData(DatabaseStructureState.Outdated, "PROVISION_NOT_ALLOWED_OUTDATED")]
        [InlineData(DatabaseStructureState.Future, "PROVISION_NOT_ALLOWED_FUTURE")]
        [InlineData(DatabaseStructureState.Unknown, "PROVISION_NOT_ALLOWED_UNKNOWN")]
        public async Task NonEmptyUnsafeStates_AreRejected(DatabaseStructureState state, string reason)
        {
            Harness harness = new(initialState: state);

            SchemaProvisionResult result = await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(reason, result.ReasonCode);
            Assert.Equal(0, harness.Executor.ProvisionCalls);
        }

        [Fact]
        public async Task Unavailable_IsRejected()
        {
            Harness harness = new(unavailable: true);

            SchemaProvisionResult result = await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal("PROVISION_NOT_ALLOWED_UNAVAILABLE", result.ReasonCode);
            Assert.Equal(0, harness.Executor.ProvisionCalls);
        }

        [Fact]
        public async Task Concurrency_AllowsOnlyOneEffectiveProvision()
        {
            Harness harness = new(new FakeExecutorMode { DelayMs = 50 });

            SchemaProvisionResult[] results = await Task.WhenAll(
                harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios),
                harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios));

            Assert.Equal(1, results.Count(item => item.Status == SchemaProvisionResultStatus.Provisioned));
            Assert.Equal(1, harness.Executor.ProvisionCalls);
            Assert.Single(harness.Repository.States, item => item.CurrentVersion == ProductosServiciosSchemaContractProvider.LatestVersion);
        }

        [Fact]
        public async Task TwoCompaniesSameDatabaseIdentity_ProvisionOnce()
        {
            Harness harness = new();

            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);
            SchemaProvisionResult second = await harness.Bootstrapper.ProvisionScopeAsync(DescriptorB, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal("ALREADY_PROVISIONED", second.ReasonCode);
            Assert.Equal(1, harness.Executor.ProvisionCalls);
            Assert.Single(harness.Repository.States);
        }

        [Fact]
        public async Task DifferentDatabaseIdentities_ProvisionIndependently()
        {
            Harness harness = new();

            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);
            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorB, IdentityB, DatabaseScopes.ProductosServicios);

            Assert.Equal(2, harness.Executor.ProvisionCalls);
            Assert.Equal(2, harness.Repository.States.Count(item => item.CurrentVersion == ProductosServiciosSchemaContractProvider.LatestVersion));
        }

        [Fact]
        public async Task Bootstrap_DoesNotSeedBusinessData()
        {
            Harness harness = new();

            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(0, harness.Executor.BusinessSeedRowsInserted);
        }

        [Fact]
        public async Task Bootstrap_DoesNotExposeSecrets()
        {
            Harness harness = new(new FakeExecutorMode { Throw = true, Error = "Password=secret;User Id=admin;Token=abc" });

            SchemaProvisionResult result = await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);
            string material = string.Join(' ', result.Warnings.Concat(harness.Repository.Attempts.Select(a => a.Error ?? string.Empty)));

            Assert.DoesNotContain("secret", material, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("admin", material, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("abc", material, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task FailureResidue_IsDetectedAsPartialOnNextExecution()
        {
            Harness harness = new(new FakeExecutorMode { ValidationFails = true, LeavesPartialResidue = true });

            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);
            SchemaProvisionResult second = await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal("PROVISION_NOT_ALLOWED_PARTIAL", second.ReasonCode);
        }

        [Fact]
        public async Task T11ThroughT15ArePreservedByUsingClassifierRepositoryContractAndManifest()
        {
            Harness harness = new();

            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.True(harness.Classifier.WasCalled);
            Assert.True(harness.Repository.InfrastructureEnsured);
            Assert.Equal(Contract().ContractVersion, harness.Repository.States.Single().CurrentVersion);
            Assert.Equal(new SchemaManifestProvider().CreateManifest(Contract()).ManifestHash, harness.Repository.States.Single().ManifestHash);
        }

        private static SchemaContract Contract() => new ProductosServiciosSchemaContractProvider().GetContract(DatabaseScopes.ProductosServicios);

        private sealed class Harness
        {
            public Harness(FakeExecutorMode? executorMode = null, DatabaseStructureState initialState = DatabaseStructureState.Empty, bool unavailable = false)
            {
                Repository = new FakeSchemaVersionRepository();
                Executor = new FakeProvisionExecutor(Repository, executorMode ?? new FakeExecutorMode());
                Classifier = new FakeClassifier(Repository, initialState, unavailable);
                Bootstrapper = new ProductosServiciosSchemaBootstrapper(
                    Classifier,
                    Repository,
                    new ProductosServiciosSchemaContractProvider(),
                    new SchemaManifestProvider(),
                    Executor,
                    new FakeProvisionLock());
            }

            public ProductosServiciosSchemaBootstrapper Bootstrapper { get; }
            public FakeSchemaVersionRepository Repository { get; }
            public FakeProvisionExecutor Executor { get; }
            public FakeClassifier Classifier { get; }
        }

        private sealed class FakeExecutorMode
        {
            public bool Throw { get; init; }
            public bool ValidationFails { get; init; }
            public bool LeavesPartialResidue { get; init; }
            public int DelayMs { get; init; }
            public string Error { get; init; } = "SIMULATED_FAILURE";
        }

        private sealed class FakeProvisionExecutor : ISchemaProvisionExecutor
        {
            private readonly FakeSchemaVersionRepository _repository;
            private readonly FakeExecutorMode _mode;
            private int _calls;

            public FakeProvisionExecutor(FakeSchemaVersionRepository repository, FakeExecutorMode mode)
            {
                _repository = repository;
                _mode = mode;
            }

            public int ProvisionCalls => _calls;
            public int BusinessSeedRowsInserted { get; private set; }

            public async Task<SchemaProvisionExecutionResult> ProvisionAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, SchemaContract contract, SchemaManifest manifest, CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref _calls);
                if (_mode.DelayMs > 0)
                {
                    await Task.Delay(_mode.DelayMs, cancellationToken);
                }

                if (_mode.Throw)
                {
                    throw new InvalidOperationException(_mode.Error);
                }

                if (_mode.LeavesPartialResidue)
                {
                    _repository.MarkPartial(identity, contract.Scope);
                }

                if (_mode.ValidationFails)
                {
                    return new SchemaProvisionExecutionResult
                    {
                        Succeeded = false,
                        ReasonCode = "PROVISION_VALIDATION_FAILED",
                        Discrepancies = new[] { "TABLE_MISSING dbo.ProductosServicios" }
                    };
                }

                return new SchemaProvisionExecutionResult
                {
                    Succeeded = true,
                    ReasonCode = "PROVISION_VALIDATED",
                    TablesCreated = contract.Tables.Count,
                    ColumnsCreated = contract.Tables.Sum(table => table.Columns.Count),
                    IndexesCreated = contract.Tables.Sum(table => table.Indexes.Count),
                    ForeignKeysCreated = contract.Tables.Sum(table => table.ForeignKeys.Count),
                    ChecksCreated = contract.Tables.Sum(table => table.CheckConstraints.Count)
                };
            }
        }

        private sealed class FakeClassifier : IDatabaseStateClassifier
        {
            private readonly FakeSchemaVersionRepository _repository;
            private readonly DatabaseStructureState _initialState;
            private readonly bool _unavailable;

            public FakeClassifier(FakeSchemaVersionRepository repository, DatabaseStructureState initialState, bool unavailable)
            {
                _repository = repository;
                _initialState = initialState;
                _unavailable = unavailable;
            }

            public bool WasCalled { get; private set; }

            public Task<DatabaseClassificationResult> ClassifyAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                WasCalled = true;
                DatabaseStructureState state = _repository.StateFor(identity, scope) ?? _initialState;
                bool available = !_unavailable;
                return Task.FromResult(new DatabaseClassificationResult
                {
                    Identity = identity,
                    SanitizedIdentity = identity.ToSanitizedString(),
                    Scope = scope,
                    State = available ? state : DatabaseStructureState.Unknown,
                    IsAvailable = available,
                    ReasonCode = available ? state.ToString().ToUpperInvariant() : "UNAVAILABLE",
                    ExpectedScopeTableCount = 20,
                    ExistingScopeTableCount = state switch
                    {
                        DatabaseStructureState.Empty => 0,
                        DatabaseStructureState.Partial => 8,
                        _ => 20
                    }
                });
            }
        }

        private sealed class FakeProvisionLock : ISchemaProvisionLock
        {
            private readonly SemaphoreSlim _semaphore = new(1, 1);

            public async Task<T> ExecuteAsync<T>(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await action(cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        private sealed class FakeSchemaVersionRepository : ISchemaVersionRepository
        {
            private readonly Dictionary<(string, string), SchemaControlState> _states = new();
            private readonly HashSet<(string, string)> _partialResidues = new();
            private readonly object _sync = new();

            public bool InfrastructureEnsured { get; private set; }
            public bool StateWrittenAfterExecutor { get; private set; }
            public List<SchemaControlAttempt> Attempts { get; } = new();
            public List<SchemaControlHistory> History { get; } = new();
            public IReadOnlyCollection<SchemaControlState> States => _states.Values.ToArray();

            public DatabaseStructureState? StateFor(DatabaseIdentity identity, string scope)
            {
                lock (_sync)
                {
                    if (_partialResidues.Contains(Key(identity, scope))) return DatabaseStructureState.Partial;
                    return _states.TryGetValue(Key(identity, scope), out SchemaControlState? state) && state.CurrentVersion == ProductosServiciosSchemaContractProvider.LatestVersion
                        ? DatabaseStructureState.Current
                        : null;
                }
            }

            public void MarkPartial(DatabaseIdentity identity, string scope)
            {
                lock (_sync)
                {
                    _partialResidues.Add(Key(identity, scope));
                }
            }

            public Task<SchemaControlInfrastructureResult> EnsureSchemaControlInfrastructureAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default)
            {
                InfrastructureEnsured = true;
                return Task.FromResult(new SchemaControlInfrastructureResult { Status = SchemaControlInfrastructureStatus.Ready });
            }

            public Task<SchemaControlState?> GetStateAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    _states.TryGetValue(Key(identity, scope), out SchemaControlState? state);
                    return Task.FromResult(state);
                }
            }

            public Task<IReadOnlyCollection<SchemaControlHistory>> GetHistoryAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyCollection<SchemaControlHistory>>(History.Where(item => item.DatabaseIdentityKey == identity.Fingerprint).ToArray());
            }

            public Task<IReadOnlyCollection<SchemaControlAttempt>> GetAttemptsAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyCollection<SchemaControlAttempt>>(Attempts.Where(item => item.DatabaseIdentityKey == identity.Fingerprint).ToArray());
            }

            public Task<Guid> BeginAttemptAsync(TenantDatabaseDescriptor descriptor, SchemaControlAttempt attempt, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    Guid id = Guid.NewGuid();
                    Attempts.Add(new SchemaControlAttempt
                    {
                        AttemptId = id,
                        DatabaseIdentityKey = attempt.DatabaseIdentityKey,
                        Scope = attempt.Scope,
                        OperationType = attempt.OperationType,
                        FromVersion = attempt.FromVersion,
                        ToVersion = attempt.ToVersion,
                        StartedAtUtc = attempt.StartedAtUtc,
                        CompletedAtUtc = attempt.CompletedAtUtc,
                        Result = attempt.Result,
                        ReasonCode = attempt.ReasonCode,
                        Error = attempt.Error
                    });
                    return Task.FromResult(id);
                }
            }

            public Task CompleteAttemptAsync(TenantDatabaseDescriptor descriptor, Guid attemptId, string result, string reasonCode, string? error, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    int index = Attempts.FindIndex(item => item.AttemptId == attemptId);
                    if (index >= 0)
                    {
                        SchemaControlAttempt existing = Attempts[index];
                        Attempts[index] = new SchemaControlAttempt
                        {
                            AttemptId = existing.AttemptId,
                            DatabaseIdentityKey = existing.DatabaseIdentityKey,
                            Scope = existing.Scope,
                            OperationType = existing.OperationType,
                            FromVersion = existing.FromVersion,
                            ToVersion = existing.ToVersion,
                            StartedAtUtc = existing.StartedAtUtc,
                            CompletedAtUtc = DateTime.UtcNow,
                            Result = result,
                            ReasonCode = reasonCode,
                            Error = Sanitize(error)
                        };
                    }
                }

                return Task.CompletedTask;
            }

            public Task RecordHistoryAsync(TenantDatabaseDescriptor descriptor, SchemaControlHistory history, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    History.Add(history);
                }

                return Task.CompletedTask;
            }

            public Task SetConfirmedStateAsync(TenantDatabaseDescriptor descriptor, SchemaControlState state, CancellationToken cancellationToken = default)
            {
                lock (_sync)
                {
                    if (state.CurrentVersion == ProductosServiciosSchemaContractProvider.LatestVersion) StateWrittenAfterExecutor = true;
                    _states[Key(state.DatabaseIdentityKey, state.Scope)] = state;
                }

                return Task.CompletedTask;
            }

            private static (string, string) Key(DatabaseIdentity identity, string scope) => (identity.Fingerprint.ToUpperInvariant(), scope);
            private static (string, string) Key(string identityKey, string scope) => (identityKey.ToUpperInvariant(), scope);
            private static string? Sanitize(string? value)
            {
                if (string.IsNullOrWhiteSpace(value)) return value;
                return value
                    .Replace("secret", "***", StringComparison.OrdinalIgnoreCase)
                    .Replace("admin", "***", StringComparison.OrdinalIgnoreCase)
                    .Replace("abc", "***", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}

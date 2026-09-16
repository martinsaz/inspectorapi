using System.Data.SqlClient;
using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class SchemaOperationLockTests
    {
        private static readonly DatabaseIdentity IdentityA = new("SERVER-A", "SERVER-A", string.Empty, "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly DatabaseIdentity IdentityAlias = new("SERVER-A", "SERVER-A", string.Empty, "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly DatabaseIdentity IdentityB = new("SERVER-B", "SERVER-B", string.Empty, "CHECKAPP_B", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly TenantDatabaseDescriptor DescriptorA = new() { EmpresaKey = "A", IdEmpresa = Guid.Parse("11111111-1111-1111-1111-111111111111"), ConnectionString = "Server=alias-a;Database=db;" };
        private static readonly TenantDatabaseDescriptor DescriptorB = new() { EmpresaKey = "B", IdEmpresa = Guid.Parse("22222222-2222-2222-2222-222222222222"), ConnectionString = "Server=alias-b;Database=db;" };

        [Fact]
        public void Resource_IsDatabaseIdentityScope_NotEmpresaOrConnectionString()
        {
            SqlSchemaOperationLock schemaLock = new(new FakeConnectionFactory());

            string resourceA = schemaLock.BuildResource(IdentityA, DatabaseScopes.ProductosServicios, SchemaOperationLockKind.Scope);
            string resourceAlias = schemaLock.BuildResource(IdentityAlias, DatabaseScopes.ProductosServicios, SchemaOperationLockKind.Scope);
            string resourceB = schemaLock.BuildResource(IdentityB, DatabaseScopes.ProductosServicios, SchemaOperationLockKind.Scope);

            Assert.Equal(resourceA, resourceAlias);
            Assert.NotEqual(resourceA, resourceB);
            Assert.Contains("CheckApp.Schema.ProductosServicios", resourceA);
            Assert.DoesNotContain(DescriptorA.IdEmpresa.ToString(), resourceA, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("alias-a", resourceA, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ControlResource_IsStableAndDistinctFromScope()
        {
            SqlSchemaOperationLock schemaLock = new(new FakeConnectionFactory());

            string control = schemaLock.BuildResource(IdentityA, DatabaseScopes.ProductosServicios, SchemaOperationLockKind.Control);
            string scope = schemaLock.BuildResource(IdentityA, DatabaseScopes.ProductosServicios, SchemaOperationLockKind.Scope);

            Assert.Contains("CheckApp.Schema.Control", control);
            Assert.Contains("CheckApp.Schema.ProductosServicios", scope);
            Assert.NotEqual(control, scope);
        }

        [Fact]
        public async Task Adapter_AcquiresControlBeforeScopeAndReleasesInReverseFinally()
        {
            RecordingLock schemaLock = new();

            string result = await schemaLock.ExecuteWithControlAndScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios, _ => Task.FromResult("ok"));

            Assert.Equal("ok", result);
            Assert.Equal(new[] { "Acquire:Control", "Acquire:ProductosServicios", "Release:ProductosServicios", "Release:Control" }, schemaLock.Events);
        }

        [Fact]
        public async Task Release_HappensOnException()
        {
            RecordingLock schemaLock = new();

            await Assert.ThrowsAsync<InvalidOperationException>(() => schemaLock.ExecuteWithControlAndScopeAsync<string>(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios, _ => throw new InvalidOperationException("boom")));

            Assert.Contains("Release:ProductosServicios", schemaLock.Events);
            Assert.Contains("Release:Control", schemaLock.Events);
        }

        [Theory]
        [InlineData(SchemaOperationLockResultCode.Timeout, "LOCK_TIMEOUT")]
        [InlineData(SchemaOperationLockResultCode.Cancelled, "LOCK_CANCELLED")]
        [InlineData(SchemaOperationLockResultCode.Deadlock, "LOCK_DEADLOCK")]
        [InlineData(SchemaOperationLockResultCode.Failed, "LOCK_FAILED")]
        public async Task Bootstrap_LockFailure_DoesNotExecuteDdlOrWriteState(SchemaOperationLockResultCode code, string reason)
        {
            Harness harness = new(new ThrowingProvisionLock(code, reason));

            SchemaProvisionResult result = await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal(SchemaProvisionResultStatus.NoProvision, result.Status);
            Assert.Equal(reason, result.ReasonCode);
            Assert.Equal(0, harness.Executor.ProvisionCalls);
            Assert.Empty(harness.Repository.History);
            Assert.Empty(harness.Repository.States);
            Assert.Empty(harness.Repository.Attempts);
        }

        [Fact]
        public async Task Bootstrap_RereadsClassificationAfterLockBeforeDdl()
        {
            Harness harness = new(new InlineProvisionLock()) { LockedState = DatabaseStructureState.Current };

            SchemaProvisionResult result = await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);

            Assert.Equal("ALREADY_PROVISIONED", result.ReasonCode);
            Assert.Equal(0, harness.Executor.ProvisionCalls);
            Assert.True(harness.Classifier.Calls >= 2);
        }

        [Fact]
        public async Task Bootstrap_TenRetriesAfterSuccess_DoNotDuplicateHistoryStateOrDdl()
        {
            Harness harness = new(new InlineProvisionLock());

            for (int i = 0; i < 10; i++)
            {
                await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);
            }

            Assert.Equal(1, harness.Executor.ProvisionCalls);
            Assert.Single(harness.Repository.History.Where(x => x.EventType == "PROVISIONED" && x.Result == "PASS"));
            Assert.Single(harness.Repository.States.Where(x => x.CurrentVersion == 1));
        }

        [Fact]
        public async Task TwoCompaniesSameDatabaseIdentity_UseSameLockAndProvisionOnce()
        {
            InlineProvisionLock provisionLock = new();
            Harness harness = new(provisionLock);

            await Task.WhenAll(
                harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios),
                harness.Bootstrapper.ProvisionScopeAsync(DescriptorB, IdentityA, DatabaseScopes.ProductosServicios));

            Assert.Equal(1, harness.Executor.ProvisionCalls);
            Assert.DoesNotContain(provisionLock.Resources, r => r.Contains(DescriptorA.IdEmpresa.ToString(), StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(provisionLock.Resources, r => r.Contains(DescriptorB.IdEmpresa.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task TwoDifferentDatabaseIdentities_ProvisionIndependently()
        {
            Harness harness = new(new InlineProvisionLock());

            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorA, IdentityA, DatabaseScopes.ProductosServicios);
            await harness.Bootstrapper.ProvisionScopeAsync(DescriptorB, IdentityB, DatabaseScopes.ProductosServicios);

            Assert.Equal(2, harness.Executor.ProvisionCalls);
            Assert.Equal(2, harness.Repository.States.Count);
        }

        private sealed class FakeConnectionFactory : ITenantSqlConnectionFactory
        {
            public SqlConnection CreateConnection(TenantDatabaseDescriptor descriptor) => new(descriptor.ConnectionString);
        }

        private sealed class RecordingLock : ISchemaOperationLock
        {
            public List<string> Events { get; } = new();
            public string BuildResource(DatabaseIdentity identity, string scope, SchemaOperationLockKind kind) => $"CheckApp.Schema.{(kind == SchemaOperationLockKind.Control ? "Control" : scope)}:{identity.Fingerprint}";
            public Task<SchemaOperationLockHandle> AcquireAsync(SqlConnection connection, SchemaOperationLockRequest request, CancellationToken cancellationToken = default, SqlTransaction? transaction = null)
            {
                string name = request.Kind == SchemaOperationLockKind.Control ? "Control" : request.Scope;
                Events.Add("Acquire:" + name);
                return Task.FromResult(new SchemaOperationLockHandle(BuildResource(request.Identity, request.Scope, request.Kind), SchemaOperationLockResultCode.Acquired, 0, _ => { Events.Add("Release:" + name); return Task.CompletedTask; }));
            }
            public async Task<T> ExecuteWithControlAndScopeAsync<T>(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
            {
                await using SchemaOperationLockHandle control = await AcquireAsync(new SqlConnection(), SchemaOperationLockRequest.Control(identity), cancellationToken);
                await using SchemaOperationLockHandle scopeLock = await AcquireAsync(new SqlConnection(), SchemaOperationLockRequest.ScopeLock(identity, scope), cancellationToken);
                return await action(cancellationToken);
            }
        }

        private sealed class ThrowingProvisionLock : ISchemaProvisionLock
        {
            private readonly SchemaOperationLockResultCode _code;
            private readonly string _reason;
            public ThrowingProvisionLock(SchemaOperationLockResultCode code, string reason) { _code = code; _reason = reason; }
            public Task<T> ExecuteAsync<T>(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default) =>
                throw new SchemaOperationLockException(_code, _reason, "CheckApp.Schema.ProductosServicios:" + identity.Fingerprint, -1);
        }

        private sealed class InlineProvisionLock : ISchemaProvisionLock
        {
            private readonly Dictionary<string, SemaphoreSlim> _locks = new();
            public List<string> Resources { get; } = new();
            public async Task<T> ExecuteAsync<T>(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
            {
                string resource = $"CheckApp.Schema.{scope}:{identity.Fingerprint}";
                Resources.Add(resource);
                SemaphoreSlim semaphore;
                lock (_locks)
                {
                    if (!_locks.TryGetValue(resource, out semaphore!))
                    {
                        semaphore = new SemaphoreSlim(1, 1);
                        _locks[resource] = semaphore;
                    }
                }
                await semaphore.WaitAsync(cancellationToken);
                try { return await action(cancellationToken); }
                finally { semaphore.Release(); }
            }
        }

        private sealed class Harness
        {
            public Harness(ISchemaProvisionLock schemaLock)
            {
                Repository = new FakeRepository();
                Classifier = new FakeClassifier(this);
                Executor = new FakeExecutor(this);
                Bootstrapper = new ProductosServiciosSchemaBootstrapper(Classifier, Repository, new ProductosServiciosSchemaContractProvider(), new SchemaManifestProvider(), Executor, schemaLock);
            }

            public ProductosServiciosSchemaBootstrapper Bootstrapper { get; }
            public FakeRepository Repository { get; }
            public FakeClassifier Classifier { get; }
            public FakeExecutor Executor { get; }
            public DatabaseStructureState InitialState { get; init; } = DatabaseStructureState.Empty;
            public DatabaseStructureState? LockedState { get; init; }

            public sealed class FakeClassifier : IDatabaseStateClassifier
            {
                private readonly Harness _harness;
                public int Calls { get; private set; }
                public FakeClassifier(Harness harness) => _harness = harness;
                public Task<DatabaseClassificationResult> ClassifyAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
                {
                    Calls++;
                    DatabaseStructureState state = Calls == 1 ? _harness.InitialState : _harness.LockedState ?? (_harness.Repository.States.Any(s => s.DatabaseIdentityKey == identity.Fingerprint) ? DatabaseStructureState.Current : DatabaseStructureState.Empty);
                    return Task.FromResult(new DatabaseClassificationResult { Identity = identity, SanitizedIdentity = identity.ToSanitizedString(), Scope = scope, State = state, IsAvailable = true });
                }
            }

            public sealed class FakeExecutor : ISchemaProvisionExecutor
            {
                private readonly Harness _harness;
                public int ProvisionCalls { get; private set; }
                public FakeExecutor(Harness harness) => _harness = harness;
                public Task<SchemaProvisionExecutionResult> ProvisionAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, SchemaContract contract, SchemaManifest manifest, CancellationToken cancellationToken = default)
                {
                    ProvisionCalls++;
                    return Task.FromResult(new SchemaProvisionExecutionResult { Succeeded = true, ReasonCode = "PROVISION_VALIDATED", TablesCreated = contract.Tables.Count, ColumnsCreated = contract.Tables.Sum(t => t.Columns.Count), IndexesCreated = contract.Tables.Sum(t => t.Indexes.Count), ForeignKeysCreated = contract.Tables.Sum(t => t.ForeignKeys.Count), ChecksCreated = contract.Tables.Sum(t => t.CheckConstraints.Count) });
                }
            }

            public sealed class FakeRepository : ISchemaVersionRepository
            {
                public List<SchemaControlState> States { get; } = new();
                public List<SchemaControlHistory> History { get; } = new();
                public List<SchemaControlAttempt> Attempts { get; } = new();
                public Task<SchemaControlInfrastructureResult> EnsureSchemaControlInfrastructureAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default) => Task.FromResult(new SchemaControlInfrastructureResult { Status = SchemaControlInfrastructureStatus.Ready });
                public Task<SchemaControlState?> GetStateAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default) => Task.FromResult(States.LastOrDefault(s => s.DatabaseIdentityKey == identity.Fingerprint && s.Scope == scope));
                public Task<IReadOnlyCollection<SchemaControlHistory>> GetHistoryAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<SchemaControlHistory>>(History.Where(h => h.DatabaseIdentityKey == identity.Fingerprint && h.Scope == scope).ToArray());
                public Task<IReadOnlyCollection<SchemaControlAttempt>> GetAttemptsAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<SchemaControlAttempt>>(Attempts.Where(a => a.DatabaseIdentityKey == identity.Fingerprint && a.Scope == scope).ToArray());
                public Task<Guid> BeginAttemptAsync(TenantDatabaseDescriptor descriptor, SchemaControlAttempt attempt, CancellationToken cancellationToken = default) { Guid id = attempt.AttemptId == Guid.Empty ? Guid.NewGuid() : attempt.AttemptId; Attempts.Add(new SchemaControlAttempt { AttemptId = id, DatabaseIdentityKey = attempt.DatabaseIdentityKey, Scope = attempt.Scope, OperationType = attempt.OperationType, FromVersion = attempt.FromVersion, ToVersion = attempt.ToVersion, StartedAtUtc = attempt.StartedAtUtc, Result = attempt.Result, ReasonCode = attempt.ReasonCode }); return Task.FromResult(id); }
                public Task CompleteAttemptAsync(TenantDatabaseDescriptor descriptor, Guid attemptId, string result, string reasonCode, string? error, CancellationToken cancellationToken = default) { int i = Attempts.FindIndex(a => a.AttemptId == attemptId); if (i >= 0) { SchemaControlAttempt a = Attempts[i]; Attempts[i] = new SchemaControlAttempt { AttemptId = a.AttemptId, DatabaseIdentityKey = a.DatabaseIdentityKey, Scope = a.Scope, OperationType = a.OperationType, FromVersion = a.FromVersion, ToVersion = a.ToVersion, StartedAtUtc = a.StartedAtUtc, CompletedAtUtc = DateTime.UtcNow, Result = result, ReasonCode = reasonCode, Error = error }; } return Task.CompletedTask; }
                public Task RecordHistoryAsync(TenantDatabaseDescriptor descriptor, SchemaControlHistory history, CancellationToken cancellationToken = default) { History.Add(history); return Task.CompletedTask; }
                public Task SetConfirmedStateAsync(TenantDatabaseDescriptor descriptor, SchemaControlState state, CancellationToken cancellationToken = default) { States.RemoveAll(s => s.DatabaseIdentityKey == state.DatabaseIdentityKey && s.Scope == state.Scope); States.Add(state); return Task.CompletedTask; }
            }
        }
    }
}

using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class SchemaMigrationEngineTests
    {
        private static readonly TenantDatabaseDescriptor Descriptor = new()
        {
            EmpresaKey = "fixture",
            IdEmpresa = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ConnectionString = "Server=fixture;Database=checkapp;TrustServerCertificate=True"
        };

        private static readonly DatabaseIdentity Identity = new("SERVER", "SERVER", string.Empty, "CHECKAPP", "cccccccc-cccc-cccc-cccc-cccccccccccc");

        [Fact]
        public void CurrentVersionOne_WithOneTransition_ReturnsOnePending()
        {
            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 2, Package(M(1, 2)), Array.Empty<SchemaControlHistory>());

            Assert.Equal(SchemaMigrationResolutionStatus.Ready, result.Status);
            Assert.Equal("PS-M1-2", Assert.Single(result.PendingMigrations).MigrationId);
        }

        [Fact]
        public void CurrentVersionOne_WithThreeStepChain_ReturnsAllInOrder()
        {
            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 4, Package(M(1, 2), M(2, 3), M(3, 4)), Array.Empty<SchemaControlHistory>());

            Assert.Equal(new[] { "PS-M1-2", "PS-M2-3", "PS-M3-4" }, result.PendingMigrations.Select(item => item.MigrationId));
        }

        [Fact]
        public void ChainGap_Blocks()
        {
            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 4, Package(M(1, 2), M(3, 4)), Array.Empty<SchemaControlHistory>());

            Assert.Equal(SchemaMigrationResolutionStatus.ChainGap, result.Status);
        }

        [Fact]
        public void ChainBranch_Blocks()
        {
            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 3, Package(M(1, 2), M(1, 3)), Array.Empty<SchemaControlHistory>());

            Assert.Equal(SchemaMigrationResolutionStatus.ChainBranch, result.Status);
        }

        [Fact]
        public void ChainCycle_Blocks()
        {
            SchemaMigrationPackage package = Package(M(1, 2), M(2, 1));
            package = new SchemaMigrationPackage(package.Release with { LatestSchemaVersion = 3, LatestManifestHash = HashContract(3) }, package.Migrations);

            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 3, package, Array.Empty<SchemaControlHistory>());

            Assert.Equal(SchemaMigrationResolutionStatus.ChainCycle, result.Status);
        }

        [Fact]
        public void FromVersionIncorrect_Blocks()
        {
            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 2, Package(M(2, 3)), Array.Empty<SchemaControlHistory>());

            Assert.Equal(SchemaMigrationResolutionStatus.ChainGap, result.Status);
        }

        [Fact]
        public void BaselineIncompatible_Blocks()
        {
            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 2, Package(M(1, 2) with { BaselineId = "OTHER" }), Array.Empty<SchemaControlHistory>());

            Assert.Equal("BASELINE_INCOMPATIBLE", result.ReasonCode);
        }

        [Fact]
        public void SqlHashModified_BlocksPackage()
        {
            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 2, Package(M(1, 2) with { SqlHash = "bad" }), Array.Empty<SchemaControlHistory>());

            Assert.Equal("SQL_HASH_MISMATCH", result.ReasonCode);
        }

        [Fact]
        public void MigrationIdAlreadyApplied_ReturnsAlreadyApplied()
        {
            SchemaMigrationDefinition migration = M(1, 2);
            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 2, Package(migration), new[] { Applied(migration) });

            Assert.Equal(SchemaMigrationResolutionStatus.AlreadyApplied, result.Status);
        }

        [Fact]
        public async Task Reexecution_DoesNotDuplicateSuccessHistory()
        {
            Harness harness = new(Package(M(1, 2)));

            await harness.Runner.MigrateToLatestAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);
            SchemaMigrationExecutionResult second = await harness.Runner.MigrateToLatestAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal(SchemaMigrationExecutionStatus.NoProvision, second.Status);
            Assert.Single(harness.Repository.History.Where(item => item.EventType == "MIGRATED" && item.Result == "PASS"));
        }

        [Fact]
        public async Task ErrorBeforeDdl_RecordsFailedAttemptAndKeepsState()
        {
            Harness harness = new(Package(M(1, 2))) { ExecutorMode = ExecutorMode.FailBeforeDdl };

            SchemaMigrationExecutionResult result = await harness.Runner.MigrateToLatestAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal(SchemaMigrationExecutionStatus.Failed, result.Status);
            Assert.Equal(1, harness.Repository.State!.CurrentVersion);
            Assert.Contains(harness.Repository.Attempts, item => item.Result == "FAIL");
            Assert.Empty(harness.Repository.History.Where(item => item.Result == "PASS"));
        }

        [Fact]
        public async Task ErrorDuringDdl_RollsBackAndKeepsState()
        {
            Harness harness = new(Package(M(1, 2))) { ExecutorMode = ExecutorMode.FailDuringDdl };

            await harness.Runner.MigrateToLatestAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal(0, harness.AppliedDdlCount);
            Assert.Equal(1, harness.Repository.State!.CurrentVersion);
            Assert.Empty(harness.Repository.History.Where(item => item.Result == "PASS"));
        }

        [Fact]
        public async Task ErrorDuringPostValidation_RollsBackAndDoesNotAdvanceVersion()
        {
            Harness harness = new(Package(M(1, 2))) { ExecutorMode = ExecutorMode.FailValidation };

            SchemaMigrationExecutionResult result = await harness.Runner.MigrateToLatestAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal("MIGRATION_TARGET_VALIDATION_FAILED", result.ReasonCode);
            Assert.Equal(1, harness.Repository.State!.CurrentVersion);
            Assert.Empty(harness.Repository.History.Where(item => item.Result == "PASS"));
        }

        [Fact]
        public async Task LostCommit_ReconcilesWithoutSecondExecution()
        {
            SchemaMigrationDefinition migration = M(1, 2);
            Harness harness = new(Package(migration));
            Guid attemptId = Guid.NewGuid();
            harness.Repository.History.Add(Applied(migration));
            harness.Repository.State = State(2, migration.TargetManifestHash);

            SchemaMigrationExecutionResult result = await harness.Runner.RecoverUncertainCommitAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios, migration.MigrationId, attemptId);

            Assert.Equal(SchemaMigrationExecutionStatus.Recovered, result.Status);
            Assert.Equal(0, harness.ExecutorExecutionCount);
        }

        [Fact]
        public async Task LockOccupied_DoesNotExecuteDdl()
        {
            Harness harness = new(Package(M(1, 2))) { LockThrows = true };

            await Assert.ThrowsAsync<TimeoutException>(() => harness.Runner.MigrateToLatestAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios));
            Assert.Equal(0, harness.ExecutorExecutionCount);
        }

        [Fact]
        public async Task ConcurrentExecutors_OnlyOneAppliesTransition()
        {
            Harness harness = new(Package(M(1, 2)));

            await Task.WhenAll(
                harness.Runner.MigrateToLatestAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios),
                harness.Runner.MigrateToLatestAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios));

            Assert.Equal(1, harness.ExecutorExecutionCount);
            Assert.Single(harness.Repository.History.Where(item => item.EventType == "MIGRATED" && item.Result == "PASS"));
        }

        [Fact]
        public void SqlWithGo_IsRejected()
        {
            SchemaMigrationDefinition migration = M(1, 2) with { UpSql = "CREATE TABLE dbo.X(id int);\nGO" };
            migration = migration with { SqlHash = SchemaMigrationHash.Sha256(migration.UpSql) };

            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 2, Package(migration), Array.Empty<SchemaControlHistory>());

            Assert.Equal("SQL_BATCH_GO_NOT_ALLOWED", result.ReasonCode);
        }

        [Fact]
        public void SqlOutsideApprovedPackage_IsRejected()
        {
            SchemaMigrationPackage package = new(new SchemaReleaseManifest(DatabaseScopes.ProductosServicios, "PS-B20260909", 1, 2, HashContract(2), new[] { "MISSING" }), new[] { M(1, 2) });

            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 2, package, Array.Empty<SchemaControlHistory>());

            Assert.Equal("APPROVED_MIGRATION_MISSING", result.ReasonCode);
        }

        [Fact]
        public void TargetAboveLatest_Blocks()
        {
            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 3, Package(M(1, 2)), Array.Empty<SchemaControlHistory>());

            Assert.Equal(SchemaMigrationResolutionStatus.TargetVersionInvalid, result.Status);
        }

        [Fact]
        public async Task CurrentVersionAboveLatest_RequiresReviewAndZeroDdl()
        {
            Harness harness = new(Package(M(1, 2))) { InitialVersion = 5 };

            SchemaMigrationExecutionResult result = await harness.Runner.MigrateToLatestAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal(SchemaMigrationExecutionStatus.RequiresReview, result.Status);
            Assert.Equal("VERSION_FUTURA_REQUIERE_REVISION", result.ReasonCode);
            Assert.Equal(0, harness.ExecutorExecutionCount);
        }

        [Fact]
        public void ManifestAltered_Blocks()
        {
            SchemaMigrationPackage package = Package(M(1, 2));
            package = new SchemaMigrationPackage(package.Release with { Scope = "Other" }, package.Migrations);

            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 2, package, Array.Empty<SchemaControlHistory>());

            Assert.Equal("PACKAGE_SCOPE_MISMATCH", result.ReasonCode);
        }

        [Fact]
        public void ContractHashAltered_Blocks()
        {
            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 2, Package(M(1, 2) with { TargetManifestHash = "bad" }), Array.Empty<SchemaControlHistory>());

            Assert.Equal("TARGET_MANIFEST_HASH_MISMATCH", result.ReasonCode);
        }

        [Fact]
        public void HistoryAltered_Blocks()
        {
            SchemaMigrationDefinition migration = M(1, 2);
            SchemaControlHistory altered = Applied(migration);
            altered = new SchemaControlHistory
            {
                DatabaseIdentityKey = altered.DatabaseIdentityKey,
                Scope = altered.Scope,
                EventType = altered.EventType,
                FromVersion = altered.FromVersion,
                ToVersion = altered.ToVersion,
                OperationId = altered.OperationId,
                StartedAtUtc = altered.StartedAtUtc,
                CompletedAtUtc = altered.CompletedAtUtc,
                Result = altered.Result,
                Details = "MigrationId=PS-M1-2; SqlHash=altered; TargetManifestHash=" + migration.TargetManifestHash
            };

            SchemaMigrationResolution result = Resolver().GetPendingMigrations(Identity, DatabaseScopes.ProductosServicios, 1, 2, Package(migration), new[] { altered });

            Assert.Equal(SchemaMigrationResolutionStatus.HistoryInconsistent, result.Status);
        }

        [Fact]
        public async Task AppliedTestMigration_WritesHistoryStateAndHash()
        {
            SchemaMigrationDefinition migration = M(1, 2);
            Harness harness = new(Package(migration));

            SchemaMigrationExecutionResult result = await harness.Runner.MigrateToLatestAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal(SchemaMigrationExecutionStatus.Migrated, result.Status);
            Assert.Equal(2, harness.Repository.State!.CurrentVersion);
            Assert.Equal(migration.TargetManifestHash, harness.Repository.State.ManifestHash);
            Assert.Contains(harness.Repository.History, item => item.EventType == "MIGRATED" && item.Result == "PASS" && item.OperationId == migration.MigrationId);
        }

        [Fact]
        public async Task ReexecutionTenTimes_AppliesOnceWithoutDuplicates()
        {
            Harness harness = new(Package(M(1, 2)));

            for (int i = 0; i < 10; i++)
            {
                await harness.Runner.MigrateToLatestAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);
            }

            Assert.Equal(1, harness.ExecutorExecutionCount);
            Assert.Single(harness.Repository.History.Where(item => item.EventType == "MIGRATED" && item.Result == "PASS"));
        }

        [Fact]
        public void RealProductosServiciosPackage_DeclaresApprovedV2Migration()
        {
            var provider = new ProductosServiciosMigrationPackageProvider(new ProductosServiciosSchemaContractProvider(), new SchemaManifestProvider());

            SchemaMigrationPackage package = provider.GetPackage(DatabaseScopes.ProductosServicios);

            Assert.Equal(2, package.Release.LatestSchemaVersion);
            SchemaMigrationDefinition migration = Assert.Single(package.Migrations);
            Assert.Equal(1, migration.FromVersion);
            Assert.Equal(2, migration.ToVersion);
            Assert.Equal("PS-M20260916-V1-V2-DESCRIPCIONES-NVARCHAR-MAX", migration.MigrationId);
            Assert.Equal(new[]
            {
                "dbo.ProductosServiciosCategorias.Descripcion",
                "dbo.ProductosServiciosMarcas.Descripcion",
                "dbo.ProductosServiciosColecciones.Descripcion"
            }, migration.ObjectsAffected);
            Assert.Equal(3, CountOccurrences(migration.UpSql, "ALTER COLUMN [Descripcion] NVARCHAR(MAX) NULL"));
            Assert.DoesNotContain("ProductosServiciosUnidadesMedida", migration.UpSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ProductosServiciosContract_V1RemainsImmutableAndV2OnlyWidensThreeDescriptions()
        {
            var provider = new ProductosServiciosSchemaContractProvider();

            SchemaContract v1 = provider.GetContract(DatabaseScopes.ProductosServicios, 1);
            SchemaContract v2 = provider.GetContract(DatabaseScopes.ProductosServicios, 2);

            Assert.Equal("NVARCHAR(500)", Column(v1, "ProductosServiciosCategorias", "Descripcion").SqlType);
            Assert.Equal(500, Column(v1, "ProductosServiciosCategorias", "Descripcion").MaxLength);
            Assert.Equal("NVARCHAR(500)", Column(v1, "ProductosServiciosMarcas", "Descripcion").SqlType);
            Assert.Equal(500, Column(v1, "ProductosServiciosMarcas", "Descripcion").MaxLength);
            Assert.Equal("NVARCHAR(500)", Column(v1, "ProductosServiciosColecciones", "Descripcion").SqlType);
            Assert.Equal(500, Column(v1, "ProductosServiciosColecciones", "Descripcion").MaxLength);

            Assert.Equal("NVARCHAR(MAX)", Column(v2, "ProductosServiciosCategorias", "Descripcion").SqlType);
            Assert.Equal(-1, Column(v2, "ProductosServiciosCategorias", "Descripcion").MaxLength);
            Assert.Equal("NVARCHAR(MAX)", Column(v2, "ProductosServiciosMarcas", "Descripcion").SqlType);
            Assert.Equal(-1, Column(v2, "ProductosServiciosMarcas", "Descripcion").MaxLength);
            Assert.Equal("NVARCHAR(MAX)", Column(v2, "ProductosServiciosColecciones", "Descripcion").SqlType);
            Assert.Equal(-1, Column(v2, "ProductosServiciosColecciones", "Descripcion").MaxLength);

            var comparableV2 = v2 with
            {
                ContractVersion = v1.ContractVersion,
                Tables = v2.Tables.Select(table => table with
                {
                    Columns = table.Columns.Select(column =>
                        IsMigratedDescription(table.Name, column.Name)
                            ? column with { SqlType = "NVARCHAR(500)", MaxLength = 500 }
                            : column).ToArray()
                }).ToArray()
            };

            Assert.Equal(new SchemaManifestProvider().CreateManifest(v1).ManifestHash, new SchemaManifestProvider().CreateManifest(comparableV2).ManifestHash);
        }

        [Fact]
        public async Task EmptyScope_DoesNotMigrateAndRequiresBootstrap()
        {
            Harness harness = new(Package(M(1, 2))) { ClassificationState = DatabaseStructureState.Empty };

            SchemaMigrationExecutionResult result = await harness.Runner.MigrateToLatestAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal("MIGRATION_NOT_ALLOWED_EMPTY_BOOTSTRAP_REQUIRED", result.ReasonCode);
            Assert.Equal(0, harness.ExecutorExecutionCount);
        }

        private static SchemaMigrationResolver Resolver() => new();

        private static SchemaColumnContract Column(SchemaContract contract, string tableName, string columnName)
        {
            return contract.Tables.Single(table => string.Equals(table.Name, tableName, StringComparison.OrdinalIgnoreCase))
                .Columns.Single(column => string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsMigratedDescription(string tableName, string columnName)
        {
            return string.Equals(columnName, "Descripcion", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(tableName, "ProductosServiciosCategorias", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(tableName, "ProductosServiciosMarcas", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(tableName, "ProductosServiciosColecciones", StringComparison.OrdinalIgnoreCase));
        }

        private static int CountOccurrences(string value, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = value.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                index += pattern.Length;
            }

            return count;
        }

        private static SchemaMigrationPackage Package(params SchemaMigrationDefinition[] migrations)
        {
            int latest = migrations.Length == 0 ? 1 : migrations.Max(item => item.ToVersion);
            return new SchemaMigrationPackage(
                new SchemaReleaseManifest(DatabaseScopes.ProductosServicios, "PS-B20260909", 1, latest, HashContract(latest), migrations.Select(item => item.MigrationId).ToArray()),
                migrations);
        }

        private static SchemaMigrationDefinition M(int from, int to)
        {
            string sql = $"ALTER TABLE dbo.ProductosServicios ADD T{to} int NULL;";
            SchemaContract contract = Contract(to);
            return new SchemaMigrationDefinition(
                $"PS-M{from}-{to}",
                "PS-B20260909",
                DatabaseScopes.ProductosServicios,
                from,
                to,
                to,
                new[] { "dbo.ProductosServicios" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                SchemaMigrationHash.Sha256(sql),
                new SchemaManifestProvider().CreateManifest(contract).ManifestHash,
                "SingleTransaction",
                "Low",
                true,
                TimeSpan.FromSeconds(30),
                Array.Empty<string>(),
                "ReconcileAfterUncertainCommit",
                sql,
                contract);
        }

        private static SchemaContract Contract(int version)
        {
            return new SchemaContract(
                DatabaseScopes.ProductosServicios,
                version,
                "ProductosServicios",
                new[]
                {
                    new SchemaTableContract(
                        "dbo",
                        "ProductosServicios",
                        new[] { new SchemaColumnContract("id", "UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null) },
                        new SchemaPrimaryKeyContract("PK_ProductosServicios", new[] { "id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        Array.Empty<SchemaUniqueContract>(),
                        Array.Empty<SchemaCheckContract>(),
                        Array.Empty<SchemaIndexContract>())
                },
                new[] { "fixture" },
                new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));
        }

        private static string HashContract(int version) => new SchemaManifestProvider().CreateManifest(Contract(version)).ManifestHash;

        private static SchemaControlHistory Applied(SchemaMigrationDefinition migration)
        {
            return new SchemaControlHistory
            {
                DatabaseIdentityKey = Identity.Fingerprint,
                Scope = DatabaseScopes.ProductosServicios,
                EventType = "MIGRATED",
                FromVersion = migration.FromVersion,
                ToVersion = migration.ToVersion,
                OperationId = migration.MigrationId,
                StartedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow,
                Result = "PASS",
                Details = SchemaMigrationHistoryDetails.Build(migration)
            };
        }

        private static SchemaControlState State(int version, string hash)
        {
            return new SchemaControlState
            {
                DatabaseIdentityKey = Identity.Fingerprint,
                Scope = DatabaseScopes.ProductosServicios,
                CurrentVersion = version,
                ManifestHash = hash,
                LastResult = "CURRENT",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        private sealed class Harness
        {
            private readonly FakePackageProvider _packageProvider;
            private readonly FakeClassifier _classifier;
            private readonly FakeValidator _validator = new();
            private readonly FakeLock _lock;
            private readonly FakeExecutor _executor;

            public Harness(SchemaMigrationPackage package)
            {
                _packageProvider = new FakePackageProvider(package);
                Repository = new FakeRepository { State = State(1, package.Release.LatestManifestHash) };
                _classifier = new FakeClassifier(this);
                _lock = new FakeLock(this);
                _executor = new FakeExecutor(this);
                Runner = new SchemaMigrationRunner(_classifier, Repository, _packageProvider, new SchemaMigrationResolver(), _executor, _lock, _validator);
            }

            public FakeRepository Repository { get; }
            public SchemaMigrationRunner Runner { get; }
            public ExecutorMode ExecutorMode { get; init; }
            public DatabaseStructureState ClassificationState { get; init; } = DatabaseStructureState.Current;
            public bool LockThrows { get; init; }
            public int InitialVersion { get => Repository.State?.CurrentVersion ?? 1; init => Repository.State = State(value, _packageProvider.Package.Release.LatestManifestHash); }
            public int AppliedDdlCount { get; set; }
            public int ExecutorExecutionCount { get; set; }

            private sealed class FakePackageProvider : ISchemaMigrationPackageProvider
            {
                public FakePackageProvider(SchemaMigrationPackage package) => Package = package;
                public SchemaMigrationPackage Package { get; }
                public SchemaMigrationPackage GetPackage(string scope) => Package;
            }

            private sealed class FakeClassifier : IDatabaseStateClassifier
            {
                private readonly Harness _harness;
                public FakeClassifier(Harness harness) => _harness = harness;
                public Task<DatabaseClassificationResult> ClassifyAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
                {
                    DatabaseStructureState state = _harness.Repository.State?.CurrentVersion > _harness._packageProvider.Package.Release.LatestSchemaVersion
                        ? DatabaseStructureState.Future
                        : _harness.ClassificationState;
                    return Task.FromResult(new DatabaseClassificationResult { Identity = identity, SanitizedIdentity = identity.ToSanitizedString(), Scope = scope, State = state, IsAvailable = true });
                }
            }

            private sealed class FakeLock : ISchemaProvisionLock
            {
                private readonly Harness _harness;
                private readonly SemaphoreSlim _semaphore = new(1, 1);
                public FakeLock(Harness harness) => _harness = harness;
                public async Task<T> ExecuteAsync<T>(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
                {
                    if (_harness.LockThrows) throw new TimeoutException("LOCK_TIMEOUT");
                    await _semaphore.WaitAsync(cancellationToken);
                    try { return await action(cancellationToken); }
                    finally { _semaphore.Release(); }
                }
            }

            private sealed class FakeExecutor : ISchemaMigrationSqlExecutor
            {
                private readonly Harness _harness;
                public FakeExecutor(Harness harness) => _harness = harness;
                public Task ExecuteAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, SchemaMigrationDefinition migration, Func<SchemaMigrationDefinition, CancellationToken, Task<SchemaContractValidationResult>> validateTargetAsync, CancellationToken cancellationToken = default)
                {
                    _harness.ExecutorExecutionCount++;
                    if (_harness.ExecutorMode == ExecutorMode.FailBeforeDdl) throw new InvalidOperationException("PRECONDITION_FAILED");
                    if (_harness.ExecutorMode == ExecutorMode.FailDuringDdl) throw new InvalidOperationException("DDL_FAILED");
                    if (_harness.ExecutorMode == ExecutorMode.FailValidation) throw new SchemaMigrationValidationException(new[] { "TABLE_MISSING dbo.ProductosServicios" });
                    _harness.AppliedDdlCount++;
                    return Task.CompletedTask;
                }
            }

            private sealed class FakeValidator : ISchemaContractPhysicalValidator
            {
                public Task<SchemaContractValidationResult> ValidateAsync(TenantDatabaseDescriptor descriptor, SchemaContract contract, CancellationToken cancellationToken = default)
                {
                    return Task.FromResult(new SchemaContractValidationResult { DetectedTables = contract.Tables.Count });
                }
            }
        }

        private enum ExecutorMode
        {
            Success,
            FailBeforeDdl,
            FailDuringDdl,
            FailValidation
        }

        private sealed class FakeRepository : ISchemaVersionRepository
        {
            public SchemaControlState? State { get; set; }
            public List<SchemaControlHistory> History { get; } = new();
            public List<SchemaControlAttempt> Attempts { get; } = new();

            public Task<SchemaControlInfrastructureResult> EnsureSchemaControlInfrastructureAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default) =>
                Task.FromResult(new SchemaControlInfrastructureResult { Status = SchemaControlInfrastructureStatus.Ready });

            public Task<SchemaControlState?> GetStateAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default) => Task.FromResult(State);

            public Task<IReadOnlyCollection<SchemaControlHistory>> GetHistoryAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<SchemaControlHistory>>(History.ToArray());

            public Task<IReadOnlyCollection<SchemaControlAttempt>> GetAttemptsAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<SchemaControlAttempt>>(Attempts.ToArray());

            public Task<Guid> BeginAttemptAsync(TenantDatabaseDescriptor descriptor, SchemaControlAttempt attempt, CancellationToken cancellationToken = default)
            {
                Guid id = attempt.AttemptId == Guid.Empty ? Guid.NewGuid() : attempt.AttemptId;
                Attempts.Add(new SchemaControlAttempt { AttemptId = id, DatabaseIdentityKey = attempt.DatabaseIdentityKey, Scope = attempt.Scope, OperationType = attempt.OperationType, FromVersion = attempt.FromVersion, ToVersion = attempt.ToVersion, StartedAtUtc = attempt.StartedAtUtc, Result = attempt.Result, ReasonCode = attempt.ReasonCode });
                return Task.FromResult(id);
            }

            public Task CompleteAttemptAsync(TenantDatabaseDescriptor descriptor, Guid attemptId, string result, string reasonCode, string? error, CancellationToken cancellationToken = default)
            {
                int index = Attempts.FindIndex(item => item.AttemptId == attemptId);
                if (index >= 0)
                {
                    SchemaControlAttempt old = Attempts[index];
                    Attempts[index] = new SchemaControlAttempt { AttemptId = old.AttemptId, DatabaseIdentityKey = old.DatabaseIdentityKey, Scope = old.Scope, OperationType = old.OperationType, FromVersion = old.FromVersion, ToVersion = old.ToVersion, StartedAtUtc = old.StartedAtUtc, CompletedAtUtc = DateTime.UtcNow, Result = result, ReasonCode = reasonCode, Error = error };
                }
                else
                {
                    Attempts.Add(new SchemaControlAttempt { AttemptId = attemptId, Result = result, ReasonCode = reasonCode, Error = error });
                }
                return Task.CompletedTask;
            }

            public Task RecordHistoryAsync(TenantDatabaseDescriptor descriptor, SchemaControlHistory history, CancellationToken cancellationToken = default)
            {
                History.Add(history);
                return Task.CompletedTask;
            }

            public Task SetConfirmedStateAsync(TenantDatabaseDescriptor descriptor, SchemaControlState state, CancellationToken cancellationToken = default)
            {
                State = state;
                return Task.CompletedTask;
            }
        }
    }
}

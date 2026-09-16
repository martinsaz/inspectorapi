using checklistWs.Services.Tenant;
using checklistWs.Controllers.ProductosServicios;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class ProductosServiciosCompanyBootstrapperTests
{
    private static TenantDatabaseDescriptor Descriptor(Guid? id = null) => new()
    { EmpresaKey = "QA", IdEmpresa = id ?? Guid.NewGuid(), ConnectionString = "TEST_ONLY_NOT_A_CONNECTION" };

    private static CompatibilityDecision Allowed() => new()
    { IsAllowed = true, ReasonCode = "COMPATIBLE", SanitizedIdentity = "QA/DB/IDENTITY", SchemaResult = SchemaValidationGlobalResult.SchemaOk };

    private sealed class Gate : IProductosServiciosCompatibilityGate
    {
        public Func<TenantDatabaseDescriptor, CancellationToken, Task<CompatibilityDecision>> Run = (_, _) => Task.FromResult(Allowed());
        public int Calls;
        public Task<CompatibilityDecision> EvaluateAsync(TenantDatabaseDescriptor d, string scope = DatabaseScopes.ProductosServicios, CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref Calls); return Run(d, cancellationToken); }
    }
    private static ProductosServiciosCompanyBootstrapper Service(Gate gate) => new(gate, NullLogger<ProductosServiciosCompanyBootstrapper>.Instance);

    [Fact] public async Task CompatibleCompanyRequiresNoSeedsEvenOnFirstInvocation()
    {
        var gate = new Gate(); var r = await Service(gate).BootstrapAsync(Descriptor());
        Assert.Equal(1, gate.Calls); Assert.Equal("NO_CHANGES", r.Status);
        Assert.Equal("NO_REQUIRED_COMPANY_SEEDS", r.ReasonCode);
        Assert.Empty(r.CreatedItems); Assert.Empty(r.ExistingItems); Assert.Empty(r.SkippedItems); Assert.False(r.ExecutedDdl);
        Assert.True(r.CompletedAtUtc >= r.StartedAtUtc); Assert.True(Guid.TryParse(r.ReferenceId, out _));
    }

    [Theory]
    [InlineData("EMPTY")][InlineData("PARTIAL")][InlineData("OUTDATED")][InlineData("FUTURE")]
    [InlineData("UNKNOWN")][InlineData("UNAVAILABLE")][InlineData("DRIFT")][InlineData("PREPARING")]
    [InlineData("MIGRATING")][InlineData("INCONCLUSIVE")][InlineData("HASH_MISMATCH")]
    [InlineData("REQUIRES_REVIEW")][InlineData("SCHEMA_DRIFT_CRITICO")]
    public async Task EveryT20BlockStopsCompanyPreparation(string code)
    {
        var gate = new Gate { Run = (_, _) => Task.FromResult(new CompatibilityDecision { ReasonCode = code }) };
        var r = await Service(gate).BootstrapAsync(Descriptor());
        Assert.Equal("BLOCKED", r.Status); Assert.Equal("COMPATIBILITY_BLOCKED", r.ReasonCode);
        Assert.Empty(r.CreatedItems); Assert.False(r.ExecutedDdl);
    }

    [Fact] public async Task TenExecutionsAreStatelessAndIdempotent()
    {
        var g = new Gate(); var service = Service(g); var d = Descriptor(); var ids = new HashSet<string>();
        for (int i = 0; i < 10; i++) { var r = await service.BootstrapAsync(d); Assert.Equal("NO_CHANGES", r.Status); Assert.Empty(r.CreatedItems); ids.Add(r.ReferenceId); }
        Assert.Equal(10, g.Calls); Assert.Equal(10, ids.Count);
    }
    [Fact] public async Task ConcurrentSameCompanyCannotDuplicateAnything()
    {
        var g = new Gate { Run = async (_, _) => { await Task.Yield(); return Allowed(); } };
        var service = Service(g); var d = Descriptor();
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => service.BootstrapAsync(d)));
        Assert.All(results, r => { Assert.Equal("NO_CHANGES", r.Status); Assert.Empty(r.CreatedItems); });
        Assert.Equal(10, g.Calls);
    }
    [Fact] public async Task TwoCompaniesUseTheirOwnServerSideDescriptor()
    {
        var seen = new System.Collections.Concurrent.ConcurrentBag<Guid>();
        var g = new Gate { Run = (d, _) => { seen.Add(d.IdEmpresa); return Task.FromResult(Allowed()); } };
        var c = Descriptor(); var d = Descriptor();
        await Task.WhenAll(Service(g).BootstrapAsync(c), Service(g).BootstrapAsync(d));
        Assert.Contains(c.IdEmpresa, seen); Assert.Contains(d.IdEmpresa, seen); Assert.Equal(2, seen.Count);
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)]
    public async Task InvalidServerContextFailsBeforeGate(int invalid)
    {
        var d = invalid switch { 0 => null!, 1 => Descriptor(Guid.Empty), 2 => new TenantDatabaseDescriptor { IdEmpresa = Guid.NewGuid(), ConnectionString = "x" }, _ => new TenantDatabaseDescriptor { IdEmpresa = Guid.NewGuid(), EmpresaKey = "QA" } };
        var g = new Gate(); Assert.Equal("BLOCKED", (await Service(g).BootstrapAsync(d)).Status); Assert.Equal(0, g.Calls);
    }
    [Fact] public async Task OtherScopeCannotInitializeCompany()
    { var g = new Gate(); Assert.Equal("BLOCKED", (await Service(g).BootstrapAsync(Descriptor(), "Other")).Status); Assert.Equal(0, g.Calls); }
    [Fact] public async Task GateFailureCannotLeakSecretsOrLeavePartialInitialization()
    {
        var g = new Gate { Run = (_, _) => throw new Exception("Password=CANARY;Token=CANARY") };
        var r = await Service(g).BootstrapAsync(Descriptor());
        Assert.Equal("REQUIRES_REVIEW", r.ReasonCode); Assert.Empty(r.CreatedItems); Assert.DoesNotContain("CANARY", JsonSerializer.Serialize(r));
    }
    [Fact] public async Task CancellationFailsClosedWithoutChanges()
    {
        var g = new Gate(); using var cts = new CancellationTokenSource(); cts.Cancel();
        var r = await Service(g).BootstrapAsync(Descriptor(), cancellationToken: cts.Token);
        Assert.Equal("CANCELLED", r.ReasonCode); Assert.Empty(r.CreatedItems); Assert.Equal(0, g.Calls);
    }
    [Fact] public async Task PreviouslyCompatibleDoesNotBypassNewBlock()
    {
        var g = new Gate(); var svc = Service(g); var d = Descriptor();
        Assert.Equal("NO_CHANGES", (await svc.BootstrapAsync(d)).Status);
        g.Run = (_, _) => Task.FromResult(new CompatibilityDecision { ReasonCode = "DRIFT" });
        Assert.Equal("BLOCKED", (await svc.BootstrapAsync(d)).Status);
    }
    [Fact] public async Task InconsistentAllowCannotBeUsed()
    {
        var g = new Gate { Run = (_, _) => Task.FromResult(new CompatibilityDecision { IsAllowed = true }) };
        Assert.Equal("BLOCKED", (await Service(g).BootstrapAsync(Descriptor())).Status);
    }
    private sealed class Resolver : ITenantDatabaseResolver
    {
        public int Calls;
        public Task<TenantDatabaseDescriptor> ResolveAsync(TenantDatabaseContext c, CancellationToken cancellationToken = default)
        { Calls++; return Task.FromResult(new TenantDatabaseDescriptor { EmpresaKey = c.EmpresaKey, IdEmpresa = c.IdEmpresa, ConnectionString = "QA" }); }
    }
    private sealed class NoSql : ITenantSqlConnectionFactory
    {
        public System.Data.SqlClient.SqlConnection CreateConnection(TenantDatabaseDescriptor d) => throw new InvalidOperationException("Unexpected business SQL");
    }
    private sealed class Company : IProductosServiciosCompanyBootstrapper
    {
        public int Calls;
        public Guid Seen;
        public Task<CompanyBootstrapResult> BootstrapAsync(TenantDatabaseDescriptor d, string scope = DatabaseScopes.ProductosServicios, CancellationToken cancellationToken = default)
        { Calls++; Seen = d.IdEmpresa; return Task.FromResult(new CompanyBootstrapResult { ReasonCode = "QA_STOP" }); }
    }
    private sealed class Authz : IProductosServiciosAuthorizationService
    {
        public Task<ProductosServiciosAuthorizationDecision> AuthorizeAsync(ProductosServiciosAuthorizationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ProductosServiciosAuthorizationDecision { HasAccess = true, CanWrite = true, PermissionCode = request.PermissionCode, ReasonCode = "ALLOW_WRITE" });
    }
    private static ProductosServiciosController Controller(Guid id, Resolver resolver, Gate gate, Company company)
    {
        var controller = new ProductosServiciosController(new ConfigurationBuilder().Build(), NullLogger<ProductosServiciosController>.Instance, resolver, new NoSql(), gate, new Authz(), company);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("idEmpresa", id.ToString()), new Claim("empresa", "QA"), new Claim(ClaimTypes.NameIdentifier, "QA-SUBJECT") }, "QA"));
        return controller;
    }
    [Fact] public async Task ClientCannotImposeAnotherCompanyBeforeT22()
    {
        var resolver = new Resolver(); var gate = new Gate(); var company = new Company();
        var r = await Controller(Guid.NewGuid(), resolver, gate, company).ObtenerProductosServicios(Guid.NewGuid());
        Assert.Equal(403, Assert.IsType<ObjectResult>(r).StatusCode); Assert.Equal(0, resolver.Calls); Assert.Equal(0, company.Calls);
    }
    [Fact] public async Task ControllerRunsCompanyPreparationOnlyAfterT20AndUsesAuthorizedCompany()
    {
        var resolver = new Resolver(); var gate = new Gate(); var company = new Company(); var id = Guid.NewGuid();
        var r = await Controller(id, resolver, gate, company).ObtenerProductosServicios(id);
        Assert.Equal(503, Assert.IsType<ObjectResult>(r).StatusCode); Assert.Equal(1, gate.Calls); Assert.Equal(id, company.Seen);
    }
    [Fact] public async Task ControllerNeverCallsCompanyServiceWhenT20Blocks()
    {
        var resolver = new Resolver(); var gate = new Gate { Run = (_, _) => Task.FromResult(new CompatibilityDecision { ReasonCode = "SCHEMA_EMPTY" }) }; var company = new Company(); var id = Guid.NewGuid();
        var r = await Controller(id, resolver, gate, company).ObtenerProductosServicios(id);
        Assert.Equal(503, Assert.IsType<ObjectResult>(r).StatusCode); Assert.Equal(0, company.Calls);
    }
    [Fact] public void NoOpHasNoSqlFirebaseOrStructuralWriterDependency()
    {
        var dependencies = typeof(ProductosServiciosCompanyBootstrapper).GetConstructors().Single().GetParameters().Select(p => p.ParameterType).ToArray();
        Assert.Equal(new[] { typeof(IProductosServiciosCompatibilityGate), typeof(ILogger<ProductosServiciosCompanyBootstrapper>) }, dependencies);
        Assert.DoesNotContain(typeof(ISchemaVersionRepository), dependencies);
        Assert.DoesNotContain(typeof(ITenantSqlConnectionFactory), dependencies);
        Assert.DoesNotContain(typeof(ITenantConnectionReader), dependencies);
    }
    private sealed class CaptureLog : ILogger<ProductosServiciosCompanyBootstrapper>
    {
        public string Text = string.Empty;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Text += formatter(state, exception);
    }
    [Fact] public async Task LogsContainAuditIdentityAndCountsWithoutConnectionOrSecret()
    {
        var log = new CaptureLog(); var id = Guid.NewGuid();
        var result = await new ProductosServiciosCompanyBootstrapper(new Gate(), log).BootstrapAsync(Descriptor(id));
        Assert.Contains(id.ToString(), log.Text); Assert.Contains(result.ReferenceId, log.Text);
        Assert.Contains("CreatedCount=0", log.Text); Assert.DoesNotContain("QA/DB/IDENTITY", log.Text);
        Assert.Contains(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("QA/DB/IDENTITY"))), log.Text);
        Assert.DoesNotContain("TEST_ONLY_NOT_A_CONNECTION", log.Text);
        Assert.DoesNotContain("ConnectionString", JsonSerializer.Serialize(result));
    }
    [Fact] public async Task CancellationDuringGateDoesNotReturnSuccessfulPreparation()
    {
        using var cts = new CancellationTokenSource();
        var g = new Gate { Run = (_, _) => { cts.Cancel(); return Task.FromResult(Allowed()); } };
        var result = await Service(g).BootstrapAsync(Descriptor(), cancellationToken: cts.Token);
        Assert.Equal("BLOCKED", result.Status); Assert.Equal("CANCELLED", result.ReasonCode); Assert.Empty(result.CreatedItems);
    }

}

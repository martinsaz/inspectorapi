using checklistWs.Services.Tenant;
using checklistWs.Controllers.ProductosServicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class ProductosServiciosSecurityContextTests
{
    [Fact]
    public void ProductosServiciosPermissionCodeUsesApprovedPoCode()
    {
        Assert.Equal("05001000", ProductosServiciosAuthorizationDefaults.PermissionCode);
        Assert.NotEqual("02000000", ProductosServiciosAuthorizationDefaults.PermissionCode);
    }

    [Fact]
    public void AuthorizationSourcePrefersConfiguredLegacyConnectionOverTenantDatabase()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CadenaConexionSQLServer"] = "Server=legacy;Database=db_a883c3_checklist;User Id=qa;",
                ["ProductosServicios:AuthorizationConnectionString"] = "Server=auth;Database=db_a883c3_checklist;User Id=qa;"
            })
            .Build();

        Assert.Equal(
            "Server=auth;Database=db_a883c3_checklist;User Id=qa;",
            ProductosServiciosAuthorizationService.ResolveAuthorizationConnectionString(configuration));
    }

    [Fact]
    public void AuthorizationSourceFallsBackToLegacyConnectionStringWhenDedicatedKeyIsMissing()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CadenaConexionSQLServer"] = "Server=legacy;Database=db_a883c3_checklist;User Id=qa;"
            })
            .Build();

        Assert.Equal(
            "Server=legacy;Database=db_a883c3_checklist;User Id=qa;",
            ProductosServiciosAuthorizationService.ResolveAuthorizationConnectionString(configuration));
    }

    private const string Secret = "SYNTHETIC_TEST_KEY_NOT_A_CREDENTIAL";
    private static readonly Guid A = Guid.NewGuid(), B = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static ClaimsPrincipal Principal(params Claim[] extra) => new(new ClaimsIdentity(new[]
    { new Claim("idEmpresa", A.ToString()), new Claim("empresa", "QA-A"), new Claim(ClaimTypes.NameIdentifier,"firebase-uid-non-guid") }.Concat(extra), "ControlledQa"));
    private static HeaderDictionary Signed(DateTimeOffset? timestamp = null, Guid? company = null, string user = "firebase-uid-non-guid")
    {
        var id=(company??A).ToString();var time=(timestamp??Now).ToString("O");
        using var hmac=new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        return new HeaderDictionary
        {
            ["X-ProductosServicios-Proxy-EmpresaId"]=id,
            ["X-ProductosServicios-Proxy-Empresa"]="QA-A",
            ["X-ProductosServicios-Proxy-UsuarioId"]=user,
            ["X-ProductosServicios-Proxy-Timestamp"]=time,
            ["X-ProductosServicios-Proxy-Signature"]=Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(string.Join('\n',id,"QA-A",user,time))))
        };
    }
    private static bool Valid(ClaimsPrincipal p, IHeaderDictionary h, string secret = Secret) => ProductosServiciosIdentityValidation.TryValidate(p,h,secret,Now,out _,out _);
    [Fact] public void AuthenticatedPrincipalRequiresAllFields() => Assert.True(Valid(Principal(),new HeaderDictionary()));
    [Fact] public void SignedProxyAcceptsFirebaseUidWithoutPretendingItIsSqlGuid()
    { Assert.True(ProductosServiciosIdentityValidation.TryValidate(new ClaimsPrincipal(),Signed(),Secret,Now,out var c,out var u)); Assert.Equal(A,c.IdEmpresa);Assert.Equal("firebase-uid-non-guid",u); }
    [Theory][InlineData("idEmpresa")][InlineData("empresa")][InlineData(ClaimTypes.NameIdentifier)]
    public void MissingClaimFailsClosed(string type) => Assert.False(Valid(new ClaimsPrincipal(new ClaimsIdentity(Principal().Claims.Where(c=>c.Type!=type),"QA")),new HeaderDictionary()));
    [Theory][InlineData("tenantId")][InlineData("companyId")][InlineData("idempresa")]
    public void ConflictingCompanyAliasFailsClosed(string type) => Assert.False(Valid(Principal(new Claim(type,B.ToString())),new HeaderDictionary()));
    [Theory][InlineData("empresaNombre")][InlineData("tenantName")]
    public void ConflictingEmpresaAliasFailsClosed(string type) => Assert.False(Valid(Principal(new Claim(type,"QA-B")),new HeaderDictionary()));
    [Fact] public void ConflictingUserFailsClosed() => Assert.False(Valid(Principal(new Claim("sub","another-subject")),new HeaderDictionary()));
    [Fact] public void UnauthenticatedClaimsCannotAuthorize() => Assert.False(Valid(new ClaimsPrincipal(new ClaimsIdentity(Principal().Claims)),new HeaderDictionary()));
    [Fact] public void NoClaimsNoProxyCannotAuthorize() => Assert.False(Valid(new ClaimsPrincipal(),new HeaderDictionary()));
    [Fact] public void ExpiredAuthenticatedClaimCannotAuthorize() => Assert.False(Valid(Principal(new Claim("exp",Now.AddMinutes(-1).ToUnixTimeSeconds().ToString())),new HeaderDictionary()));
    [Fact] public void InvalidExpirationFailsClosed() => Assert.False(Valid(Principal(new Claim("exp","invalid")),new HeaderDictionary()));
    [Theory][InlineData(-6)][InlineData(6)] public void ProxyOutsideToleranceFailsClosed(int minutes) => Assert.False(Valid(new ClaimsPrincipal(),Signed(Now.AddMinutes(minutes))));
    [Fact] public void ExistingReplayWindowIsPreservedWithoutInventedNonce()
    { var h=Signed(Now.AddMinutes(-1));Assert.True(Valid(new ClaimsPrincipal(),h));Assert.True(Valid(new ClaimsPrincipal(),h)); }
    [Theory][InlineData("EmpresaId")][InlineData("Empresa")][InlineData("UsuarioId")][InlineData("Timestamp")][InlineData("Signature")]
    public void EverySignedFieldIsRequired(string field)
    { var h=Signed();h.Remove("X-ProductosServicios-Proxy-"+field);Assert.False(Valid(new ClaimsPrincipal(),h)); }
    [Theory][InlineData("EmpresaId")][InlineData("Empresa")][InlineData("UsuarioId")]
    public void EveryIdentityFieldIsSigned(string field)
    { var h=Signed();h["X-ProductosServicios-Proxy-"+field]=field=="EmpresaId"?B.ToString():"QA-B";Assert.False(Valid(new ClaimsPrincipal(),h)); }
    [Fact] public void DuplicateHeaderIsNotFirstWins()
    { var h=Signed();h["X-ProductosServicios-Proxy-Empresa"]=new StringValues(new[]{"QA-A","QA-B"});Assert.False(Valid(new ClaimsPrincipal(),h)); }
    [Fact] public void InvalidBase64SignatureFailsClosed()
    { var h=Signed();h["X-ProductosServicios-Proxy-Signature"]="***";Assert.False(Valid(new ClaimsPrincipal(),h)); }
    [Fact] public void EmptyKeyCannotSignProxy() => Assert.False(Valid(new ClaimsPrincipal(),Signed(),""));
    [Fact] public void ContradictoryAuthenticatedAndSignedContextFailsClosed() => Assert.False(Valid(Principal(),Signed(company:B)));
    [Fact] public void MatchingAuthenticatedAndSignedContextWorks() => Assert.True(Valid(Principal(),Signed()));
    [Fact] public void ArbitraryTenantHeadersNeverSelectContext()
    { var h=new HeaderDictionary{["X-Tenant"]=B.ToString(),["DatabaseIdentity"]="OTHER",["Scope"]="OTHER"};Assert.True(ProductosServiciosIdentityValidation.TryValidate(Principal(),h,Secret,Now,out var c,out _));Assert.Equal(A,c.IdEmpresa); }

    private sealed class Resolver : ITenantDatabaseResolver
    {
        public int Calls; public TenantDatabaseResolutionCode? Error;
        public Task<TenantDatabaseDescriptor> ResolveAsync(TenantDatabaseContext c,CancellationToken cancellationToken=default)
        {Calls++;if(Error.HasValue)throw new TenantDatabaseResolutionException(Error.Value);return Task.FromResult(new TenantDatabaseDescriptor{IdEmpresa=c.IdEmpresa,EmpresaKey=c.EmpresaKey,ConnectionString="CANARY_NOT_SQL"});}
    }
    private sealed class Factory : ITenantSqlConnectionFactory
    {
        public int Calls;
        public System.Data.SqlClient.SqlConnection CreateConnection(TenantDatabaseDescriptor d)
        {Calls++;throw new InvalidOperationException("Password=CANARY;Token=CANARY;SERVER=CANARY");}
    }
    private sealed class Gate : IProductosServiciosCompatibilityGate
    {
        public int Calls; public bool Allow=true;
        public Task<CompatibilityDecision> EvaluateAsync(TenantDatabaseDescriptor d,string scope=DatabaseScopes.ProductosServicios,CancellationToken cancellationToken=default)
        {Calls++;return Task.FromResult(new CompatibilityDecision{IsAllowed=Allow,ReasonCode=Allow?"COMPATIBLE":"SCHEMA_DRIFT",SanitizedIdentity="QA",SchemaResult=SchemaValidationGlobalResult.SchemaOk});}
    }
    private sealed class Authz : IProductosServiciosAuthorizationService
    {
        public int Calls; public bool Access = true; public bool Write = true; public ProductosServiciosPermissionRequirement? Requirement; public string? PermissionCode;
        public Task<ProductosServiciosAuthorizationDecision> AuthorizeAsync(ProductosServiciosAuthorizationRequest request, CancellationToken cancellationToken = default)
        {
            Calls++; Requirement = request.Requirement; PermissionCode = request.PermissionCode;
            return Task.FromResult(new ProductosServiciosAuthorizationDecision
            {
                HasAccess = Access,
                CanWrite = Write,
                PermissionCode = request.PermissionCode,
                ReasonCode = Access ? Write ? "ALLOW_WRITE" : "ALLOW_READ" : "ACCESS_DENIED",
                ReferenceId = "qa-reference"
            });
        }
    }
    private sealed class Harness
    {
        public Resolver Resolver=new();public Factory Factory=new();public Gate Gate=new(); public Authz Authz = new(); public ProductosServiciosController Controller;
        public Harness()
        {
            Controller=new ProductosServiciosController(new ConfigurationBuilder().Build(),NullLogger<ProductosServiciosController>.Instance,Resolver,Factory,Gate,Authz,new ProductosServiciosCompanyBootstrapper(Gate,NullLogger<ProductosServiciosCompanyBootstrapper>.Instance));
            Controller.ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext{User=Principal()}};
        }
    }
    [Theory][InlineData("empresa","QA-B")][InlineData("empresaKey","QA-B")][InlineData("cadena","CANARY")][InlineData("connectionString","CANARY")][InlineData("server","CANARY")]
    public async Task QueryCannotOverrideServerTenantOrConnection(string key,string value)
    {var h=new Harness();h.Controller.Request.QueryString=new QueryString("?"+key+"="+value);var r=await h.Controller.ObtenerProductosServicios(A);Assert.Equal(403,Assert.IsType<ObjectResult>(r).StatusCode);Assert.Equal(0,h.Resolver.Calls);Assert.Equal(0,h.Factory.Calls);}
    [Fact] public async Task QueryIdCannotContradictBoundBodyOrClaims()
    {var h=new Harness();h.Controller.Request.QueryString=new QueryString("?idEmpresa="+B);var r=await h.Controller.ObtenerProductosServicios(A);Assert.Equal(403,Assert.IsType<ObjectResult>(r).StatusCode);Assert.Equal(0,h.Factory.Calls);}
    [Fact] public async Task JsonCompanyCannotOverrideClaims()
    {var h=new Harness();var r=await h.Controller.GuardarProductoServicio(new checklistWs.Models.ProductosServicios.ProductoServicioGuardarRequest { IdEmpresa=B },A);Assert.IsType<BadRequestObjectResult>(r);Assert.Equal(0,h.Factory.Calls);}
    [Fact] public async Task MultipartContradictionStopsBeforeSql()
    {var h=new Harness();h.Controller.Request.ContentType="multipart/form-data; boundary=fixture";h.Controller.Request.Form=new FormCollection(new Dictionary<string,StringValues>{{"idEmpresa",B.ToString()}});var r=await h.Controller.SubirMultimediaTemporal(A,"documento","QA",null);Assert.Equal(403,Assert.IsType<ObjectResult>(r).StatusCode);Assert.Equal(0,h.Factory.Calls);}
    [Theory][InlineData(TenantDatabaseResolutionCode.TenantNotFound)][InlineData(TenantDatabaseResolutionCode.TenantConnectionInactive)][InlineData(TenantDatabaseResolutionCode.TenantIdEmpresaMismatch)][InlineData(TenantDatabaseResolutionCode.TenantConnectionInvalid)]
    public async Task InvalidResolvedTenantNeverReachesBusinessSql(TenantDatabaseResolutionCode code)
    {var h=new Harness();h.Resolver.Error=code;var r=await h.Controller.ObtenerProductosServicios(A);Assert.Equal(403,Assert.IsType<ObjectResult>(r).StatusCode);Assert.Equal(0,h.Gate.Calls);Assert.Equal(0,h.Factory.Calls);}
    [Fact] public async Task ValidContextWithGateBlockIs503Not401()
    {var h=new Harness();h.Gate.Allow=false;var r=await h.Controller.ObtenerProductosServicios(A);Assert.Equal(503,Assert.IsType<ObjectResult>(r).StatusCode);Assert.Equal(0,h.Factory.Calls);}
    [Fact] public async Task MissingAccessStopsBeforeGateOrBusinessSql()
    {var h=new Harness();h.Authz.Access=false;var r=await h.Controller.ObtenerProductosServicios(A);Assert.Equal(403,Assert.IsType<ObjectResult>(r).StatusCode);Assert.Equal(1,h.Authz.Calls);Assert.Equal(0,h.Gate.Calls);Assert.Equal(0,h.Factory.Calls);}
    [Fact] public async Task ReadonlyAllowsReadAndBlocksWriteBeforeGate()
    {var h=new Harness();h.Authz.Write=false;var read=await h.Controller.ObtenerProductosServicios(A);Assert.Equal(500,Assert.IsType<ObjectResult>(read).StatusCode);Assert.Equal(ProductosServiciosPermissionRequirement.Read,h.Authz.Requirement);Assert.Equal("05001001",h.Authz.PermissionCode);h=new Harness();h.Authz.Write=false;var write=await h.Controller.BajaProductoServicio(A,Guid.NewGuid());Assert.Equal(403,Assert.IsType<ObjectResult>(write).StatusCode);Assert.Equal(ProductosServiciosPermissionRequirement.Write,h.Authz.Requirement);Assert.Equal("05001001",h.Authz.PermissionCode);Assert.Equal(0,h.Gate.Calls);Assert.Equal(0,h.Factory.Calls);}
    [Theory]
    [InlineData("ObtenerProductosServicios", "05001001")]
    [InlineData("ObtenerCategoriasProductosServicios", "05001003")]
    [InlineData("ObtenerMarcasProductosServicios", "05001004")]
    [InlineData("ObtenerUnidadesMedidaProductosServicios", "05001005")]
    public async Task ProductosServiciosEndpointsUseSpecificPermissionCode(string actionName, string expectedCode)
    {
        var h=new Harness();
        h.Controller.ControllerContext.ActionDescriptor = new ControllerActionDescriptor { ActionName = actionName };
        var result=actionName switch
        {
            "ObtenerCategoriasProductosServicios" => await h.Controller.ObtenerCategoriasProductosServicios(A),
            "ObtenerMarcasProductosServicios" => await h.Controller.ObtenerMarcasProductosServicios(A),
            "ObtenerUnidadesMedidaProductosServicios" => await h.Controller.ObtenerUnidadesMedidaProductosServicios(A),
            _ => await h.Controller.ObtenerProductosServicios(A)
        };
        Assert.NotNull(result);
        Assert.Equal(expectedCode,h.Authz.PermissionCode);
    }

    [Fact]
    public async Task AbcWriteEndpointUsesAbcPermissionCode()
    {
        var h=new Harness();
        h.Controller.ControllerContext.ActionDescriptor = new ControllerActionDescriptor { ActionName = "BajaProductoServicio" };
        await h.Controller.BajaProductoServicio(A, Guid.NewGuid());
        Assert.Equal(ProductosServiciosPermissionRequirement.Write, h.Authz.Requirement);
        Assert.Equal("05001001", h.Authz.PermissionCode);
    }
    [Fact] public async Task ErrorsAfterVerifiedContextAreSanitized()
    {var h=new Harness();h.Controller.Request.QueryString=new QueryString("?DatabaseIdentity=OTHER&Scope=OTHER");var r=await h.Controller.ObtenerProductosServicios(A);Assert.Equal(500,Assert.IsType<ObjectResult>(r).StatusCode);Assert.Equal(1,h.Factory.Calls);Assert.True(h.Gate.Calls>=1);Assert.DoesNotContain("CANARY",JsonSerializer.Serialize(((ObjectResult)r).Value));}
    public static IEnumerable<object[]> AllEndpoints() => typeof(ProductosServiciosController).GetMethods()
        .Where(m => m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute), true).Length > 0)
        .Select(m => new object[] { m.Name });
    [Theory][MemberData(nameof(AllEndpoints))]
    public async Task EveryEndpointRejectsMissingIdentityBeforeAnyResolverOrSql(string methodName)
    {
        var h = new Harness(); h.Controller.HttpContext.User = new ClaimsPrincipal();
        var method = typeof(ProductosServiciosController).GetMethod(methodName)!;
        var args = method.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue :
            p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) :
            p.ParameterType == typeof(string) ? string.Empty :
            p.ParameterType.GetConstructor(Type.EmptyTypes) != null ? Activator.CreateInstance(p.ParameterType) : null).ToArray();
        var result = await (Task<IActionResult>)method.Invoke(h.Controller, args)!;
        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(0, h.Resolver.Calls); Assert.Equal(0, h.Authz.Calls); Assert.Equal(0, h.Gate.Calls); Assert.Equal(0, h.Factory.Calls);
    }

}

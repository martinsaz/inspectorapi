using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Xunit;
using MvcController = checklist.Controllers.ProductosServicios.ProductosServiciosController;

namespace checklistWs.Tests.Services.Tenant;
public sealed class ProductosServiciosMvcContextTests
{
    private sealed class NeverHttp : IHttpClientFactory { public HttpClient CreateClient(string name)=>throw new InvalidOperationException("Unexpected proxy request"); }
    private sealed class StaticHttp : IHttpClientFactory
    {
        private readonly string content;
        public StaticHttp(string content) => this.content = content;
        public HttpClient CreateClient(string name) => new(new Handler(content));
        private sealed class Handler : HttpMessageHandler
        {
            private readonly string content;
            public Handler(string content) => this.content = content;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(content) });
        }
    }
    private sealed class Session : ISession
    {
        private readonly Dictionary<string,byte[]> values=new();
        public bool IsAvailable=>true; public string Id=>"QA";public IEnumerable<string> Keys=>values.Keys;
        public void Clear()=>values.Clear();public void Remove(string key)=>values.Remove(key);public void Set(string key,byte[] value)=>values[key]=value;
        public bool TryGetValue(string key,out byte[]? value)=>values.TryGetValue(key,out value);
        public Task LoadAsync(CancellationToken cancellationToken=default)=>Task.CompletedTask;public Task CommitAsync(CancellationToken cancellationToken=default)=>Task.CompletedTask;
    }
    [Theory][InlineData("valid",0)][InlineData("missingUser",401)][InlineData("missingCompany",401)][InlineData("missingId",401)][InlineData("query",403)][InlineData("session",403)][InlineData("contradictory",401)][InlineData("emptyKey",401)]
    public async Task ProxyValidatesServerContextBeforeSigning(string scenario,int expected)
    {
        var id=Guid.NewGuid();var claims=new List<Claim>{new(ClaimTypes.SerialNumber,id.ToString()),new(ClaimTypes.Sid,"QA-A"),new(ClaimTypes.NameIdentifier,"firebase-non-guid"),new(ClaimTypes.Role,Guid.NewGuid().ToString()),new(ClaimTypes.Uri,"QA-CADENA")};
        if(scenario=="missingUser")claims.RemoveAll(c=>c.Type==ClaimTypes.NameIdentifier);
        if(scenario=="missingCompany")claims.RemoveAll(c=>c.Type==ClaimTypes.Sid);
        if(scenario=="missingId")claims.RemoveAll(c=>c.Type==ClaimTypes.SerialNumber);
        if(scenario=="contradictory")claims.Add(new Claim(ClaimTypes.Sid,"QA-B"));
        var http=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity(claims,"QA")),Session=new Session()};
        if(scenario=="query")http.Request.QueryString=new QueryString("?empresa=QA-B");
        if(scenario=="session")http.Session.SetString("empresa","QA-B");
        const string roleJson = "[{\"Permisos\":\"[{\\\"Opcion\\\":\\\"05001000\\\",\\\"Permisos\\\":{\\\"Acceso\\\":1,\\\"Escritura\\\":0},\\\"Hijos\\\":[{\\\"Opcion\\\":\\\"05001001\\\",\\\"Permisos\\\":{\\\"Acceso\\\":1,\\\"Escritura\\\":1},\\\"Hijos\\\":[]}]}]\"}]";
        IHttpClientFactory clientFactory = scenario == "valid" ? new StaticHttp(roleJson) : new NeverHttp();
        var controller=new MvcController(clientFactory,new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"fireBdata:fireClave",scenario=="emptyKey"?"":"SYNTHETIC_TEST_KEY"},{"ProductosServicios:PermissionCode","05001000"}}).Build());
        controller.ControllerContext=new ControllerContext{HttpContext=http};
        var action=new ActionContext(http,new RouteData(),new ActionDescriptor());
        var ctx=new ActionExecutingContext(action,new List<IFilterMetadata>(),new Dictionary<string,object?>(),controller);
        bool nextCalled=false;
        await controller.OnActionExecutionAsync(ctx,()=>{nextCalled=true;return Task.FromResult(new ActionExecutedContext(action,new List<IFilterMetadata>(),controller));});
        if(expected==0){Assert.True(nextCalled);Assert.Null(ctx.Result);}else{Assert.False(nextCalled);Assert.Equal(expected,Assert.IsAssignableFrom<ObjectResult>(ctx.Result).StatusCode);}
    }

    [Fact]
    public async Task ProxyFailsClosedWhenPermissionIsOnlyInspeccionesCode()
    {
        var id=Guid.NewGuid();
        var claims=new List<Claim>{new(ClaimTypes.SerialNumber,id.ToString()),new(ClaimTypes.Sid,"QA-A"),new(ClaimTypes.NameIdentifier,"firebase-non-guid"),new(ClaimTypes.Role,Guid.NewGuid().ToString()),new(ClaimTypes.Uri,"QA-CADENA")};
        var http=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity(claims,"QA")),Session=new Session()};
        const string roleJson = "[{\"Permisos\":\"[{\\\"Opcion\\\":\\\"02000000\\\",\\\"Permisos\\\":{\\\"Acceso\\\":1,\\\"Escritura\\\":1},\\\"Hijos\\\":[]}]\"}]";
        var controller=new MvcController(new StaticHttp(roleJson),new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"fireBdata:fireClave","SYNTHETIC_TEST_KEY"},{"ProductosServicios:PermissionCode","05001000"}}).Build());
        controller.ControllerContext=new ControllerContext{HttpContext=http};
        var action=new ActionContext(http,new RouteData(),new ActionDescriptor());
        var ctx=new ActionExecutingContext(action,new List<IFilterMetadata>(),new Dictionary<string,object?>(),controller);
        bool nextCalled=false;
        await controller.OnActionExecutionAsync(ctx,()=>{nextCalled=true;return Task.FromResult(new ActionExecutedContext(action,new List<IFilterMetadata>(),controller));});
        Assert.False(nextCalled);
        Assert.Equal(403,Assert.IsAssignableFrom<ObjectResult>(ctx.Result).StatusCode);
    }

    [Fact]
    public async Task ProxyFailsClosedWhenOnlyProductosServiciosParentExists()
    {
        var id=Guid.NewGuid();
        var claims=new List<Claim>{new(ClaimTypes.SerialNumber,id.ToString()),new(ClaimTypes.Sid,"QA-A"),new(ClaimTypes.NameIdentifier,"firebase-non-guid"),new(ClaimTypes.Role,Guid.NewGuid().ToString()),new(ClaimTypes.Uri,"QA-CADENA")};
        var http=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity(claims,"QA")),Session=new Session()};
        const string roleJson = "[{\"Permisos\":\"[{\\\"Opcion\\\":\\\"05001000\\\",\\\"Permisos\\\":{\\\"Acceso\\\":1,\\\"Escritura\\\":1},\\\"Hijos\\\":[]}]\"}]";
        var controller=new MvcController(new StaticHttp(roleJson),new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"fireBdata:fireClave","SYNTHETIC_TEST_KEY"},{"ProductosServicios:PermissionCode","05001000"}}).Build());
        controller.ControllerContext=new ControllerContext{HttpContext=http};
        var action=new ActionContext(http,new RouteData(),new ActionDescriptor());
        var ctx=new ActionExecutingContext(action,new List<IFilterMetadata>(),new Dictionary<string,object?>(),controller);
        bool nextCalled=false;
        await controller.OnActionExecutionAsync(ctx,()=>{nextCalled=true;return Task.FromResult(new ActionExecutedContext(action,new List<IFilterMetadata>(),controller));});
        Assert.False(nextCalled);
        Assert.Equal(403,Assert.IsAssignableFrom<ObjectResult>(ctx.Result).StatusCode);
    }

    [Fact]
    public async Task ParentWriteDoesNotGrantAbcWhenAbcIsReadonly()
    {
        var id=Guid.NewGuid();
        var claims=new List<Claim>{new(ClaimTypes.SerialNumber,id.ToString()),new(ClaimTypes.Sid,"QA-A"),new(ClaimTypes.NameIdentifier,"firebase-non-guid"),new(ClaimTypes.Role,Guid.NewGuid().ToString()),new(ClaimTypes.Uri,"QA-CADENA")};
        var http=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity(claims,"QA")),Session=new Session()};
        const string roleJson = "[{\"Permisos\":\"[{\\\"Opcion\\\":\\\"05001000\\\",\\\"Permisos\\\":{\\\"Acceso\\\":1,\\\"Escritura\\\":1},\\\"Hijos\\\":[{\\\"Opcion\\\":\\\"05001001\\\",\\\"Permisos\\\":{\\\"Acceso\\\":1,\\\"Escritura\\\":0},\\\"Hijos\\\":[]}]}]\"}]";
        var controller=new MvcController(new StaticHttp(roleJson),new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"fireBdata:fireClave","SYNTHETIC_TEST_KEY"},{"ProductosServicios:PermissionCode","05001000"}}).Build());
        controller.ControllerContext=new ControllerContext{HttpContext=http};
        var action=new ActionContext(http,new RouteData(),new ActionDescriptor());
        var ctx=new ActionExecutingContext(action,new List<IFilterMetadata>(),new Dictionary<string,object?>(),controller);
        bool nextCalled=false;
        await controller.OnActionExecutionAsync(ctx,()=>{nextCalled=true;return Task.FromResult(new ActionExecutedContext(action,new List<IFilterMetadata>(),controller));});
        Assert.True(nextCalled);
        Assert.Null(ctx.Result);
    }
}

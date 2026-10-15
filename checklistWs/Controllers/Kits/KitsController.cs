using System.Security.Cryptography;
using System.Text;
using checklistWs.Models.Kits;
using checklistWs.Services.Tenant;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.Kits;

[ApiController]
[Route("api/[controller]")]
public sealed class KitsController : ControllerBase
{
    private const string ProxyContextItemKey = "__KitsProxyContext";
    private const string ReadPermission = ProductosServiciosAuthorizationDefaults.KitsPermissionCode;
    private const string WritePermission = ProductosServiciosAuthorizationDefaults.KitsAdministrarPermissionCode;
    private readonly IConfiguration _configuration;
    private readonly ITenantDatabaseResolver _resolver;
    private readonly ITenantSqlConnectionFactory _connections;
    private readonly IProductosServiciosCompatibilityGate _gate;
    private readonly IProductosServiciosAuthorizationService _authorization;
    private readonly ILogger<KitsController> _logger;

    public KitsController(IConfiguration configuration, ITenantDatabaseResolver resolver, ITenantSqlConnectionFactory connections,
        IProductosServiciosCompatibilityGate gate, IProductosServiciosAuthorizationService authorization, ILogger<KitsController> logger)
    {
        _configuration=configuration;_resolver=resolver;_connections=connections;_gate=gate;_authorization=authorization;_logger=logger;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] KitListQuery query,Guid? idEmpresa=null)=>await ExecuteAsync(false,async context=>Ok(await Service(context).ListAsync(Actor(context),query,RequestAborted)),idEmpresa);

    [HttpGet("{idKit:guid}")]
    public async Task<IActionResult> Detail(Guid idKit,Guid? idEmpresa=null)=>await ExecuteAsync(false,async context=>Ok(await Service(context).GetAsync(Actor(context),idKit,RequestAborted)),idEmpresa);

    [HttpGet("Opciones")]
    public async Task<IActionResult> Options(string search="",byte? tipoKit=null,Guid? idEmpresa=null)=>await ExecuteAsync(false,async context=>Ok(await Service(context).OptionsAsync(Actor(context),search,tipoKit,RequestAborted)),idEmpresa);

    [HttpPost("ResolverPrecio")]
    public async Task<IActionResult> ResolvePrice([FromBody] KitPriceResolutionRequest request,Guid? idEmpresa=null)=>await ExecuteAsync(false,async context=>
    {
        KitPriceResolutionResponse response=await Service(context).ResolvePriceAsync(Actor(context),request,RequestAborted);
        return response.Resuelto?Ok(response):Conflict(response);
    },idEmpresa);

    [HttpPost("SubirImagenTemporal")]
    [RequestFormLimits(MultipartBodyLengthLimit = 12L * 1024L * 1024L)]
    [RequestSizeLimit(12L * 1024L * 1024L)]
    public async Task<IActionResult> UploadTemporaryImage(Guid? idEmpresa, IFormFile? archivo) => await ExecuteAsync(true, async context =>
        Ok(await Images().UploadTemporaryAsync(context.EmpresaStorageKey, archivo, RequestAborted)), idEmpresa);

    [HttpPost("LimpiarImagenTemporal")]
    public async Task<IActionResult> CleanupTemporaryImage([FromBody] KitImageCleanupRequest request, Guid? idEmpresa = null) => await ExecuteAsync(true, async context =>
    {
        await Images().CleanupTemporaryAsync(context.EmpresaStorageKey, request?.TemporalToken);
        return Ok(new KitOperationResponse { Message = "La limpieza temporal fue procesada." });
    }, idEmpresa);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] KitCreateRequest request,Guid? idEmpresa=null)=>await ExecuteAsync(true,async context=>
    {
        if (request == null) throw new KitsDomainException("REQUEST_INVALIDO", "La solicitud es inválida.");
        KitsImageStorage images = Images();
        IKitsService service = Service(context);
        KitActor actor = Actor(context);
        KitPreparedImage? prepared = null;
        try
        {
            KitDetailResponse? existing = await service.FindByOperationKeyAsync(actor, request.OperationKey, RequestAborted);
            if (existing != null)
            {
                request.PreparedId = existing.Id;
                request.ImagenUrl = existing.ImagenUrl;
                request.ImagenNombre = existing.ImagenNombre;
                KitOperationResponse replay = await service.CreateAsync(actor, request, RequestAborted);
                if (!string.IsNullOrWhiteSpace(request.ImagenTemporalToken))
                    await images.CleanupTemporaryAsync(context.EmpresaStorageKey, request.ImagenTemporalToken);
                return Ok(replay);
            }
            request.PreparedId = Guid.NewGuid();
            if (!string.IsNullOrWhiteSpace(request.ImagenTemporalToken))
            {
                prepared = await images.PromoteAsync(context.EmpresaStorageKey, request.PreparedId.Value, request.ImagenTemporalToken);
                request.ImagenUrl = prepared.Url;
                request.ImagenNombre = prepared.OriginalName;
            }
            KitOperationResponse response = await service.CreateAsync(actor, request, RequestAborted);
            if (prepared != null) await images.CleanupTemporaryAsync(context.EmpresaStorageKey, request.ImagenTemporalToken);
            return Ok(response);
        }
        catch
        {
            if (prepared != null) await images.DeleteFinalQuietlyAsync(prepared);
            throw;
        }
    },idEmpresa);

    [HttpPut("{idKit:guid}")]
    public async Task<IActionResult> Update(Guid idKit,[FromBody] KitUpdateRequest request,Guid? idEmpresa=null)=>await ExecuteAsync(true,async context=>
    {
        if (request == null) throw new KitsDomainException("REQUEST_INVALIDO", "La solicitud es inválida.");
        KitsImageStorage images = Images();
        KitPreparedImage? prepared = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(request.ImagenTemporalToken))
            {
                prepared = await images.PromoteAsync(context.EmpresaStorageKey, idKit, request.ImagenTemporalToken);
                request.ImagenUrl = prepared.Url;
                request.ImagenNombre = prepared.OriginalName;
            }
            KitOperationResponse response = await Service(context).UpdateAsync(Actor(context),idKit,request,RequestAborted);
            if (prepared != null) await images.CleanupTemporaryAsync(context.EmpresaStorageKey, request.ImagenTemporalToken);
            return Ok(response);
        }
        catch
        {
            if (prepared != null) await images.DeleteFinalQuietlyAsync(prepared);
            throw;
        }
    },idEmpresa);

    [HttpPost("{idKit:guid}/Baja")]
    public async Task<IActionResult> Archive(Guid idKit,Guid? idEmpresa=null)=>await ExecuteAsync(true,async context=>Ok(await Service(context).ArchiveAsync(Actor(context),idKit,RequestAborted)),idEmpresa);

    [HttpPost("{idKit:guid}/Reactivar")]
    public async Task<IActionResult> Reactivate(Guid idKit,Guid? idEmpresa=null)=>await ExecuteAsync(true,async context=>Ok(await Service(context).ReactivateAsync(Actor(context),idKit,RequestAborted)),idEmpresa);

    [HttpPost("{idKit:guid}/Componentes")]
    public async Task<IActionResult> AddComponent(Guid idKit,[FromBody] KitComponentCreateRequest request,Guid? idEmpresa=null)=>await ExecuteAsync(true,async context=>Ok(await Service(context).AddComponentAsync(Actor(context),idKit,request,RequestAborted)),idEmpresa);

    [HttpPut("{idKit:guid}/Componentes/{idComponent:guid}")]
    public async Task<IActionResult> UpdateComponent(Guid idKit,Guid idComponent,[FromBody] KitComponentUpdateRequest request,Guid? idEmpresa=null)=>await ExecuteAsync(true,async context=>Ok(await Service(context).UpdateComponentAsync(Actor(context),idKit,idComponent,request,RequestAborted)),idEmpresa);

    [HttpPost("{idKit:guid}/Componentes/{idComponent:guid}/Baja")]
    public async Task<IActionResult> ArchiveComponent(Guid idKit,Guid idComponent,Guid? idEmpresa=null)=>await ExecuteAsync(true,async context=>Ok(await Service(context).ArchiveComponentAsync(Actor(context),idKit,idComponent,RequestAborted)),idEmpresa);

    [HttpPost("{idKit:guid}/Componentes/{idComponent:guid}/Reactivar")]
    public async Task<IActionResult> ReactivateComponent(Guid idKit,Guid idComponent,Guid? idEmpresa=null)=>await ExecuteAsync(true,async context=>Ok(await Service(context).ReactivateComponentAsync(Actor(context),idKit,idComponent,RequestAborted)),idEmpresa);

    [HttpGet("{idKit:guid}/Historial")]
    public async Task<IActionResult> History(Guid idKit,Guid? idEmpresa=null)=>await ExecuteAsync(false,async context=>Ok(await Service(context).HistoryAsync(Actor(context),idKit,RequestAborted)),idEmpresa);

    private async Task<IActionResult> ExecuteAsync(bool write,Func<RequestContext,Task<IActionResult>> action,Guid? clientEmpresa)
    {
        try
        {
            (RequestContext? context,IActionResult? error)=await ResolveContextAsync(write,clientEmpresa);
            if(error!=null)return error;
            return await action(context!);
        }
        catch(KitsDomainException ex){return BusinessError(ex);}
        catch(Exception ex)
        {
            Guid reference=Guid.NewGuid();
            _logger.LogError(ex,"Error no controlado en Kits. ReferenceId={ReferenceId}",reference);
            return StatusCode(500,new KitOperationResponse{Code="KITS_ERROR",Message="No fue posible completar la operación.",CorrelationId=reference});
        }
    }

    private async Task<(RequestContext?,IActionResult?)> ResolveContextAsync(bool write,Guid? clientEmpresa)
    {
        if(!ProductosServiciosIdentityValidation.TryValidate(User,Request.Headers,_configuration["fireBdata:fireClave"]??string.Empty,DateTimeOffset.UtcNow,out TenantDatabaseContext verified,out string userId))
            return (null,Unauthorized(new KitOperationResponse{Code="TENANT_CONTEXT_INVALID",Message="No fue posible resolver la empresa activa."}));
        if(clientEmpresa.HasValue&&clientEmpresa.Value!=Guid.Empty&&clientEmpresa.Value!=verified.IdEmpresa)return(null,StatusCode(403,new KitOperationResponse{Code="TENANT_INPUT_MISMATCH",Message="No fue posible autorizar la solicitud."}));
        foreach(var input in Request.Query)if(!TenantInputMatches(input.Key,input.Value,verified.IdEmpresa,verified.EmpresaKey))return(null,StatusCode(403,new KitOperationResponse{Code="TENANT_INPUT_MISMATCH",Message="No fue posible autorizar la solicitud."}));
        TenantDatabaseDescriptor descriptor;
        try{descriptor=await _resolver.ResolveAsync(verified,RequestAborted);}catch(TenantDatabaseResolutionException ex){return(null,StatusCode(503,new KitOperationResponse{Code=ex.Code.ToString(),Message="No fue posible resolver la base de datos de la empresa."}));}
        string permission=write?WritePermission:ReadPermission;
        if(write)
        {
            ProductosServiciosAuthorizationDecision read=await _authorization.AuthorizeAsync(new ProductosServiciosAuthorizationRequest{IdEmpresa=verified.IdEmpresa,UserId=userId,TenantDatabase=descriptor,Requirement=ProductosServiciosPermissionRequirement.Read,PermissionCode=ReadPermission},RequestAborted);
            if(!read.IsAllowed(ProductosServiciosPermissionRequirement.Read))return(null,Forbidden(read.ReferenceId));
        }
        ProductosServiciosPermissionRequirement requirement=write?ProductosServiciosPermissionRequirement.Write:ProductosServiciosPermissionRequirement.Read;
        ProductosServiciosAuthorizationDecision auth=await _authorization.AuthorizeAsync(new ProductosServiciosAuthorizationRequest{IdEmpresa=verified.IdEmpresa,UserId=userId,TenantDatabase=descriptor,Requirement=requirement,PermissionCode=permission},RequestAborted);
        if(!auth.IsAllowed(requirement))return(null,Forbidden(auth.ReferenceId));
        CompatibilityDecision compatibility=await _gate.EvaluateAsync(descriptor,DatabaseScopes.Kits,RequestAborted);
        if(!compatibility.IsAllowed)
        {
            _logger.LogWarning("Gate Kits bloqueó operación. ReferenceId={ReferenceId} IdentityHash={IdentityHash} Reason={Reason}",compatibility.ReferenceId,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(compatibility.SanitizedIdentity??string.Empty))),compatibility.ReasonCode);
            return(null,StatusCode(503,new KitOperationResponse{Code=compatibility.ReasonCode,Message="Kits no está disponible temporalmente."}));
        }
        Guid? actor=Guid.TryParse(userId,out Guid parsed)&&parsed!=Guid.Empty?parsed:null;RequestContext context=new(){IdEmpresa=verified.IdEmpresa,EmpresaStorageKey=verified.EmpresaKey,Descriptor=descriptor,UserId=userId,ActorId=actor};HttpContext.Items[ProxyContextItemKey]=context;return(context,null);
    }

    private IKitsService Service(RequestContext context)=>new KitsService(_connections,context.Descriptor,new ListaPreciosService(new SqlListaPreciosRepository(_connections,context.Descriptor)));
    private KitsImageStorage Images()=>new(_configuration,_logger);
    private static KitActor Actor(RequestContext context)=>new(context.IdEmpresa,context.ActorId);
    private IActionResult Forbidden(string reference)=>StatusCode(403,new{code="KITS_FORBIDDEN",message="No tienes permiso para realizar esta operación.",referenceId=reference});
    private static IActionResult BusinessError(KitsDomainException ex)
    {
        KitOperationResponse payload=new(){Code=ex.Code,Message=ex.Message,Details=ex.Details};int status=ex.Code switch{"KIT_NO_ENCONTRADO" or "COMPONENTE_NO_ENCONTRADO"=>404,"KIT_NOMBRE_DUPLICADO" or "COMPONENTE_DUPLICADO" or "IDEMPOTENCY_CONFLICT" or "CAMBIO_TIPO_CONFLICTO" or "FUENTE_INACTIVA" or "KIT_INACTIVO"=>409,_=>400};return new ObjectResult(payload){StatusCode=status};
    }
    private static bool TenantInputMatches(string key,Microsoft.Extensions.Primitives.StringValues values,Guid id,string company)
    {
        if(new[]{"cadena","connectionString","server","database","password"}.Contains(key,StringComparer.OrdinalIgnoreCase))return false;
        if(new[]{"idEmpresa","empresaId","tenantId"}.Contains(key,StringComparer.OrdinalIgnoreCase))return values.All(v=>Guid.TryParse(v,out Guid parsed)&&parsed==id);
        if(new[]{"empresa","empresaKey"}.Contains(key,StringComparer.OrdinalIgnoreCase))return values.All(v=>string.Equals(v?.Trim(),company,StringComparison.OrdinalIgnoreCase));return true;
    }
    private CancellationToken RequestAborted=>HttpContext.RequestAborted;
    private sealed class RequestContext{public Guid IdEmpresa{get;init;}public string EmpresaStorageKey{get;init;}=string.Empty;public TenantDatabaseDescriptor Descriptor{get;init;}=null!;public string UserId{get;init;}=string.Empty;public Guid? ActorId{get;init;}}
}

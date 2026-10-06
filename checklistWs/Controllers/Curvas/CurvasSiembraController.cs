using checklistWs.Services.Tenant;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.Curvas;

[Route("api/[controller]")]
[ApiController]
public sealed class CurvasSiembraController : ControllerBase
{
    private readonly ITenantDatabaseResolver _tenantResolver;
    private readonly IProductosServiciosAuthorizationService _authorizationService;
    private readonly IProductosServiciosCompatibilityGate _compatibilityGate;
    private readonly ICurvasScopeService _curvasService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CurvasSiembraController> _logger;

    public CurvasSiembraController(
        ITenantDatabaseResolver tenantResolver,
        IProductosServiciosAuthorizationService authorizationService,
        IProductosServiciosCompatibilityGate compatibilityGate,
        ICurvasScopeService curvasService,
        IConfiguration configuration,
        ILogger<CurvasSiembraController> logger)
    {
        _tenantResolver = tenantResolver;
        _authorizationService = authorizationService;
        _compatibilityGate = compatibilityGate;
        _curvasService = curvasService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("Sucursales")]
    public async Task<IActionResult> Sucursales()
    {
        TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(Guid.Empty, string.Empty);
        IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Read);
        return guard ?? Ok(await _curvasService.ListarSucursalesSiembraAsync(descriptor!, descriptor!.IdEmpresa, HttpContext.RequestAborted));
    }

    [HttpGet("CurvasActivas")]
    public async Task<IActionResult> CurvasActivas()
    {
        TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(Guid.Empty, string.Empty);
        IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Read);
        return guard ?? Ok(await _curvasService.ListarCurvasActivasSiembraAsync(descriptor!, descriptor!.IdEmpresa, HttpContext.RequestAborted));
    }

    [HttpGet("DetalleCurva")]
    public async Task<IActionResult> DetalleCurva(Guid idCurva)
    {
        TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(Guid.Empty, string.Empty);
        IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Read);
        if (guard != null) return guard;
        CurvasCatalogoDetalleDto? curva = await _curvasService.ObtenerCatalogoAsync(descriptor!, descriptor!.IdEmpresa, idCurva, HttpContext.RequestAborted);
        return curva == null || !curva.Activo
            ? NotFound(new { exito = false, mensaje = "La curva activa no está disponible." })
            : Ok(curva);
    }

    [HttpGet("Vigentes")]
    public async Task<IActionResult> Vigentes(Guid? idSucursal = null)
    {
        TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(Guid.Empty, string.Empty);
        IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Read);
        return guard ?? Ok(await _curvasService.ListarSiembrasVigentesAsync(descriptor!, descriptor!.IdEmpresa, idSucursal, HttpContext.RequestAborted));
    }

    [HttpPost("Sembrar")]
    public async Task<IActionResult> Sembrar([FromBody] CurvasSembrarCurvaRequest request)
    {
        TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(Guid.Empty, string.Empty);
        IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Write);
        if (guard != null) return guard;
        if (request == null || request.IdSucursal == Guid.Empty || request.IdCurva == Guid.Empty)
        {
            return BadRequest(new CurvasSembrarCurvaResponse { Mensaje = "Selecciona una sucursal y una curva." });
        }

        try
        {
            CurvasCatalogoDetalleDto? curva = await _curvasService.ObtenerCatalogoAsync(descriptor!, descriptor!.IdEmpresa, request.IdCurva, HttpContext.RequestAborted);
            if (curva == null || !curva.Activo || curva.Detalles.Count == 0)
            {
                return BadRequest(new CurvasSembrarCurvaResponse { Mensaje = "La curva activa no contiene objetivos disponibles." });
            }

            Guid? usuarioId = TryResolveUsuarioGuid();
            foreach (CurvasCatalogoDetalleRenglonDto detalle in curva.Detalles)
            {
                await _curvasService.SembrarAsync(descriptor, descriptor.IdEmpresa, new CurvaSiembraRequest
                {
                    IdSucursal = request.IdSucursal,
                    IdProductoServicio = detalle.IdProductoServicio,
                    IdVariante = detalle.IdVariante,
                    IdCurva = curva.Id,
                    UsuarioId = usuarioId
                }, HttpContext.RequestAborted);
            }

            IReadOnlyList<CurvasSiembraVigenteDto> vigentes = await _curvasService.ListarSiembrasVigentesAsync(descriptor, descriptor.IdEmpresa, request.IdSucursal, HttpContext.RequestAborted);
            return Ok(new CurvasSembrarCurvaResponse
            {
                Exito = true,
                Mensaje = "Curva sembrada correctamente.",
                SiembrasAplicadas = curva.Detalles.Count,
                Vigentes = vigentes
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new CurvasSembrarCurvaResponse { Mensaje = ex.Message });
        }
    }

    [HttpPost("Cerrar")]
    public async Task<IActionResult> Cerrar([FromBody] CurvasCerrarSiembraRequest request)
    {
        TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(Guid.Empty, string.Empty);
        IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Write);
        if (guard != null) return guard;
        if (request == null || request.IdSiembra == Guid.Empty)
        {
            return BadRequest(new CurvasCerrarSiembraResponse { Mensaje = "Selecciona una siembra vigente." });
        }

        try
        {
            bool cerrada = await _curvasService.CerrarSiembraAsync(
                descriptor!,
                descriptor!.IdEmpresa,
                request.IdSiembra,
                TryResolveUsuarioGuid(),
                HttpContext.RequestAborted);
            return Ok(new CurvasCerrarSiembraResponse
            {
                Exito = true,
                Cerrada = cerrada,
                Mensaje = cerrada
                    ? "La siembra se quitó correctamente y su histórico fue conservado."
                    : "La siembra ya no está vigente o no está disponible para la empresa activa."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new CurvasCerrarSiembraResponse { Mensaje = ex.Message });
        }
    }

    private async Task<IActionResult?> GuardAsync(TenantDatabaseDescriptor? descriptor, ProductosServiciosPermissionRequirement requirement)
    {
        if (descriptor == null) return Unauthorized(new { exito = false, mensaje = "No fue posible resolver la empresa activa." });
        string userId = TryResolveUsuarioId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { exito = false, mensaje = "No fue posible resolver el usuario activo." });
        ProductosServiciosAuthorizationDecision authorization = await _authorizationService.AuthorizeAsync(new ProductosServiciosAuthorizationRequest
        {
            IdEmpresa = descriptor.IdEmpresa,
            UserId = userId,
            PermissionCode = ProductosServiciosAuthorizationDefaults.CurvasSiembraPermissionCode,
            Requirement = requirement,
            TenantDatabase = descriptor
        }, HttpContext.RequestAborted);
        if (!authorization.IsAllowed(requirement))
        {
            return StatusCode(403, new { exito = false, mensaje = $"No tienes permiso para administrar la siembra de curvas. Ref: {authorization.ReferenceId}" });
        }
        CompatibilityDecision compatibility = await _compatibilityGate.EvaluateAsync(descriptor, DatabaseScopes.Curvas, HttpContext.RequestAborted);
        return compatibility.IsAllowed
            ? null
            : StatusCode(503, new { exito = false, mensaje = $"Siembra de curvas no disponible. Ref: {compatibility.ReferenceId}" });
    }

    private async Task<TenantDatabaseDescriptor?> ResolveDescriptorAsync(Guid idEmpresa, string empresaKey)
    {
        if (!TryResolveIdentity(out TenantDatabaseContext context, out _)) return null;
        try
        {
            return await _tenantResolver.ResolveAsync(context, HttpContext.RequestAborted);
        }
        catch (TenantDatabaseResolutionException ex)
        {
            _logger.LogWarning(ex, "No fue posible resolver tenant para Siembra de Curvas. IdEmpresa={IdEmpresa}", context.IdEmpresa);
            return null;
        }
    }

    private string TryResolveUsuarioId()
        => TryResolveIdentity(out _, out string userId) ? userId : string.Empty;

    private bool TryResolveIdentity(out TenantDatabaseContext context, out string userId)
        => ProductosServiciosIdentityValidation.TryValidate(
            User,
            Request.Headers,
            _configuration["fireBdata:fireClave"] ?? string.Empty,
            DateTimeOffset.UtcNow,
            out context,
            out userId);

    private Guid? TryResolveUsuarioGuid()
        => Guid.TryParse(TryResolveUsuarioId(), out Guid id) && id != Guid.Empty ? id : null;
}

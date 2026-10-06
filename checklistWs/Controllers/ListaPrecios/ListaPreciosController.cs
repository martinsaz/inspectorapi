using System.Security.Cryptography;
using System.Text;
using checklistWs.Services.Tenant;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.ListaPrecios
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class ListaPreciosController : ControllerBase
    {
        private const string ProxyContextItemKey = "__ListaPreciosProxyContext";
        private const string AccessPermissionCode = ProductosServiciosAuthorizationDefaults.ListaPreciosPermissionCode;
        private const string AdminPermissionCode = ProductosServiciosAuthorizationDefaults.ListaPreciosAdministrarPermissionCode;

        private readonly IConfiguration _configuration;
        private readonly ITenantDatabaseResolver _tenantDatabaseResolver;
        private readonly ITenantSqlConnectionFactory _connectionFactory;
        private readonly IProductosServiciosCompatibilityGate _compatibilityGate;
        private readonly IProductosServiciosAuthorizationService _authorizationService;
        private readonly ILogger<ListaPreciosController> _logger;

        public ListaPreciosController(
            IConfiguration configuration,
            ITenantDatabaseResolver tenantDatabaseResolver,
            ITenantSqlConnectionFactory connectionFactory,
            IProductosServiciosCompatibilityGate compatibilityGate,
            IProductosServiciosAuthorizationService authorizationService,
            ILogger<ListaPreciosController> logger)
        {
            _configuration = configuration;
            _tenantDatabaseResolver = tenantDatabaseResolver;
            _connectionFactory = connectionFactory;
            _compatibilityGate = compatibilityGate;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        [HttpGet("Listas")]
        public async Task<IActionResult> ObtenerListas(Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return Ok(await Service(context).ObtenerListasAsync(context.IdEmpresa, HttpContext.RequestAborted));
        }

        [HttpGet("Combos")]
        public async Task<IActionResult> ObtenerCombos(Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return Ok(await Service(context).ObtenerCombosAsync(context.IdEmpresa, HttpContext.RequestAborted));
        }

        [HttpGet("Consulta")]
        public async Task<IActionResult> Consultar(
            Guid? idEmpresa = null,
            int? nivel = null,
            string busqueda = "",
            byte? tipo = null,
            Guid? idCategoria = null,
            Guid? idMarca = null,
            Guid? idColeccion = null,
            Guid? idEtiqueta = null,
            Guid? idAtributo = null,
            string valoresAtributo = "",
            Guid? idVariante = null,
            Guid? idPresentacionVenta = null,
            decimal? precioMinimo = null,
            decimal? precioMaximo = null,
            string descuento = "",
            decimal? descuentoPct = null,
            Guid? idSucursal = null,
            string sucursales = "",
            string existencia = "",
            decimal? cantidadMenorA = null,
            string estatus = "activos")
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            List<Guid> idsSucursales = new();
            if (!string.IsNullOrWhiteSpace(sucursales))
            {
                foreach (string token in sucursales.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!Guid.TryParse(token, out Guid parsed) || parsed == Guid.Empty)
                        return BadRequest(new { code = "SUCURSAL_INVALIDA", message = "La selección de sucursales no es válida." });
                    idsSucursales.Add(parsed);
                }
            }

            List<Guid> idsValoresAtributo = new();
            if (!string.IsNullOrWhiteSpace(valoresAtributo))
            {
                foreach (string token in valoresAtributo.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!Guid.TryParse(token, out Guid parsed) || parsed == Guid.Empty)
                        return BadRequest(new { code = "ATRIBUTO_VALOR_INVALIDO", message = "La selección de valores del atributo no es válida." });
                    idsValoresAtributo.Add(parsed);
                }
            }

            ListaPreciosConsultaRequest request = new()
            {
                Nivel = nivel,
                Busqueda = busqueda ?? string.Empty,
                Tipo = tipo,
                IdCategoria = idCategoria,
                IdMarca = idMarca,
                IdColeccion = idColeccion,
                IdEtiqueta = idEtiqueta,
                IdAtributo = idAtributo,
                IdsValoresAtributo = idsValoresAtributo,
                IdVariante = idVariante,
                IdPresentacionVenta = idPresentacionVenta,
                PrecioMinimo = precioMinimo,
                PrecioMaximo = precioMaximo,
                Descuento = descuento ?? string.Empty,
                DescuentoPct = descuentoPct,
                IdSucursal = idSucursal,
                IdsSucursales = idsSucursales,
                Existencia = existencia ?? string.Empty,
                CantidadMenorA = cantidadMenorA,
                Estatus = estatus ?? "activos"
            };

            try
            {
                return Ok(await Service(context).ConsultarAsync(context.IdEmpresa, request, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex)
            {
                return ToBusinessError(ex.Message);
            }
        }

        [HttpGet("Inventario")]
        public async Task<IActionResult> ObtenerInventario(
            Guid idProductoServicio,
            byte? tipoIdentidad = null,
            Guid? idVariante = null,
            Guid? idPresentacionVenta = null,
            Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                return Ok(await Service(context).ObtenerInventarioDetalleAsync(context.IdEmpresa, new ListaPreciosResolverRequest
                {
                    TipoIdentidad = tipoIdentidad,
                    IdProductoServicio = idProductoServicio,
                    IdVariante = idVariante,
                    IdPresentacionVenta = idPresentacionVenta,
                    RequiereActivo = true
                }, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex)
            {
                return ToBusinessError(ex.Message);
            }
        }

        [HttpGet("PreciosProducto")]
        public async Task<IActionResult> ObtenerPreciosProducto(Guid idProductoServicio, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return Ok(await Service(context).ObtenerPreciosPorProductoAsync(context.IdEmpresa, idProductoServicio, HttpContext.RequestAborted));
        }

        [HttpGet("Editor")]
        public async Task<IActionResult> ObtenerEditor(Guid idProductoServicio, byte? tipoIdentidad = null, Guid? idVariante = null, Guid? idPresentacionVenta = null, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error)) return error!;
            try
            {
                return Ok(await Service(context).ObtenerEditorAsync(context.IdEmpresa, new ListaPreciosResolverRequest
                {
                    TipoIdentidad = tipoIdentidad, IdProductoServicio = idProductoServicio,
                    IdVariante = idVariante, IdPresentacionVenta = idPresentacionVenta, RequiereActivo = true
                }, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex) { return ToBusinessError(ex.Message); }
        }

        [HttpPost("Resolver")]
        public async Task<IActionResult> Resolver([FromBody] ListaPreciosResolverRequest? request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            ListaPreciosResolutionResult result = await Service(context).ResolverPrecioAsync(context.IdEmpresa, request!, HttpContext.RequestAborted);
            return result.CodigoResolucion switch
            {
                ListaPreciosResolutionCodes.FueraDeV1 => BadRequest(result),
                ListaPreciosResolutionCodes.IdentidadInvalida => BadRequest(result),
                ListaPreciosResolutionCodes.IdentidadInactiva => StatusCode(409, result),
                _ => Ok(result)
            };
        }

        [HttpPost("GuardarPrecio")]
        public async Task<IActionResult> GuardarPrecio([FromBody] ListaPreciosGuardarPrecioRequest? request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AdminPermissionCode, ProductosServiciosPermissionRequirement.Write, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                if (request == null) return BadRequest(new ListaPreciosOperacionResponse { Code = "REQUEST_INVALIDO", Mensaje = "Solicitud inválida." });
                request.UsuarioId ??= TryResolveUsuarioId();
                Guid id = await Service(context).GuardarPrecioAsync(context.IdEmpresa, request, HttpContext.RequestAborted);
                return Ok(new ListaPreciosOperacionResponse { Code = "OK", Mensaje = "Precio guardado.", Id = id });
            }
            catch (InvalidOperationException ex)
            {
                return ToBusinessError(ex.Message);
            }
        }

        [HttpPost("Preview")]
        public async Task<IActionResult> Preview([FromBody] ListaPreciosPreviewRequest? request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                if (request == null) return BadRequest(new ListaPreciosOperacionResponse { Code = "REQUEST_INVALIDO", Mensaje = "Solicitud inválida." });
                ListaPreciosResolutionResult result = await Service(context).PreviewAsync(context.IdEmpresa, request, HttpContext.RequestAborted);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return ToBusinessError(ex.Message);
            }
        }

        [HttpPost("PreviewMatriz")]
        public async Task<IActionResult> PreviewMatriz([FromBody] ListaPreciosMatrizRequest? request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
                return error!;
            try
            {
                if (request == null) return BadRequest(new { code = "REQUEST_INVALIDO", message = "Solicitud inválida." });
                request.UsuarioId ??= TryResolveUsuarioId();
                return Ok(await Service(context).PreviewMatrizAsync(context.IdEmpresa, request, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex) { return ToBusinessError(ex.Message); }
        }

        [HttpPost("GuardarMatriz")]
        public async Task<IActionResult> GuardarMatriz([FromBody] ListaPreciosMatrizRequest? request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AdminPermissionCode, ProductosServiciosPermissionRequirement.Write, out RequestContext context, out IActionResult? error))
                return error!;
            try
            {
                if (request == null) return BadRequest(new { code = "REQUEST_INVALIDO", message = "Solicitud inválida." });
                request.UsuarioId ??= TryResolveUsuarioId();
                return Ok(await Service(context).GuardarMatrizAsync(context.IdEmpresa, request, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex) { return ToBusinessError(ex.Message); }
        }

        [HttpPost("PreviewAjusteMasivo")]
        public async Task<IActionResult> PreviewAjusteMasivo([FromBody] ListaPreciosAjusteMasivoRequest? request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                if (request == null) return BadRequest(new ListaPreciosOperacionResponse { Code = "REQUEST_INVALIDO", Mensaje = "Solicitud inválida." });
                return Ok(await Service(context).PreviewAjusteMasivoAsync(context.IdEmpresa, request, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex)
            {
                return ToBusinessError(ex.Message);
            }
        }

        [HttpPost("EjecutarAjusteMasivo")]
        public async Task<IActionResult> EjecutarAjusteMasivo([FromBody] ListaPreciosAjusteMasivoRequest? request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AdminPermissionCode, ProductosServiciosPermissionRequirement.Write, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                if (request == null) return BadRequest(new ListaPreciosOperacionResponse { Code = "REQUEST_INVALIDO", Mensaje = "Solicitud inválida." });
                request.UsuarioId ??= TryResolveUsuarioId();
                return Ok(await Service(context).EjecutarAjusteMasivoAsync(context.IdEmpresa, request, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex)
            {
                return ToBusinessError(ex.Message);
            }
        }

        [HttpPost("PreviewCopiarLista")]
        public async Task<IActionResult> PreviewCopiarLista([FromBody] ListaPreciosCopiarListaRequest? request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                if (request == null) return BadRequest(new ListaPreciosOperacionResponse { Code = "REQUEST_INVALIDO", Mensaje = "Solicitud inválida." });
                return Ok(await Service(context).PreviewCopiarListaAsync(context.IdEmpresa, request, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex)
            {
                return ToBusinessError(ex.Message);
            }
        }

        [HttpPost("EjecutarCopiarLista")]
        public async Task<IActionResult> EjecutarCopiarLista([FromBody] ListaPreciosCopiarListaRequest? request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AdminPermissionCode, ProductosServiciosPermissionRequirement.Write, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                if (request == null) return BadRequest(new ListaPreciosOperacionResponse { Code = "REQUEST_INVALIDO", Mensaje = "Solicitud inválida." });
                request.UsuarioId ??= TryResolveUsuarioId();
                return Ok(await Service(context).EjecutarCopiarListaAsync(context.IdEmpresa, request, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex)
            {
                return ToBusinessError(ex.Message);
            }
        }

        [HttpPost("PreviewDescuentoMarca")]
        public async Task<IActionResult> PreviewDescuentoMarca([FromBody] ListaPreciosDescuentoMarcaRequest? request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                if (request == null) return BadRequest(new ListaPreciosOperacionResponse { Code = "REQUEST_INVALIDO", Mensaje = "Solicitud inválida." });
                return Ok(await Service(context).PreviewDescuentoMarcaAsync(context.IdEmpresa, request, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex)
            {
                return ToBusinessError(ex.Message);
            }
        }

        [HttpPost("EjecutarDescuentoMarca")]
        public async Task<IActionResult> EjecutarDescuentoMarca([FromBody] ListaPreciosDescuentoMarcaRequest? request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AdminPermissionCode, ProductosServiciosPermissionRequirement.Write, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                if (request == null) return BadRequest(new ListaPreciosOperacionResponse { Code = "REQUEST_INVALIDO", Mensaje = "Solicitud inválida." });
                request.UsuarioId ??= TryResolveUsuarioId();
                return Ok(await Service(context).EjecutarDescuentoMarcaAsync(context.IdEmpresa, request, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex)
            {
                return ToBusinessError(ex.Message);
            }
        }

        [HttpPost("Historial")]
        public async Task<IActionResult> Historial([FromBody] ListaPreciosResolverRequest? request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                if (request == null) return BadRequest(new ListaPreciosOperacionResponse { Code = "REQUEST_INVALIDO", Mensaje = "Solicitud inválida." });
                return Ok(await Service(context).ObtenerHistorialAsync(context.IdEmpresa, request, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex)
            {
                return ToBusinessError(ex.Message);
            }
        }

        [HttpGet("HistorialConsulta")]
        public async Task<IActionResult> HistorialConsulta([FromQuery] ListaPreciosHistorialConsultaRequest request, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                return Ok(await Service(context).ConsultarHistorialAsync(context.IdEmpresa, request, HttpContext.RequestAborted));
            }
            catch (InvalidOperationException ex)
            {
                return ToBusinessError(ex.Message);
            }
        }

        [HttpPost("BajaPrecio/{idPrecio:guid}")]
        public async Task<IActionResult> BajaPrecio(Guid idPrecio, Guid? idEmpresa = null)
        {
            if (!await TryResolveContextAsync(idEmpresa, AdminPermissionCode, ProductosServiciosPermissionRequirement.Write, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            await Service(context).BajaPrecioAsync(context.IdEmpresa, idPrecio, TryResolveUsuarioId(), HttpContext.RequestAborted);
            return Ok(new ListaPreciosOperacionResponse { Code = "OK", Mensaje = "Precio archivado.", Id = idPrecio });
        }

        private IListaPreciosService Service(RequestContext context)
        {
            return new ListaPreciosService(new SqlListaPreciosRepository(_connectionFactory, context.TenantDatabase));
        }

        private Task<bool> TryResolveContextAsync(
            Guid? clientEmpresaId,
            string permissionCode,
            ProductosServiciosPermissionRequirement requirement,
            out RequestContext context,
            out IActionResult? error)
        {
            context = null!;
            error = null;

            Guid? effectiveEmpresaId = TryResolveEmpresaId(out string? empresaKey);
            if (!effectiveEmpresaId.HasValue || effectiveEmpresaId.Value == Guid.Empty || string.IsNullOrWhiteSpace(empresaKey))
            {
                error = Unauthorized(new ListaPreciosOperacionResponse { Code = "TENANT_CONTEXT_INVALID", Mensaje = "No fue posible resolver la empresa activa." });
                return Task.FromResult(false);
            }

            if (clientEmpresaId.HasValue && clientEmpresaId.Value != Guid.Empty && clientEmpresaId.Value != effectiveEmpresaId.Value)
            {
                error = StatusCode(403, new ListaPreciosOperacionResponse { Code = "TENANT_INPUT_MISMATCH", Mensaje = "No fue posible autorizar la solicitud." });
                return Task.FromResult(false);
            }

            foreach (var item in Request.Query)
            {
                if (!TenantInputMatches(item.Key, item.Value, effectiveEmpresaId.Value, empresaKey))
                {
                    error = StatusCode(403, new ListaPreciosOperacionResponse { Code = "TENANT_INPUT_MISMATCH", Mensaje = "No fue posible autorizar la solicitud." });
                    return Task.FromResult(false);
                }
            }

            TenantDatabaseDescriptor tenantDatabase;
            try
            {
                tenantDatabase = _tenantDatabaseResolver.ResolveAsync(
                    new TenantDatabaseContext { EmpresaKey = empresaKey, IdEmpresa = effectiveEmpresaId.Value },
                    HttpContext.RequestAborted).GetAwaiter().GetResult();
            }
            catch (TenantDatabaseResolutionException ex)
            {
                _logger.LogWarning("Resolución tenant ListaPrecios falló. Categoria={TenantResolutionCode} EmpresaKey={EmpresaKey} IdEmpresa={IdEmpresa}",
                    ex.Code, SanitizeEmpresaKey(empresaKey), effectiveEmpresaId.Value);
                error = StatusCode(503, new ListaPreciosOperacionResponse { Code = ex.Code.ToString(), Mensaje = "No fue posible resolver la base de datos de la empresa." });
                return Task.FromResult(false);
            }

            SignedProxyContext? signedContext = HttpContext.Items.TryGetValue(ProxyContextItemKey, out var identity)
                ? identity as SignedProxyContext
                : null;
            if (requirement == ProductosServiciosPermissionRequirement.Write &&
                string.Equals(permissionCode, AdminPermissionCode, StringComparison.OrdinalIgnoreCase))
            {
                ProductosServiciosAuthorizationDecision accessAuthorization = _authorizationService.AuthorizeAsync(
                    new ProductosServiciosAuthorizationRequest
                    {
                        IdEmpresa = effectiveEmpresaId.Value,
                        UserId = signedContext?.UserId ?? string.Empty,
                        TenantDatabase = tenantDatabase,
                        Requirement = ProductosServiciosPermissionRequirement.Read,
                        PermissionCode = AccessPermissionCode
                    },
                    HttpContext.RequestAborted).GetAwaiter().GetResult();
                if (!accessAuthorization.IsAllowed(ProductosServiciosPermissionRequirement.Read))
                {
                    error = StatusCode(403, new { code = "LISTA_PRECIOS_FORBIDDEN", message = "No tienes permiso para realizar esta operación.", referenceId = accessAuthorization.ReferenceId });
                    return Task.FromResult(false);
                }
            }

            ProductosServiciosAuthorizationDecision authorization = _authorizationService.AuthorizeAsync(
                new ProductosServiciosAuthorizationRequest
                {
                    IdEmpresa = effectiveEmpresaId.Value,
                    UserId = signedContext?.UserId ?? string.Empty,
                    TenantDatabase = tenantDatabase,
                    Requirement = requirement,
                    PermissionCode = permissionCode
                },
                HttpContext.RequestAborted).GetAwaiter().GetResult();
            if (!authorization.IsAllowed(requirement))
            {
                error = StatusCode(403, new { code = "LISTA_PRECIOS_FORBIDDEN", message = "No tienes permiso para realizar esta operación.", referenceId = authorization.ReferenceId });
                return Task.FromResult(false);
            }

            CompatibilityDecision compatibility = _compatibilityGate.EvaluateAsync(tenantDatabase, DatabaseScopes.ListaPrecios, HttpContext.RequestAborted).GetAwaiter().GetResult();
            if (!compatibility.IsAllowed)
            {
                _logger.LogWarning("Gate ListaPrecios bloqueó operación. ReferenceId={ReferenceId} Identity={DatabaseIdentity} ReasonCode={ReasonCode}",
                    compatibility.ReferenceId,
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(compatibility.SanitizedIdentity ?? string.Empty))),
                    compatibility.ReasonCode);
                error = StatusCode(503, new { code = compatibility.ReasonCode, message = "Lista de Precios no está disponible temporalmente.", referenceId = compatibility.ReferenceId });
                return Task.FromResult(false);
            }

            context = new RequestContext
            {
                IdEmpresa = effectiveEmpresaId.Value,
                EmpresaStorageKey = empresaKey,
                TenantDatabase = tenantDatabase
            };
            return Task.FromResult(true);
        }

        private Guid? TryResolveEmpresaId(out string? empresaKey)
        {
            empresaKey = null;
            if (!ProductosServiciosIdentityValidation.TryValidate(
                User,
                Request.Headers,
                _configuration["fireBdata:fireClave"] ?? string.Empty,
                DateTimeOffset.UtcNow,
                out TenantDatabaseContext verified,
                out string userId))
            {
                return null;
            }

            HttpContext.Items[ProxyContextItemKey] = new SignedProxyContext
            {
                IdEmpresa = verified.IdEmpresa,
                EmpresaStorageKey = verified.EmpresaKey,
                UserId = userId,
                UsuarioId = Guid.TryParse(userId, out Guid actor) && actor != Guid.Empty ? actor : null
            };
            empresaKey = verified.EmpresaKey;
            return verified.IdEmpresa;
        }

        private Guid? TryResolveUsuarioId()
        {
            return HttpContext.Items.TryGetValue(ProxyContextItemKey, out var identity)
                ? (identity as SignedProxyContext)?.UsuarioId
                : null;
        }

        private static IActionResult ToBusinessError(string code)
        {
            ListaPreciosOperacionResponse payload = new() { Code = code, Mensaje = "No fue posible procesar la operación de Lista de Precios." };
            return code switch
            {
                "PRECIO_NEGATIVO" => new BadRequestObjectResult(payload),
                ListaPreciosResolutionCodes.ListaInvalida => new BadRequestObjectResult(payload),
                ListaPreciosResolutionCodes.DescuentoInvalido => new BadRequestObjectResult(payload),
                "PRECIO_MINIMO_INVALIDO" => new BadRequestObjectResult(payload),
                "PRECIO_MAXIMO_INVALIDO" => new BadRequestObjectResult(payload),
                "RANGO_PRECIO_INVALIDO" => new BadRequestObjectResult(payload),
                "SELECCION_VACIA" => new BadRequestObjectResult(payload),
                "CAMPO_INVALIDO" => new BadRequestObjectResult(payload),
                "TIPO_AJUSTE_INVALIDO" => new BadRequestObjectResult(payload),
                "OPERACION_INVALIDA" => new BadRequestObjectResult(payload),
                "VALOR_AJUSTE_INVALIDO" => new BadRequestObjectResult(payload),
                ListaPreciosResolutionCodes.RedondeoInvalido => new BadRequestObjectResult(payload),
                ListaPreciosResolutionCodes.VigenciaInvalida => new BadRequestObjectResult(payload),
                ListaPreciosResolutionCodes.FueraDeV1 => new BadRequestObjectResult(payload),
                ListaPreciosResolutionCodes.IdentidadInvalida => new BadRequestObjectResult(payload),
                ListaPreciosResolutionCodes.IdentidadInactiva => new ObjectResult(payload) { StatusCode = 409 },
                ListaPreciosResolutionCodes.Duplicado => new ObjectResult(payload) { StatusCode = 409 },
                "LISTA_PRECIOS_DUPLICIDAD_ACTIVA" => new ObjectResult(payload) { StatusCode = 409 },
                _ => new BadRequestObjectResult(payload)
            };
        }

        private static bool TenantInputMatches(string key, Microsoft.Extensions.Primitives.StringValues values, Guid id, string empresa)
        {
            if (new[] { "cadena", "connectionString", "server", "database", "password" }.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
            if (new[] { "idEmpresa", "empresaId", "tenantId" }.Contains(key, StringComparer.OrdinalIgnoreCase))
                return values.All(v => Guid.TryParse(v, out Guid parsed) && parsed == id);
            if (new[] { "empresa", "empresaKey" }.Contains(key, StringComparer.OrdinalIgnoreCase))
                return values.All(v => string.Equals(v?.Trim(), empresa, StringComparison.OrdinalIgnoreCase));
            return true;
        }

        private static string SanitizeEmpresaKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Length <= 4 ? "****" : $"{value[..2]}***{value[^2..]}";
        }

        private sealed class RequestContext
        {
            public Guid IdEmpresa { get; init; }
            public string EmpresaStorageKey { get; init; } = string.Empty;
            public TenantDatabaseDescriptor TenantDatabase { get; init; } = null!;
        }

        private sealed class SignedProxyContext
        {
            public Guid IdEmpresa { get; init; }
            public string EmpresaStorageKey { get; init; } = string.Empty;
            public string UserId { get; init; } = string.Empty;
            public Guid? UsuarioId { get; init; }
        }
    }
}

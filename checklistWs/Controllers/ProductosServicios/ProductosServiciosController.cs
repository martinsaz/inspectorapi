using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using checklistWs.Models.ProductosServicios;
using checklistWs.Services.ProductosServicios;
using checklistWs.Services.Tenant;
using checklistWs.Utiles;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Storage;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace checklistWs.Controllers.ProductosServicios
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosServiciosController : ControllerBase
    {
        private const byte TipoProducto = 1;
        private const byte TipoServicio = 2;
        private const byte AplicaATodos = 0;
        private const byte AplicaAProductos = 1;
        private const byte AplicaAServicios = 2;
        private const byte MovimientoExistenciaInicial = 1;
        private const byte MovimientoEntrada = 2;
        private const byte MovimientoSalida = 3;
        private const byte MovimientoAjustePositivo = 4;
        private const byte MovimientoAjusteNegativo = 5;
        private const string InventarioV1MovimientoBloqueadoMensaje = "Inventario V1 requiere sucursal, origen e idempotencia. Registra inventario desde el flujo operativo correspondiente.";
        private const int CodigoLength = 50;
        private const int NombreLength = 150;
        private const int ObservacionesLength = 1000;
        private const int DescripcionCatalogoLength = 20000;
        private const int TagLength = 100;
        private const int UnidadCodigoLength = 30;
        private const int UnidadNombreLength = 100;
        private const int AbreviaturaLength = 20;
        private const int ReferenciaLength = 150;
        private const int NumeroColeccionLength = 50;
        private const int ClaveSatProductoLength = 20;
        private const int ClaveSatUnidadLength = 10;
        private const int ObjetoImpuestoLength = 4;
        private const int PrecioUnitarioUnidadLength = 20;
        private const int PrecioUnitarioUnidadTotalLength = 20;
        private const int PrecioUnitarioUnidadBaseLength = 20;
        private const int TipoPaqueteLength = 30;
        private const decimal FactorVolumetricoDefault = 5000m;
        private const int AtributoNombreLength = 100;
        private const int AtributoValorLength = 120;
        private const int OpcionVarianteNombreLength = 100;
        private const int NombreArchivoLength = 255;
        private const int MimeTypeLength = 120;
        private const int UrlLength = 1000;
        private const long ImagenMaxBytes = 10L * 1024L * 1024L;
        private const long VideoMaxBytes = 200L * 1024L * 1024L;
        private const long DocumentoMaxBytes = 25L * 1024L * 1024L;
        private const long UploadTemporalRequestLimitBytes = 12L * 1024L * 1024L;
        private const long UploadTemporalMultimediaRequestLimitBytes = 210L * 1024L * 1024L;
        private static readonly TimeSpan TemporalTokenLifetime = TimeSpan.FromHours(6);
        private static readonly TimeSpan ProxyHeaderTolerance = TimeSpan.FromMinutes(5);
        private static readonly string[] MimeTypesImagenPermitidos = new[] { "image/jpeg", "image/png", "image/webp" };
        private static readonly string[] ExtensionesImagenPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] TiposMultimediaPermitidos = new[] { "foto", "video", "documento" };
        private static readonly HashSet<string> CategoriaActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Categorias",
            "ObtenerCategoriasProductosServicios",
            "ObtenerCategoriaProductoServicio",
            "GuardarCategoriaProductoServicio",
            "BajaCategoriaProductoServicio",
            "ActivarCategoriaProductoServicio",
            "ObtenerCatalogoCategoriasProductosServicios",
            "ExportarCategoriasProductosServicios"
        };
        private static readonly HashSet<string> MarcaActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Marcas",
            "ObtenerMarcasProductosServicios",
            "ObtenerMarcaProductoServicio",
            "GuardarMarcaProductoServicio",
            "GuardarTagProductoServicio",
            "BajaMarcaProductoServicio",
            "ActivarMarcaProductoServicio",
            "ObtenerCatalogoMarcasProductosServicios",
            "ExportarMarcasProductosServicios"
        };
        private static readonly HashSet<string> UnidadMedidaActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "UnidadesMedida",
            "ObtenerUnidadesMedidaProductosServicios",
            "ObtenerUnidadMedidaProductoServicio",
            "GuardarUnidadMedidaProductoServicio",
            "BajaUnidadMedidaProductoServicio",
            "ActivarUnidadMedidaProductoServicio",
            "ObtenerCatalogoUnidadesMedidaProductosServicios",
            "ExportarUnidadesMedidaProductosServicios"
        };
        private static readonly string[] EmpresaClaimKeys = new[] { "idEmpresa", "empresaId", "tenantId", "companyId", "tenant", "idempresa" };
        private static readonly string[] EmpresaNombreClaimKeys = new[] { "empresa", "empresaNombre", "tenantName", "companyName", "nombreEmpresa" };
        private static readonly string[] UsuarioClaimKeys = new[] { ClaimTypes.NameIdentifier, "sub", "idUsuario", "userid", "uid" };
        private const string ProxyEmpresaIdHeader = "X-ProductosServicios-Proxy-EmpresaId";
        private const string ProxyEmpresaKeyHeader = "X-ProductosServicios-Proxy-Empresa";
        private const string ProxyUsuarioIdHeader = "X-ProductosServicios-Proxy-UsuarioId";
        private const string ProxyTimestampHeader = "X-ProductosServicios-Proxy-Timestamp";
        private const string ProxySignatureHeader = "X-ProductosServicios-Proxy-Signature";
        private const string AbcPermissionCode = ProductosServiciosAuthorizationDefaults.AbcPermissionCode;
        private const string CategoriasPermissionCode = ProductosServiciosAuthorizationDefaults.CategoriasPermissionCode;
        private const string MarcasPermissionCode = ProductosServiciosAuthorizationDefaults.MarcasPermissionCode;
        private const string UnidadesMedidaPermissionCode = ProductosServiciosAuthorizationDefaults.UnidadesMedidaPermissionCode;
        private const string ProxyContextItemKey = "__ProductosServiciosProxyContext";

        private readonly IConfiguration _configuration;
        private readonly ITenantDatabaseResolver _tenantDatabaseResolver;
        private readonly ITenantSqlConnectionFactory _tenantSqlConnectionFactory;
        private readonly IProductosServiciosCompatibilityGate _compatibilityGate;
        private readonly IProductosServiciosAuthorizationService _authorizationService;
        private readonly IProductosServiciosCompanyBootstrapper _companyBootstrapper;
        private readonly ILogger<ProductosServiciosController> _logger;

        private sealed class ProductoServicioLogisticsMetrics
        {
            public decimal FactorVolumetrico { get; init; }
            public decimal? PesoFisicoTotalKg { get; init; }
            public decimal? PesoVolumetricoKg { get; init; }
            public decimal? PesoFacturableKg { get; init; }
        }

        public ProductosServiciosController(
            IConfiguration configuration,
            ILogger<ProductosServiciosController> logger,
            ITenantDatabaseResolver tenantDatabaseResolver,
            ITenantSqlConnectionFactory tenantSqlConnectionFactory,
            IProductosServiciosCompatibilityGate compatibilityGate,
            IProductosServiciosAuthorizationService authorizationService,
            IProductosServiciosCompanyBootstrapper companyBootstrapper)
        {
            _configuration = configuration;
            _logger = logger;
            _tenantDatabaseResolver = tenantDatabaseResolver;
            _tenantSqlConnectionFactory = tenantSqlConnectionFactory;
            _compatibilityGate = compatibilityGate;
            _authorizationService = authorizationService;
            _companyBootstrapper = companyBootstrapper;
        }

        [HttpGet("ObtenerProductosServicios")]
        public async Task<IActionResult> ObtenerProductosServicios(
            Guid idEmpresa,
            string busqueda = "",
            byte? tipo = null,
            Guid? idCategoria = null,
            Guid? idMarca = null,
            Guid? idUnidadMedida = null,
            bool? causaInventario = null,
            string estatus = "")
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();

                StringBuilder query = new StringBuilder(@"
SELECT
    ps.id,
    ps.idEmpresa,
    ps.identityKey,
    ps.Tipo,
    CASE ps.Tipo WHEN 1 THEN 'Producto' ELSE 'Servicio' END AS TipoNombre,
    ps.Codigo,
    COALESCE(NULLIF(tg.TagsDisplay, ''), ISNULL(ps.Tag, '')) AS Tag,
    ps.Nombre,
    ISNULL(ps.Descripcion, '') AS Descripcion,
    ps.idCategoria,
    cat.Nombre AS Categoria,
    cat.AplicaA AS CategoriaAplicaA,
    ps.idMarca,
    ISNULL(m.Nombre, '') AS Marca,
    ps.idUnidadMedida,
    um.Nombre AS UnidadMedida,
    um.Abreviatura AS UnidadAbreviatura,
    um.PermiteDecimales AS UnidadPermiteDecimales,
    ps.idColeccion,
    ISNULL(col.Numero, '') AS ColeccionNumero,
    ISNULL(col.Nombre, '') AS ColeccionNombre,
    ps.idPaquete,
    ISNULL(pa.Nombre, '') AS PaqueteNombre,
    ISNULL(pa.TipoPaquete, '') AS TipoPaquete,
    pa.LargoCm AS PaqueteLargoCm,
    pa.AnchoCm AS PaqueteAnchoCm,
    pa.AltoCm AS PaqueteAltoCm,
    pa.PesoEmpaqueVacioKg,
    ps.Costo,
    ps.PrecioPublico,
    ps.PrecioComparacion,
    ps.PrecioUnitarioMonto,
    ps.PrecioUnitarioCantidadTotal,
    ISNULL(ps.PrecioUnitarioUnidadTotal, '') AS PrecioUnitarioUnidadTotal,
    ps.PrecioUnitarioBaseCantidad,
    ISNULL(ps.PrecioUnitarioUnidad, '') AS PrecioUnitarioUnidad,
    ISNULL(ps.PrecioUnitarioUnidadBase, '') AS PrecioUnitarioUnidadBase,
    ISNULL(ps.ObjetoImpuesto, '') AS ObjetoImpuesto,
    ISNULL(ps.PorcentajeIVA, 0) AS PorcentajeIVA,
    ISNULL(ps.ClaveProductoSat, '') AS ClaveProductoSat,
    ISNULL(ps.ClaveUnidadSat, '') AS ClaveUnidadSat,
    ps.EsProductoFisico,
    ps.PesoKg,
    ps.LargoCm,
    ps.AnchoCm,
    ps.AltoCm,
    ps.UsaNumeroSerie,
    ps.CausaInventario,
    ps.PermiteVentaSinExistencia,
    CAST(NULL AS uniqueidentifier) AS IdExistencia,
    ISNULL(inv.ExistenciaActual, 0) AS ExistenciaActual,
    CAST(0 AS decimal(18,4)) AS ExistenciaMinima,
    CAST(NULL AS decimal(18,4)) AS CostoPromedio,
    ISNULL(ps.ImagenUrl, '') AS ImagenUrl,
    ISNULL(ps.ImagenNombre, '') AS ImagenNombre,
    ISNULL(mm.CantidadFotos, 0) AS CantidadFotos,
    ISNULL(mm.CantidadVideos, 0) AS CantidadVideos,
    ISNULL(mm.CantidadDocumentos, 0) AS CantidadDocumentos,
    ISNULL(vr.CantidadVariantes, 0) AS CantidadVariantes,
    ps.Activo,
    ps.FechaCreacion,
    ps.FechaActualizacion,
    ps.FechaArchivado
FROM dbo.ProductosServicios ps
INNER JOIN dbo.ProductosServiciosCategorias cat
    ON cat.idEmpresa = ps.idEmpresa AND cat.id = ps.idCategoria
INNER JOIN dbo.ProductosServiciosUnidadesMedida um
    ON um.idEmpresa = ps.idEmpresa AND um.id = ps.idUnidadMedida
LEFT JOIN dbo.ProductosServiciosMarcas m
    ON m.idEmpresa = ps.idEmpresa AND m.id = ps.idMarca
LEFT JOIN dbo.ProductosServiciosColecciones col
    ON col.idEmpresa = ps.idEmpresa AND col.id = ps.idColeccion
LEFT JOIN dbo.ProductosServiciosPaquetes pa
    ON pa.idEmpresa = ps.idEmpresa AND pa.id = ps.idPaquete
OUTER APPLY (
    SELECT SUM(s.CantidadBaseActual) AS ExistenciaActual
    FROM dbo.InventarioSaldos s
    WHERE s.idEmpresa = ps.idEmpresa AND s.idProductoServicio = ps.id
) inv
LEFT JOIN (
    SELECT
        pm.idEmpresa,
        pm.idProductoServicio,
        SUM(CASE WHEN pm.Activo = 1 AND pm.Foto = 1 THEN 1 ELSE 0 END) AS CantidadFotos,
        SUM(CASE WHEN pm.Activo = 1 AND pm.Video = 1 THEN 1 ELSE 0 END) AS CantidadVideos,
        SUM(CASE WHEN pm.Activo = 1 AND pm.Documento = 1 THEN 1 ELSE 0 END) AS CantidadDocumentos
    FROM dbo.ProductosServiciosMultimedia pm
    GROUP BY pm.idEmpresa, pm.idProductoServicio
) mm
    ON mm.idEmpresa = ps.idEmpresa AND mm.idProductoServicio = ps.id
LEFT JOIN (
    SELECT
        pv.idEmpresa,
        pv.idProductoServicio,
        COUNT(1) AS CantidadVariantes
    FROM dbo.ProductosServiciosVariantes pv
    WHERE pv.Activo = 1
    GROUP BY pv.idEmpresa, pv.idProductoServicio
) vr
    ON vr.idEmpresa = ps.idEmpresa AND vr.idProductoServicio = ps.id
LEFT JOIN (
    SELECT
        pt.idEmpresa,
        pt.idProductoServicio,
        STUFF((
            SELECT ', ' + t2.Nombre
            FROM dbo.ProductosServiciosProductoTags pt2
            INNER JOIN dbo.ProductosServiciosTags t2
                ON t2.idEmpresa = pt2.idEmpresa AND t2.id = pt2.idTag
            WHERE pt2.idEmpresa = pt.idEmpresa
              AND pt2.idProductoServicio = pt.idProductoServicio
              AND t2.Activo = 1
            ORDER BY t2.Nombre
            FOR XML PATH(''), TYPE
        ).value('.', 'nvarchar(max)'), 1, 2, '') AS TagsDisplay
    FROM dbo.ProductosServiciosProductoTags pt
    GROUP BY pt.idEmpresa, pt.idProductoServicio
) tg
    ON tg.idEmpresa = ps.idEmpresa AND tg.idProductoServicio = ps.id
WHERE ps.idEmpresa = @IdEmpresa");

                using SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    query.Append(@"
  AND (
      ps.Codigo LIKE @Busqueda
      OR COALESCE(NULLIF(tg.TagsDisplay, ''), ISNULL(ps.Tag, '')) LIKE @Busqueda
      OR ps.Nombre LIKE @Busqueda
      OR ISNULL(ps.Descripcion, '') LIKE @Busqueda
  )");
                    command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
                }

                AppendTinyIntFilter(query, command, "ps.Tipo", "@Tipo", tipo);
                AppendGuidFilter(query, command, "ps.idCategoria", "@IdCategoria", idCategoria);
                AppendGuidFilter(query, command, "ps.idMarca", "@IdMarca", idMarca);
                AppendGuidFilter(query, command, "ps.idUnidadMedida", "@IdUnidadMedida", idUnidadMedida);
                AppendBitFilter(query, command, "ps.CausaInventario", "@CausaInventario", causaInventario);
                AppendEstatusFilter(query, "ps.Activo", estatus);

                query.Append(" ORDER BY ps.Activo DESC, ps.Nombre, ps.Codigo");
                command.CommandText = query.ToString();

                List<ProductoServicioListadoDto> items = new List<ProductoServicioListadoDto>();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(MapProductoServicioListado(reader));
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerProductosServicios", "No fue posible cargar los productos y servicios.");
            }
        }

        [HttpGet("ObtenerProductoServicio")]
        public async Task<IActionResult> ObtenerProductoServicio(Guid idEmpresa, Guid idProductoServicio)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
SELECT
    ps.id,
    ps.idEmpresa,
    ps.identityKey,
    ps.Tipo,
    CASE ps.Tipo WHEN 1 THEN 'Producto' ELSE 'Servicio' END AS TipoNombre,
    ps.Codigo,
    ISNULL(ps.Tag, '') AS Tag,
    ps.Nombre,
    ISNULL(ps.Descripcion, '') AS Descripcion,
    ps.idCategoria,
    cat.Nombre AS Categoria,
    cat.AplicaA AS CategoriaAplicaA,
    ps.idMarca,
    ISNULL(m.Nombre, '') AS Marca,
    ps.idUnidadMedida,
    um.Nombre AS UnidadMedida,
    um.Abreviatura AS UnidadAbreviatura,
    um.PermiteDecimales AS UnidadPermiteDecimales,
    ps.idColeccion,
    ISNULL(col.Numero, '') AS ColeccionNumero,
    ISNULL(col.Nombre, '') AS ColeccionNombre,
    ps.idPaquete,
    ISNULL(pa.Nombre, '') AS PaqueteNombre,
    ISNULL(pa.TipoPaquete, '') AS TipoPaquete,
    pa.LargoCm AS PaqueteLargoCm,
    pa.AnchoCm AS PaqueteAnchoCm,
    pa.AltoCm AS PaqueteAltoCm,
    pa.PesoEmpaqueVacioKg,
    ps.Costo,
    ps.PrecioPublico,
    ps.PrecioComparacion,
    ps.PrecioUnitarioMonto,
    ps.PrecioUnitarioCantidadTotal,
    ISNULL(ps.PrecioUnitarioUnidadTotal, '') AS PrecioUnitarioUnidadTotal,
    ps.PrecioUnitarioBaseCantidad,
    ISNULL(ps.PrecioUnitarioUnidad, '') AS PrecioUnitarioUnidad,
    ISNULL(ps.PrecioUnitarioUnidadBase, '') AS PrecioUnitarioUnidadBase,
    ISNULL(ps.ObjetoImpuesto, '') AS ObjetoImpuesto,
    ISNULL(ps.PorcentajeIVA, 0) AS PorcentajeIVA,
    ISNULL(ps.ClaveProductoSat, '') AS ClaveProductoSat,
    ISNULL(ps.ClaveUnidadSat, '') AS ClaveUnidadSat,
    ps.EsProductoFisico,
    ps.PesoKg,
    ps.LargoCm,
    ps.AnchoCm,
    ps.AltoCm,
    ps.UsaNumeroSerie,
    ps.CausaInventario,
    ps.PermiteVentaSinExistencia,
    CAST(NULL AS uniqueidentifier) AS IdExistencia,
    ISNULL(inv.ExistenciaActual, 0) AS ExistenciaActual,
    CAST(0 AS decimal(18,4)) AS ExistenciaMinima,
    CAST(NULL AS decimal(18,4)) AS CostoPromedio,
    ISNULL(ps.ImagenUrl, '') AS ImagenUrl,
    ISNULL(ps.ImagenNombre, '') AS ImagenNombre,
    ISNULL(mm.CantidadFotos, 0) AS CantidadFotos,
    ISNULL(mm.CantidadVideos, 0) AS CantidadVideos,
    ISNULL(mm.CantidadDocumentos, 0) AS CantidadDocumentos,
    ISNULL(vr.CantidadVariantes, 0) AS CantidadVariantes,
    ps.Activo,
    ps.FechaCreacion,
    ps.FechaActualizacion,
    ps.FechaArchivado
FROM dbo.ProductosServicios ps
INNER JOIN dbo.ProductosServiciosCategorias cat
    ON cat.idEmpresa = ps.idEmpresa AND cat.id = ps.idCategoria
INNER JOIN dbo.ProductosServiciosUnidadesMedida um
    ON um.idEmpresa = ps.idEmpresa AND um.id = ps.idUnidadMedida
LEFT JOIN dbo.ProductosServiciosMarcas m
    ON m.idEmpresa = ps.idEmpresa AND m.id = ps.idMarca
LEFT JOIN dbo.ProductosServiciosColecciones col
    ON col.idEmpresa = ps.idEmpresa AND col.id = ps.idColeccion
LEFT JOIN dbo.ProductosServiciosPaquetes pa
    ON pa.idEmpresa = ps.idEmpresa AND pa.id = ps.idPaquete
OUTER APPLY (
    SELECT SUM(s.CantidadBaseActual) AS ExistenciaActual
    FROM dbo.InventarioSaldos s
    WHERE s.idEmpresa = ps.idEmpresa AND s.idProductoServicio = ps.id
) inv
OUTER APPLY (
    SELECT
        SUM(CASE WHEN pm.Activo = 1 AND pm.Foto = 1 THEN 1 ELSE 0 END) AS CantidadFotos,
        SUM(CASE WHEN pm.Activo = 1 AND pm.Video = 1 THEN 1 ELSE 0 END) AS CantidadVideos,
        SUM(CASE WHEN pm.Activo = 1 AND pm.Documento = 1 THEN 1 ELSE 0 END) AS CantidadDocumentos
    FROM dbo.ProductosServiciosMultimedia pm
    WHERE pm.idEmpresa = ps.idEmpresa AND pm.idProductoServicio = ps.id
) mm
OUTER APPLY (
    SELECT COUNT(1) AS CantidadVariantes
    FROM dbo.ProductosServiciosVariantes pv
    WHERE pv.idEmpresa = ps.idEmpresa AND pv.idProductoServicio = ps.id AND pv.Activo = 1
) vr
WHERE ps.idEmpresa = @IdEmpresa AND ps.id = @IdProductoServicio", connection);

                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

                ProductoServicioDetalleDto? detalle = null;
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        ProductoServicioListadoDto baseItem = MapProductoServicioListado(reader);
                        detalle = new ProductoServicioDetalleDto
                        {
                            Id = baseItem.Id,
                            IdEmpresa = baseItem.IdEmpresa,
                            IdentityKey = baseItem.IdentityKey,
                            Tipo = baseItem.Tipo,
                            TipoNombre = baseItem.TipoNombre,
                            Codigo = baseItem.Codigo,
                            Tag = baseItem.Tag,
                            Nombre = baseItem.Nombre,
                            Descripcion = baseItem.Descripcion,
                            IdCategoria = baseItem.IdCategoria,
                            Categoria = baseItem.Categoria,
                            CategoriaAplicaA = baseItem.CategoriaAplicaA,
                            IdMarca = baseItem.IdMarca,
                            Marca = baseItem.Marca,
                            IdUnidadMedida = baseItem.IdUnidadMedida,
                            UnidadMedida = baseItem.UnidadMedida,
                            UnidadAbreviatura = baseItem.UnidadAbreviatura,
                            UnidadPermiteDecimales = baseItem.UnidadPermiteDecimales,
                            IdColeccion = baseItem.IdColeccion,
                            ColeccionNumero = baseItem.ColeccionNumero,
                            ColeccionNombre = baseItem.ColeccionNombre,
                            IdPaquete = baseItem.IdPaquete,
                            PaqueteNombre = baseItem.PaqueteNombre,
                            TipoPaquete = ReadString(reader, "TipoPaquete"),
                            PaqueteLargoCm = ReadNullableDecimal(reader, "PaqueteLargoCm"),
                            PaqueteAnchoCm = ReadNullableDecimal(reader, "PaqueteAnchoCm"),
                            PaqueteAltoCm = ReadNullableDecimal(reader, "PaqueteAltoCm"),
                            PaquetePesoEmpaqueVacioKg = ReadNullableDecimal(reader, "PesoEmpaqueVacioKg"),
                            Costo = baseItem.Costo,
                            PrecioPublico = baseItem.PrecioPublico,
                            PrecioComparacion = baseItem.PrecioComparacion,
                            PrecioUnitarioMonto = baseItem.PrecioUnitarioMonto,
                            PrecioUnitarioCantidadTotal = baseItem.PrecioUnitarioCantidadTotal,
                            PrecioUnitarioUnidadTotal = baseItem.PrecioUnitarioUnidadTotal,
                            PrecioUnitarioBaseCantidad = baseItem.PrecioUnitarioBaseCantidad,
                            PrecioUnitarioUnidad = baseItem.PrecioUnitarioUnidad,
                            PrecioUnitarioUnidadBase = baseItem.PrecioUnitarioUnidadBase,
                            ObjetoImpuesto = baseItem.ObjetoImpuesto,
                            PorcentajeIVA = baseItem.PorcentajeIVA,
                            ClaveProductoSat = baseItem.ClaveProductoSat,
                            ClaveUnidadSat = baseItem.ClaveUnidadSat,
                            EsProductoFisico = baseItem.EsProductoFisico,
                            PesoKg = baseItem.PesoKg,
                            LargoCm = baseItem.LargoCm,
                            AnchoCm = baseItem.AnchoCm,
                            AltoCm = baseItem.AltoCm,
                            UsaNumeroSerie = baseItem.UsaNumeroSerie,
                            CausaInventario = baseItem.CausaInventario,
                            PermiteVentaSinExistencia = baseItem.PermiteVentaSinExistencia,
                            ExistenciaActual = baseItem.ExistenciaActual,
                            ExistenciaMinima = baseItem.ExistenciaMinima,
                            CostoPromedio = baseItem.CostoPromedio,
                            ImagenUrl = baseItem.ImagenUrl,
                            ImagenNombre = baseItem.ImagenNombre,
                            CantidadFotos = baseItem.CantidadFotos,
                            CantidadVideos = baseItem.CantidadVideos,
                            CantidadDocumentos = baseItem.CantidadDocumentos,
                            CantidadVariantes = baseItem.CantidadVariantes,
                            Activo = baseItem.Activo,
                            FechaCreacion = baseItem.FechaCreacion,
                            FechaActualizacion = baseItem.FechaActualizacion,
                            FechaArchivado = baseItem.FechaArchivado,
                            IdExistencia = ReadNullableGuid(reader, "IdExistencia")
                        };
                    }
                }

                if (detalle == null)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "El producto o servicio no está disponible." });
                }

                detalle.MovimientosRecientes = await ObtenerMovimientosInventarioInternoAsync(connection, context.IdEmpresa, idProductoServicio, 10);
                detalle.Tags = await ObtenerTagsProductoAsync(connection, context.IdEmpresa, idProductoServicio, detalle.Tag);
                detalle.Atributos = await ObtenerAtributosProductoAsync(connection, context.IdEmpresa, idProductoServicio);
                detalle.OpcionesVariante = await ObtenerOpcionesVarianteProductoAsync(connection, context.IdEmpresa, idProductoServicio);
                detalle.Variantes = await ObtenerVariantesProductoAsync(connection, context.IdEmpresa, idProductoServicio);
                detalle.Multimedia = await ObtenerMultimediaProductoAsync(connection, context.IdEmpresa, idProductoServicio);
                detalle.PresentacionesVenta = detalle.Tipo == TipoProducto
                    ? await ObtenerPresentacionesVentaAsync(connection, null, context.IdEmpresa, idProductoServicio, true)
                    : new List<ProductoServicioPresentacionVentaDto>();
                ApplyLogisticsMetrics(detalle);
                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerProductoServicio", "No fue posible cargar el detalle del producto o servicio.");
            }
        }

        [HttpGet("ObtenerFichaTecnicaProductoServicio")]
        public async Task<IActionResult> ObtenerFichaTecnicaProductoServicio(Guid idEmpresa, Guid idProductoServicio)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            if (idProductoServicio == Guid.Empty)
            {
                return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "El producto o servicio no está disponible." });
            }

            try
            {
                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();

                ProductoServicioFichaTecnicaDto? ficha = await ObtenerFichaTecnicaProductoAsync(connection, context.IdEmpresa, idProductoServicio);
                if (ficha == null || ficha.Id == Guid.Empty)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "El producto o servicio no está disponible." });
                }

                return Ok(ficha);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerFichaTecnicaProductoServicio", "No fue posible cargar la ficha técnica del producto o servicio.");
            }
        }

        [HttpGet("ExportarFichaTecnicaProductoServicioPdf")]
        public async Task<IActionResult> ExportarFichaTecnicaProductoServicioPdf(Guid idEmpresa, Guid idProductoServicio)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            if (idProductoServicio == Guid.Empty)
            {
                return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "El producto o servicio no está disponible." });
            }

            try
            {
                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();

                ProductoServicioFichaTecnicaDto? ficha = await ObtenerFichaTecnicaProductoAsync(connection, context.IdEmpresa, idProductoServicio);
                if (ficha == null || ficha.Id == Guid.Empty)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "El producto o servicio no está disponible." });
                }

                byte[] pdf = await BuildFichaTecnicaPdfDocumentAsync(ficha);
                string fileName = BuildFichaTecnicaFileName(ficha.Codigo, ficha.Nombre);
                return File(pdf, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ExportarFichaTecnicaProductoServicioPdf", "No fue posible generar el PDF de la ficha técnica.");
            }
        }

        private async Task<ProductoServicioFichaTecnicaDto?> ObtenerFichaTecnicaProductoAsync(SqlConnection connection, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    ps.id,
    ps.idEmpresa,
    ps.Tipo,
    CASE ps.Tipo WHEN 1 THEN 'Producto' ELSE 'Servicio' END AS TipoNombre,
    ps.Codigo,
    ps.Nombre,
    ISNULL(ps.Descripcion, '') AS Descripcion,
    ps.Activo,
    cat.Nombre AS Categoria,
    cat.AplicaA AS CategoriaAplicaA,
    ISNULL(m.Nombre, '') AS Marca,
    ps.idColeccion,
    ISNULL(col.Numero, '') AS ColeccionNumero,
    ISNULL(col.Nombre, '') AS ColeccionNombre,
    ps.idPaquete,
    ISNULL(pa.Nombre, '') AS PaqueteNombre,
    ISNULL(pa.TipoPaquete, '') AS TipoPaquete,
    pa.LargoCm AS PaqueteLargoCm,
    pa.AnchoCm AS PaqueteAnchoCm,
    pa.AltoCm AS PaqueteAltoCm,
    pa.PesoEmpaqueVacioKg,
    ISNULL(ps.ImagenUrl, '') AS ImagenUrl,
    ISNULL(ps.ImagenNombre, '') AS ImagenNombre,
    ps.idUnidadMedida,
    um.Nombre AS UnidadMedida,
    um.Abreviatura AS UnidadAbreviatura,
    um.PermiteDecimales AS UnidadPermiteDecimales,
    ps.Costo,
    ps.PrecioPublico,
    ps.PrecioComparacion,
    ps.PrecioUnitarioMonto,
    ps.PrecioUnitarioCantidadTotal,
    ISNULL(ps.PrecioUnitarioUnidadTotal, '') AS PrecioUnitarioUnidadTotal,
    ps.PrecioUnitarioBaseCantidad,
    ISNULL(ps.PrecioUnitarioUnidad, '') AS PrecioUnitarioUnidad,
    ISNULL(ps.PrecioUnitarioUnidadBase, '') AS PrecioUnitarioUnidadBase,
    ISNULL(ps.ClaveProductoSat, '') AS ClaveProductoSat,
    ISNULL(ps.ClaveUnidadSat, '') AS ClaveUnidadSat,
    ISNULL(ps.ObjetoImpuesto, '') AS ObjetoImpuesto,
    ISNULL(ps.PorcentajeIVA, 0) AS PorcentajeIVA,
    ps.EsProductoFisico,
    ps.PesoKg,
    ps.LargoCm,
    ps.AnchoCm,
    ps.AltoCm,
    ps.UsaNumeroSerie,
    ps.CausaInventario,
    ps.PermiteVentaSinExistencia,
    ISNULL(inv.ExistenciaActual, 0) AS ExistenciaActual,
    CAST(0 AS decimal(18,4)) AS ExistenciaMinima
FROM dbo.ProductosServicios ps
INNER JOIN dbo.ProductosServiciosCategorias cat
    ON cat.idEmpresa = ps.idEmpresa AND cat.id = ps.idCategoria
INNER JOIN dbo.ProductosServiciosUnidadesMedida um
    ON um.idEmpresa = ps.idEmpresa AND um.id = ps.idUnidadMedida
LEFT JOIN dbo.ProductosServiciosMarcas m
    ON m.idEmpresa = ps.idEmpresa AND m.id = ps.idMarca
LEFT JOIN dbo.ProductosServiciosColecciones col
    ON col.idEmpresa = ps.idEmpresa AND col.id = ps.idColeccion
LEFT JOIN dbo.ProductosServiciosPaquetes pa
    ON pa.idEmpresa = ps.idEmpresa AND pa.id = ps.idPaquete
OUTER APPLY (
    SELECT SUM(s.CantidadBaseActual) AS ExistenciaActual
    FROM dbo.InventarioSaldos s
    WHERE s.idEmpresa = ps.idEmpresa AND s.idProductoServicio = ps.id
) inv
WHERE ps.idEmpresa = @IdEmpresa
  AND ps.id = @IdProductoServicio
  AND ps.FechaArchivado IS NULL", connection);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            ProductoServicioFichaTecnicaDto? ficha = null;
            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    ficha = new ProductoServicioFichaTecnicaDto
                    {
                        Id = ReadGuid(reader, "id"),
                        IdEmpresa = ReadGuid(reader, "idEmpresa"),
                        Tipo = ReadByte(reader, "Tipo"),
                        TipoNombre = ReadString(reader, "TipoNombre"),
                        Codigo = ReadString(reader, "Codigo"),
                        Nombre = ReadString(reader, "Nombre"),
                        Descripcion = ReadString(reader, "Descripcion"),
                        Activo = ReadBool(reader, "Activo"),
                        EstatusNombre = ReadBool(reader, "Activo") ? "Activo" : "Inactivo",
                        Categoria = ReadString(reader, "Categoria"),
                        CategoriaAplicaA = ReadByte(reader, "CategoriaAplicaA"),
                        Marca = ReadString(reader, "Marca"),
                        IdColeccion = ReadNullableGuid(reader, "idColeccion"),
                        ColeccionNumero = ReadString(reader, "ColeccionNumero"),
                        ColeccionNombre = ReadString(reader, "ColeccionNombre"),
                        IdPaquete = ReadNullableGuid(reader, "idPaquete"),
                        PaqueteNombre = ReadString(reader, "PaqueteNombre"),
                        TipoPaquete = ReadString(reader, "TipoPaquete"),
                        PaqueteLargoCm = ReadNullableDecimal(reader, "PaqueteLargoCm"),
                        PaqueteAnchoCm = ReadNullableDecimal(reader, "PaqueteAnchoCm"),
                        PaqueteAltoCm = ReadNullableDecimal(reader, "PaqueteAltoCm"),
                        PaquetePesoEmpaqueVacioKg = ReadNullableDecimal(reader, "PesoEmpaqueVacioKg"),
                        ImagenUrl = ReadString(reader, "ImagenUrl"),
                        ImagenNombre = ReadString(reader, "ImagenNombre"),
                        IdUnidadMedida = ReadGuid(reader, "idUnidadMedida"),
                        UnidadMedida = ReadString(reader, "UnidadMedida"),
                        UnidadAbreviatura = ReadString(reader, "UnidadAbreviatura"),
                        UnidadPermiteDecimales = ReadBool(reader, "UnidadPermiteDecimales"),
                        Costo = ReadNullableDecimal(reader, "Costo"),
                        PrecioPublico = ReadDecimal(reader, "PrecioPublico"),
                        PrecioComparacion = ReadNullableDecimal(reader, "PrecioComparacion"),
                        PrecioUnitarioMonto = ReadNullableDecimal(reader, "PrecioUnitarioMonto"),
                        PrecioUnitarioCantidadTotal = ReadNullableDecimal(reader, "PrecioUnitarioCantidadTotal"),
                        PrecioUnitarioUnidadTotal = ReadString(reader, "PrecioUnitarioUnidadTotal"),
                        PrecioUnitarioBaseCantidad = ReadNullableDecimal(reader, "PrecioUnitarioBaseCantidad"),
                        PrecioUnitarioUnidad = ReadString(reader, "PrecioUnitarioUnidad"),
                        PrecioUnitarioUnidadBase = ReadString(reader, "PrecioUnitarioUnidadBase"),
                        ClaveProductoSat = ReadString(reader, "ClaveProductoSat"),
                        ClaveUnidadSat = ReadString(reader, "ClaveUnidadSat"),
                        ObjetoImpuesto = ReadString(reader, "ObjetoImpuesto"),
                        PorcentajeIVA = ReadDecimal(reader, "PorcentajeIVA"),
                        EsProductoFisico = ReadBool(reader, "EsProductoFisico"),
                        PesoKg = ReadNullableDecimal(reader, "PesoKg"),
                        LargoCm = ReadNullableDecimal(reader, "LargoCm"),
                        AnchoCm = ReadNullableDecimal(reader, "AnchoCm"),
                        AltoCm = ReadNullableDecimal(reader, "AltoCm"),
                        UsaNumeroSerie = ReadBool(reader, "UsaNumeroSerie"),
                        CausaInventario = ReadBool(reader, "CausaInventario"),
                        PermiteVentaSinExistencia = ReadBool(reader, "PermiteVentaSinExistencia"),
                        ExistenciaActual = ReadNullableDecimal(reader, "ExistenciaActual"),
                        ExistenciaMinima = ReadNullableDecimal(reader, "ExistenciaMinima")
                    };
                }
            }

            if (ficha == null)
            {
                return null;
            }

            ficha.PresentacionesVenta = ficha.Tipo == TipoProducto
                ? await ObtenerPresentacionesVentaAsync(connection, null, idEmpresa, idProductoServicio, true)
                : new List<ProductoServicioPresentacionVentaDto>();
            ficha.Tags = await ObtenerTagsProductoAsync(connection, idEmpresa, idProductoServicio, string.Empty);
            ficha.Atributos = await ObtenerAtributosProductoAsync(connection, idEmpresa, idProductoServicio);
            ficha.Variantes = await ObtenerVariantesProductoAsync(connection, idEmpresa, idProductoServicio);
            ficha.Multimedia = await ObtenerMultimediaProductoAsync(connection, idEmpresa, idProductoServicio);
            ficha.PrecioUnitarioResumen = BuildPrecioUnitarioResumen(
                ficha.PrecioPublico,
                ficha.PrecioUnitarioCantidadTotal,
                ficha.PrecioUnitarioUnidadTotal,
                ficha.PrecioUnitarioBaseCantidad,
                ficha.PrecioUnitarioUnidadBase);
            ApplyLogisticsMetrics(ficha);
            await EnriquecerFichaSatAsync(ficha);
            return ficha;
        }

        private async Task EnriquecerFichaSatAsync(ProductoServicioFichaTecnicaDto ficha)
        {
            if (!string.IsNullOrWhiteSpace(ficha.ClaveProductoSat))
            {
                ficha.ClaveProductoSatDescripcion = await ResolveSatCatalogDescriptionAsync(
                    ficha.ClaveProductoSat,
                    term => BuscarClavesProductoSatAsync(new[] { term }, 10));
            }

            if (!string.IsNullOrWhiteSpace(ficha.ClaveUnidadSat))
            {
                ficha.ClaveUnidadSatDescripcion = await ResolveSatCatalogDescriptionAsync(
                ficha.ClaveUnidadSat,
                    term => BuscarClavesUnidadSatAsync(term, 25));
            }
        }

        private static decimal ResolveFactorVolumetrico(Guid _idEmpresa)
        {
            return FactorVolumetricoDefault;
        }

        private static ProductoServicioLogisticsMetrics CalculateLogisticsMetrics(
            byte tipo,
            bool esProductoFisico,
            decimal? pesoKg,
            decimal? paquetePesoEmpaqueVacioKg,
            decimal? paqueteLargoCm,
            decimal? paqueteAnchoCm,
            decimal? paqueteAltoCm,
            Guid? idPaquete,
            Guid idEmpresa)
        {
            decimal factorVolumetrico = ResolveFactorVolumetrico(idEmpresa);
            if (tipo != TipoProducto || !esProductoFisico)
            {
                return new ProductoServicioLogisticsMetrics
                {
                    FactorVolumetrico = factorVolumetrico
                };
            }

            decimal? pesoFisicoTotalKg = null;
            if (pesoKg.HasValue)
            {
                pesoFisicoTotalKg = pesoKg.Value + (paquetePesoEmpaqueVacioKg ?? 0m);
            }

            decimal? pesoVolumetricoKg = null;
            bool hasPaquete = idPaquete.HasValue && idPaquete.Value != Guid.Empty;
            if (hasPaquete &&
                paqueteLargoCm.HasValue && paqueteLargoCm.Value > 0 &&
                paqueteAnchoCm.HasValue && paqueteAnchoCm.Value > 0 &&
                paqueteAltoCm.HasValue && paqueteAltoCm.Value > 0 &&
                factorVolumetrico > 0)
            {
                pesoVolumetricoKg = (paqueteLargoCm.Value * paqueteAnchoCm.Value * paqueteAltoCm.Value) / factorVolumetrico;
            }

            decimal? pesoFacturableKg = null;
            if (pesoFisicoTotalKg.HasValue && pesoVolumetricoKg.HasValue)
            {
                pesoFacturableKg = Math.Max(pesoFisicoTotalKg.Value, pesoVolumetricoKg.Value);
            }
            else
            {
                pesoFacturableKg = pesoFisicoTotalKg ?? pesoVolumetricoKg;
            }

            return new ProductoServicioLogisticsMetrics
            {
                FactorVolumetrico = factorVolumetrico,
                PesoFisicoTotalKg = pesoFisicoTotalKg,
                PesoVolumetricoKg = pesoVolumetricoKg,
                PesoFacturableKg = pesoFacturableKg
            };
        }

        private static void ApplyLogisticsMetrics(ProductoServicioDetalleDto detalle)
        {
            ProductoServicioLogisticsMetrics metrics = CalculateLogisticsMetrics(
                detalle.Tipo,
                detalle.EsProductoFisico,
                detalle.PesoKg,
                detalle.PaquetePesoEmpaqueVacioKg,
                detalle.PaqueteLargoCm,
                detalle.PaqueteAnchoCm,
                detalle.PaqueteAltoCm,
                detalle.IdPaquete,
                detalle.IdEmpresa);

            detalle.PesoFisicoTotalKg = metrics.PesoFisicoTotalKg;
            detalle.PesoVolumetricoKg = metrics.PesoVolumetricoKg;
            detalle.PesoFacturableKg = metrics.PesoFacturableKg;
        }

        private static void ApplyLogisticsMetrics(ProductoServicioFichaTecnicaDto ficha)
        {
            ProductoServicioLogisticsMetrics metrics = CalculateLogisticsMetrics(
                ficha.Tipo,
                ficha.EsProductoFisico,
                ficha.PesoKg,
                ficha.PaquetePesoEmpaqueVacioKg,
                ficha.PaqueteLargoCm,
                ficha.PaqueteAnchoCm,
                ficha.PaqueteAltoCm,
                ficha.IdPaquete,
                ficha.IdEmpresa);

            ficha.PesoFisicoTotalKg = metrics.PesoFisicoTotalKg;
            ficha.PesoVolumetricoKg = metrics.PesoVolumetricoKg;
            ficha.PesoFacturableKg = metrics.PesoFacturableKg;
        }

        private static string BuildPrecioUnitarioResumen(decimal precioPublico, decimal? cantidadTotal, string unidadTotal, decimal? baseCantidad, string unidadBase)
        {
            if (!cantidadTotal.HasValue || cantidadTotal.Value <= 0 || !baseCantidad.HasValue || baseCantidad.Value <= 0 || string.IsNullOrWhiteSpace(unidadTotal) || string.IsNullOrWhiteSpace(unidadBase))
            {
                return string.Empty;
            }

            decimal? unitPrice = CalculateUnitPrice(precioPublico, cantidadTotal.Value, unidadTotal, baseCantidad.Value, unidadBase);
            if (!unitPrice.HasValue)
            {
                return string.Empty;
            }

            string baseText = baseCantidad.Value == 1m ? string.Empty : baseCantidad.Value.ToString("0.####", CultureInfo.InvariantCulture);
            return $"{unitPrice.Value.ToString("C2", CultureInfo.GetCultureInfo("es-MX"))}/{baseText}{unidadBase.Trim()}";
        }

        private static decimal? CalculateUnitPrice(decimal precioPublico, decimal cantidadTotal, string unidadTotal, decimal medidaBase, string unidadBase)
        {
            if (precioPublico <= 0 || cantidadTotal <= 0 || medidaBase <= 0)
            {
                return null;
            }

            UnitPriceUnitMeta? totalMeta = ResolveUnitPriceUnitMeta(unidadTotal);
            UnitPriceUnitMeta? baseMeta = ResolveUnitPriceUnitMeta(unidadBase);
            if (totalMeta == null || baseMeta == null || totalMeta.Family != baseMeta.Family)
            {
                return null;
            }

            decimal totalNormalizado = cantidadTotal * totalMeta.Factor;
            decimal baseNormalizada = medidaBase * baseMeta.Factor;
            if (totalNormalizado <= 0 || baseNormalizada <= 0)
            {
                return null;
            }

            return (precioPublico / totalNormalizado) * baseNormalizada;
        }

        private static UnitPriceUnitMeta? ResolveUnitPriceUnitMeta(string unidad)
        {
            return (unidad ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "kg" => new UnitPriceUnitMeta("mass", 1000m),
                "g" => new UnitPriceUnitMeta("mass", 1m),
                "lb" => new UnitPriceUnitMeta("mass", 453.59237m),
                "l" => new UnitPriceUnitMeta("volume", 1000m),
                "ml" => new UnitPriceUnitMeta("volume", 1m),
                "pz" => new UnitPriceUnitMeta("piece", 1m),
                "m" => new UnitPriceUnitMeta("length", 1m),
                _ => null
            };
        }

        private async Task<string> ResolveSatCatalogDescriptionAsync(string clave, Func<string, Task<List<ProductoServicioOpcionDto>>> search)
        {
            string normalized = (clave ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            try
            {
                List<ProductoServicioOpcionDto> results = await search(normalized);
                ProductoServicioOpcionDto? exact = results.FirstOrDefault(item => string.Equals(item.Clave, normalized, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                {
                    string label = (exact.Nombre ?? string.Empty).Trim();
                    int separatorIndex = label.IndexOf(" - ", StringComparison.Ordinal);
                    return separatorIndex >= 0 ? label[(separatorIndex + 3)..].Trim() : label;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No fue posible resolver la descripción SAT para la clave {Clave}.", normalized);
            }

            return string.Empty;
        }

        [HttpPost("SubirImagenTemporal")]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadTemporalRequestLimitBytes)]
        [RequestSizeLimit(UploadTemporalRequestLimitBytes)]
        public async Task<IActionResult> SubirImagenTemporal(Guid idEmpresa, IFormFile? archivo)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                if (archivo == null || archivo.Length <= 0)
                {
                    return BadRequest(new ProductoServicioImagenTemporalResponse { Mensaje = "Selecciona una imagen válida para cargar." });
                }

                string validacion = ValidateImagenTemporalUpload(archivo);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioImagenTemporalResponse { Mensaje = validacion });
                }

                byte[] fileBytes = await ReadFileBytesAsync(archivo);
                validacion = ValidateImageSignature(archivo.FileName, archivo.ContentType, fileBytes);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioImagenTemporalResponse { Mensaje = validacion });
                }

                UploadedImagePayload uploaded = await UploadImageToFirebaseAsync(
                    BuildTemporalFolderName(context.EmpresaStorageKey),
                    BuildStoredFileName(archivo.FileName, archivo.ContentType),
                    fileBytes,
                    archivo.FileName,
                    archivo.ContentType,
                    archivo.Length);

                return Ok(new ProductoServicioImagenTemporalResponse
                {
                    Mensaje = "La imagen temporal fue cargada.",
                    Archivo = new ProductoServicioImagenTemporalDto
                    {
                        TemporalToken = CreateTemporalToken(new TemporalImageTokenPayload
                        {
                            NombreOriginal = uploaded.NombreOriginal,
                            NombreAlmacenado = uploaded.NombreAlmacenado,
                            Extension = uploaded.Extension,
                            MimeType = uploaded.MimeType,
                            UrlFirebase = uploaded.UrlFirebase,
                            FolderName = uploaded.FolderName,
                            PesoBytes = uploaded.PesoBytes,
                            ExpiraUtc = DateTime.UtcNow.Add(TemporalTokenLifetime)
                        }),
                        NombreOriginal = uploaded.NombreOriginal,
                        NombreAlmacenado = uploaded.NombreAlmacenado,
                        Extension = uploaded.Extension,
                        MimeType = uploaded.MimeType,
                        UrlFirebase = uploaded.UrlFirebase,
                        PesoBytes = uploaded.PesoBytes
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "SubirImagenTemporal", "No fue posible procesar la imagen temporal.");
            }
        }

        [HttpPost("LimpiarImagenTemporal")]
        public async Task<IActionResult> LimpiarImagenTemporal(Guid idEmpresa, [FromBody] ProductoServicioImagenTemporalCleanupRequest? request)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                List<FirebaseCleanupItem> items = new List<FirebaseCleanupItem>();
                foreach (string token in request?.Tokens ?? new List<string>())
                {
                    TemporalImageTokenPayload? payload = TryParseTemporalToken(token);
                    if (payload == null ||
                        string.IsNullOrWhiteSpace(payload.FolderName) ||
                        string.IsNullOrWhiteSpace(payload.NombreAlmacenado) ||
                        !FolderBelongsToEmpresa(payload.FolderName, context.EmpresaStorageKey))
                    {
                        continue;
                    }

                    items.Add(new FirebaseCleanupItem
                    {
                        FolderName = payload.FolderName,
                        StoredName = payload.NombreAlmacenado
                    });
                }

                await CleanupUploadedFirebaseFilesAsync(items);
                return Ok(new ProductoServicioOperacionResponse { Mensaje = "La limpieza temporal fue procesada." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "LimpiarImagenTemporal", "No fue posible limpiar la imagen temporal.");
            }
        }

        [HttpPost("SubirMultimediaTemporal")]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadTemporalMultimediaRequestLimitBytes)]
        [RequestSizeLimit(UploadTemporalMultimediaRequestLimitBytes)]
        public async Task<IActionResult> SubirMultimediaTemporal(Guid idEmpresa, [FromForm] string tipoMultimedia, [FromForm] string operacionCarga, IFormFile? archivo)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                string tipo = NormalizeTipoMultimedia(tipoMultimedia);
                if (string.IsNullOrWhiteSpace(tipo))
                {
                    return BadRequest(new ProductoServicioMultimediaTemporalResponse { Mensaje = "Selecciona un tipo de evidencia válido." });
                }

                if (archivo == null || archivo.Length <= 0)
                {
                    return BadRequest(new ProductoServicioMultimediaTemporalResponse { Mensaje = "Selecciona un archivo válido para cargar." });
                }

                string validation = ValidateTemporalMultimediaUpload(tipo, archivo);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ProductoServicioMultimediaTemporalResponse { Mensaje = validation });
                }

                byte[] fileBytes = await ReadFileBytesAsync(archivo);
                string folderName = BuildTemporalMultimediaFolderName(context.EmpresaStorageKey, operacionCarga, tipo);
                UploadedImagePayload uploaded = await UploadImageToFirebaseAsync(
                    folderName,
                    BuildStoredFileName(archivo.FileName, archivo.ContentType),
                    fileBytes,
                    archivo.FileName,
                    archivo.ContentType,
                    archivo.Length);

                return Ok(new ProductoServicioMultimediaTemporalResponse
                {
                    Mensaje = "La evidencia temporal fue cargada.",
                    Archivo = new ProductoServicioMultimediaTemporalDto
                    {
                        TemporalToken = CreateTemporalToken(new TemporalImageTokenPayload
                        {
                            NombreOriginal = uploaded.NombreOriginal,
                            NombreAlmacenado = uploaded.NombreAlmacenado,
                            Extension = uploaded.Extension,
                            MimeType = uploaded.MimeType,
                            UrlFirebase = uploaded.UrlFirebase,
                            FolderName = uploaded.FolderName,
                            PesoBytes = uploaded.PesoBytes,
                            TipoMultimedia = tipo,
                            ExpiraUtc = DateTime.UtcNow.Add(TemporalTokenLifetime)
                        }),
                        TipoMultimedia = tipo,
                        NombreOriginal = uploaded.NombreOriginal,
                        NombreAlmacenado = uploaded.NombreAlmacenado,
                        Extension = uploaded.Extension,
                        MimeType = uploaded.MimeType,
                        UrlFirebase = uploaded.UrlFirebase,
                        PesoBytes = uploaded.PesoBytes
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "SubirMultimediaTemporal", "No fue posible procesar la evidencia temporal.");
            }
        }

        [HttpPost("LimpiarMultimediaTemporal")]
        public async Task<IActionResult> LimpiarMultimediaTemporal(Guid idEmpresa, [FromBody] ProductoServicioMultimediaTemporalCleanupRequest? request)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                List<FirebaseCleanupItem> items = new List<FirebaseCleanupItem>();
                foreach (string token in request?.Tokens ?? new List<string>())
                {
                    TemporalImageTokenPayload? payload = TryParseTemporalToken(token);
                    if (payload == null ||
                        string.IsNullOrWhiteSpace(payload.FolderName) ||
                        string.IsNullOrWhiteSpace(payload.NombreAlmacenado) ||
                        !FolderBelongsToEmpresa(payload.FolderName, context.EmpresaStorageKey))
                    {
                        continue;
                    }

                    items.Add(new FirebaseCleanupItem
                    {
                        FolderName = payload.FolderName,
                        StoredName = payload.NombreAlmacenado
                    });
                }

                await CleanupUploadedFirebaseFilesAsync(items);
                return Ok(new ProductoServicioOperacionResponse { Mensaje = "La limpieza temporal fue procesada." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "LimpiarMultimediaTemporal", "No fue posible limpiar la evidencia temporal.");
            }
        }

        [HttpPost("GuardarProductoServicio")]
        public async Task<IActionResult> GuardarProductoServicio([FromBody] ProductoServicioGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                string validacion = ValidateProductoServicioRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validacion });
                }

                NormalizedProductoServicioRequest normalized = NormalizeRequest(request);
                Guid productoId = normalized.Id ?? Guid.NewGuid();
                Guid? usuarioId = TryResolveUsuarioId();
                PreparedImageOperation preparedImage = await PrepareImageOperationAsync(context, productoId, normalized);
                PreparedMultimediaOperation preparedMultimedia = await PrepareMultimediaOperationAsync(context, productoId, normalized);
                PreparedVariantSyncResult variantSync = new PreparedVariantSyncResult();

                try
                {
                    using SqlConnection connection = CreateConnection(context);
                    await connection.OpenAsync();
                    using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                    bool esNuevo = !normalized.Id.HasValue || normalized.Id.Value == Guid.Empty;
                    ProductoServicioSnapshot? existente = esNuevo
                        ? null
                        : await ObtenerProductoServicioSnapshotAsync(connection, transaction, context.IdEmpresa, productoId);

                    if (!esNuevo && existente == null)
                    {
                        transaction.Rollback();
                        return NotFound(new ProductoServicioOperacionResponse { Mensaje = "El producto o servicio no está disponible para actualizar." });
                    }

                    if (await ExisteCodigoProductoServicioAsync(connection, transaction, context.IdEmpresa, normalized.Codigo, esNuevo ? null : productoId))
                    {
                        transaction.Rollback();
                        return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Ya existe un producto o servicio con el mismo código." });
                    }

                    string catalogoValidation = await ValidateCatalogReferencesAsync(connection, transaction, context.IdEmpresa, normalized, esNuevo, existente);
                    if (!string.IsNullOrWhiteSpace(catalogoValidation))
                    {
                        transaction.Rollback();
                        return BadRequest(new ProductoServicioOperacionResponse { Mensaje = catalogoValidation });
                    }

                    if (existente != null && existente.Tipo == TipoProducto && existente.IdUnidadMedida != normalized.IdUnidadMedida &&
                        await ContarPresentacionesVentaActivasAsync(connection, transaction, context.IdEmpresa, productoId) > 0)
                    {
                        transaction.Rollback();
                        return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "No puedes cambiar la unidad base porque existen presentaciones de venta configuradas." });
                    }

                    if (existente != null && existente.Tipo == TipoProducto && normalized.Tipo != TipoProducto &&
                        await ContarPresentacionesVentaActivasAsync(connection, transaction, context.IdEmpresa, productoId) > 0)
                    {
                        transaction.Rollback();
                        return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "El producto tiene presentaciones de venta configuradas. Elimínalas antes de cambiarlo a servicio." });
                    }

                    ProductoServicioExistenciaDto? existenciaActual = existente == null || existente.IdExistencia == null
                        ? null
                        : await ObtenerExistenciaInternaAsync(connection, transaction, context.IdEmpresa, productoId);
                    int movimientosHistoricos = await ContarMovimientosInventarioAsync(connection, transaction, context.IdEmpresa, productoId);

                    if (existente != null)
                    {
                        string transitionValidation = ValidateInventoryTransition(existente, normalized, existenciaActual, movimientosHistoricos);
                        if (!string.IsNullOrWhiteSpace(transitionValidation))
                        {
                            transaction.Rollback();
                            return BadRequest(new ProductoServicioOperacionResponse { Mensaje = transitionValidation });
                        }
                    }

                    ResolvedImageMutation imageMutation = ResolveImageMutation(existente, preparedImage);
                    DateTime ahora = DateTime.UtcNow;

                    if (esNuevo)
                    {
                        using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServicios
    (id, idEmpresa, identityKey, Tipo, Codigo, Tag, Nombre, Descripcion, idCategoria, idMarca, idUnidadMedida, idColeccion, idPaquete, Costo, PrecioPublico, PrecioComparacion, PrecioUnitarioMonto, PrecioUnitarioCantidadTotal, PrecioUnitarioUnidadTotal, PrecioUnitarioBaseCantidad, PrecioUnitarioUnidad, PrecioUnitarioUnidadBase, ObjetoImpuesto, PorcentajeIVA, ClaveProductoSat, ClaveUnidadSat, EsProductoFisico, PesoKg, LargoCm, AnchoCm, AltoCm, UsaNumeroSerie, CausaInventario, PermiteVentaSinExistencia, ImagenUrl, ImagenNombre, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Tipo, @Codigo, @Tag, @Nombre, @Descripcion, @IdCategoria, @IdMarca, @IdUnidadMedida, @IdColeccion, @IdPaquete, @Costo, @PrecioPublico, @PrecioComparacion, @PrecioUnitarioMonto, @PrecioUnitarioCantidadTotal, @PrecioUnitarioUnidadTotal, @PrecioUnitarioBaseCantidad, @PrecioUnitarioUnidad, @PrecioUnitarioUnidadBase, @ObjetoImpuesto, @PorcentajeIVA, @ClaveProductoSat, @ClaveUnidadSat, @EsProductoFisico, @PesoKg, @LargoCm, @AnchoCm, @AltoCm, @UsaNumeroSerie, @CausaInventario, @PermiteVentaSinExistencia, @ImagenUrl, @ImagenNombre, @Activo, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);

                        AddProductoServicioParameters(insert, productoId, context.IdEmpresa, normalized, imageMutation, ahora, true);
                        await insert.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServicios
SET
    Tipo = @Tipo,
    Codigo = @Codigo,
    Tag = @Tag,
    Nombre = @Nombre,
    Descripcion = @Descripcion,
    idCategoria = @IdCategoria,
    idMarca = @IdMarca,
    idUnidadMedida = @IdUnidadMedida,
    idColeccion = @IdColeccion,
    idPaquete = @IdPaquete,
    Costo = @Costo,
    PrecioPublico = @PrecioPublico,
    PrecioComparacion = @PrecioComparacion,
    -- LEGACY - REEMPLAZADO POR PRESENTACIONES DE VENTA. Conservar valores históricos al editar.
    PrecioUnitarioMonto = COALESCE(@PrecioUnitarioMonto, PrecioUnitarioMonto),
    PrecioUnitarioCantidadTotal = COALESCE(@PrecioUnitarioCantidadTotal, PrecioUnitarioCantidadTotal),
    PrecioUnitarioUnidadTotal = COALESCE(@PrecioUnitarioUnidadTotal, PrecioUnitarioUnidadTotal),
    PrecioUnitarioBaseCantidad = COALESCE(@PrecioUnitarioBaseCantidad, PrecioUnitarioBaseCantidad),
    PrecioUnitarioUnidad = COALESCE(@PrecioUnitarioUnidad, PrecioUnitarioUnidad),
    PrecioUnitarioUnidadBase = COALESCE(@PrecioUnitarioUnidadBase, PrecioUnitarioUnidadBase),
    ObjetoImpuesto = @ObjetoImpuesto,
    PorcentajeIVA = @PorcentajeIVA,
    ClaveProductoSat = @ClaveProductoSat,
    ClaveUnidadSat = @ClaveUnidadSat,
    EsProductoFisico = @EsProductoFisico,
    PesoKg = @PesoKg,
    LargoCm = @LargoCm,
    AnchoCm = @AnchoCm,
    AltoCm = @AltoCm,
    UsaNumeroSerie = @UsaNumeroSerie,
    CausaInventario = @CausaInventario,
    PermiteVentaSinExistencia = @PermiteVentaSinExistencia,
    ImagenUrl = @ImagenUrl,
    ImagenNombre = @ImagenNombre,
    Activo = @Activo,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);

                        AddProductoServicioParameters(update, productoId, context.IdEmpresa, normalized, imageMutation, ahora, false);
                        int rowsAffected = await update.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            transaction.Rollback();
                            return NotFound(new ProductoServicioOperacionResponse { Mensaje = "El producto o servicio no está disponible para actualizar." });
                        }
                    }

                    if (esNuevo && normalized.Tipo == TipoProducto && normalized.PresentacionesVenta.Count > 0)
                    {
                        await GuardarPresentacionesInicialesAsync(connection, transaction, context.IdEmpresa, productoId, normalized.IdUnidadMedida, normalized.PresentacionesVenta, ahora);
                    }
                    await EnsureAndSynchronizeBasePresentationAsync(connection, transaction, context.IdEmpresa, productoId, normalized.Tipo, normalized.IdUnidadMedida, normalized.PrecioPublico, ahora);
                    await SynchronizeInventoryForSaveAsync(connection, transaction, productoId, context.IdEmpresa, normalized, existente, existenciaActual, movimientosHistoricos, usuarioId, ahora);
                    await SynchronizeProductoTagsAsync(connection, transaction, context.IdEmpresa, productoId, normalized.Tags, ahora);
                    await SynchronizeProductoAtributosAsync(connection, transaction, context.IdEmpresa, productoId, normalized.Atributos, ahora);
                    Dictionary<string, VariantOptionReference> optionReferences = await SynchronizeProductoOpcionesVarianteAsync(connection, transaction, context.IdEmpresa, productoId, normalized.OpcionesVariante, ahora);
                    variantSync = await SynchronizeProductoVariantesAsync(connection, transaction, context, productoId, normalized.Variantes, optionReferences, ahora);
                    await SynchronizeProductoMultimediaAsync(connection, transaction, context.IdEmpresa, productoId, preparedMultimedia, ahora);
                    transaction.Commit();

                    await FinalizeImageOperationAfterCommitAsync(preparedImage, imageMutation.PreviousImageCleanup);
                    await FinalizeVariantSyncAfterCommitAsync(variantSync);
                    await FinalizeMultimediaOperationAfterCommitAsync(preparedMultimedia);
                    return Ok(new ProductoServicioOperacionResponse
                    {
                        Mensaje = esNuevo ? "El producto o servicio fue registrado." : "El producto o servicio fue actualizado."
                    });
                }
                catch (ProductoServicioValidationException validationEx)
                {
                    await CompensatePreparedVariantSyncAsync(variantSync);
                    await CompensatePreparedImageAsync(preparedImage);
                    await CompensatePreparedMultimediaAsync(preparedMultimedia);
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validationEx.Message });
                }
                catch (Exception ex)
                {
                    await CompensatePreparedVariantSyncAsync(variantSync);
                    await CompensatePreparedImageAsync(preparedImage);
                    await CompensatePreparedMultimediaAsync(preparedMultimedia);
                    return HandleException(ex, "GuardarProductoServicio", "No fue posible completar el guardado del producto o servicio.");
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarProductoServicio_Preparacion", "No fue posible completar el guardado del producto o servicio.");
            }
        }

        [HttpPost("BajaProductoServicio")]
        public async Task<IActionResult> BajaProductoServicio(Guid idEmpresa, Guid idProductoServicio)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            return await CambiarEstatusProductoServicioAsync(context, context.IdEmpresa, idProductoServicio, false);
        }

        [HttpPost("ActivarProductoServicio")]
        public async Task<IActionResult> ActivarProductoServicio(Guid idEmpresa, Guid idProductoServicio)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            return await CambiarEstatusProductoServicioAsync(context, context.IdEmpresa, idProductoServicio, true);
        }

        [HttpPost("GuardarPresentacionVentaProductoServicio")]
        public async Task<IActionResult> GuardarPresentacionVentaProductoServicio([FromBody] ProductoServicioPresentacionVentaGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write)) return error!;
            if (request.IdProductoServicio == Guid.Empty || request.IdUnidadVenta == Guid.Empty || request.CantidadVenta <= 0 || request.EquivalenciaBase <= 0 || request.Precio < 0 || request.Orden < 0)
                return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Los datos de la presentación no son válidos." });

            try
            {
                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                ProductoServicioSnapshot? producto = await ObtenerProductoServicioSnapshotAsync(connection, transaction, context.IdEmpresa, request.IdProductoServicio);
                if (producto == null || producto.Tipo != TipoProducto)
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Las presentaciones de venta solo aplican a productos disponibles." });
                }

                if (request.IdUnidadVenta == producto.IdUnidadMedida)
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = UnidadBasePresentacionMensaje });
                }
                ProductoServicioUnidadMedidaDto? unidad = await ObtenerUnidadInternaAsync(connection, transaction, context.IdEmpresa, producto.IdUnidadMedida);
                if (unidad == null || (!unidad.PermiteDecimales && decimal.Truncate(request.EquivalenciaBase) != request.EquivalenciaBase))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "La equivalencia no cumple con la precisión de la unidad base." });
                }
                ProductoServicioUnidadMedidaDto? unidadVenta = await ObtenerUnidadInternaAsync(connection, transaction, context.IdEmpresa, request.IdUnidadVenta);
                if (unidadVenta == null || !unidadVenta.Activo || (!unidadVenta.PermiteDecimales && decimal.Truncate(request.CantidadVenta) != request.CantidadVenta))
                {
                    transaction.Rollback(); return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "La unidad o cantidad de venta no es válida." });
                }
                request.Nombre = Truncate(unidadVenta.Nombre, 100); // Compatibility with the legacy column; authoritative catalog name.
                bool conversionFisica = unidad.Convertible && unidadVenta.Convertible && unidad.TipoUnidad == unidadVenta.TipoUnidad && unidad.TipoUnidad != "OTHER";
                bool conversionManual = unidadVenta.TipoUnidad == "OTHER";
                if (!conversionFisica && !conversionManual)
                {
                    transaction.Rollback(); return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "La unidad de venta no es compatible con la unidad base." });
                }
                if (conversionFisica)
                {
                    request.EquivalenciaBase = decimal.Round(request.CantidadVenta * unidadVenta.FactorConversion!.Value / unidad.FactorConversion!.Value, 4, MidpointRounding.AwayFromZero);
                }

                DateTime ahora = DateTime.UtcNow;
                Guid id = request.Id.GetValueOrDefault();
                bool esNueva = id == Guid.Empty;
                List<ProductoServicioPresentacionVentaDto> activas = await ObtenerPresentacionesVentaAsync(connection, transaction, context.IdEmpresa, producto.Id, true);
                ProductoServicioPresentacionVentaDto? actual = esNueva ? null : activas.FirstOrDefault(item => item.Id == id);
                if (!esNueva && actual == null)
                {
                    transaction.Rollback();
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "La presentación no está disponible." });
                }
                if ((actual != null && EsPresentacionBase(actual, producto.IdUnidadMedida))
                    || (request.IdUnidadVenta == producto.IdUnidadMedida && request.CantidadVenta == 1 && request.EquivalenciaBase == 1))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "La presentación base se administra automáticamente desde Unidad base y Precio público." });
                }
                if (activas.Any(item => item.Id != id && EsPresentacionDuplicada(item, request)))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = PresentacionDuplicadaMensaje });
                }
                bool makeDefault = false;

                if (esNueva)
                {
                    id = Guid.NewGuid();
                    using SqlCommand insert = new SqlCommand(@"INSERT INTO dbo.ProductosServiciosPresentacionesVenta (id, idEmpresa, identityKey, idProductoServicio, Nombre, CantidadVenta, idUnidadVenta, EquivalenciaBase, Precio, EsPredeterminada, Orden, Activo, FechaCreacion, FechaActualizacion) VALUES (@Id, @IdEmpresa, NEWID(), @ProductoId, @Nombre, @CantidadVenta, @IdUnidadVenta, @EquivalenciaBase, @Precio, @Predeterminada, @Orden, 1, @Ahora, @Ahora)", connection, transaction);
                    AddPresentacionParameters(insert, id, context.IdEmpresa, producto.Id, request, makeDefault, ahora);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand(@"UPDATE dbo.ProductosServiciosPresentacionesVenta SET Nombre = @Nombre, CantidadVenta = @CantidadVenta, idUnidadVenta = @IdUnidadVenta, EquivalenciaBase = @EquivalenciaBase, Precio = @Precio, EsPredeterminada = @Predeterminada, Orden = @Orden, FechaActualizacion = @Ahora WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
                    AddPresentacionParameters(update, id, context.IdEmpresa, producto.Id, request, makeDefault, ahora);
                    await update.ExecuteNonQueryAsync();
                }

                await EnsureAndSynchronizeBasePresentationAsync(connection, transaction, context.IdEmpresa, producto.Id, producto.Tipo, producto.IdUnidadMedida, producto.PrecioPublico, ahora);
                transaction.Commit();
                return Ok(new ProductoServicioOperacionResponse { Id = id, Mensaje = esNueva ? "La presentación fue registrada." : "La presentación fue actualizada." });
            }
            catch (Exception ex) { return HandleException(ex, "GuardarPresentacionVentaProductoServicio", "No fue posible guardar la presentación de venta."); }
        }

        [HttpPost("BajaPresentacionVentaProductoServicio")]
        public async Task<IActionResult> BajaPresentacionVentaProductoServicio(Guid idEmpresa, Guid idPresentacionVenta)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write)) return error!;
            try
            {
                using SqlConnection connection = CreateConnection(context); await connection.OpenAsync(); using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                List<ProductoServicioPresentacionVentaDto> item = await ObtenerPresentacionesVentaPorIdAsync(connection, transaction, context.IdEmpresa, idPresentacionVenta);
                ProductoServicioPresentacionVentaDto? presentacion = item.FirstOrDefault();
                if (presentacion == null) { transaction.Rollback(); return NotFound(new ProductoServicioOperacionResponse { Mensaje = "La presentación no está disponible." }); }
                ProductoServicioSnapshot? producto = await ObtenerProductoServicioSnapshotAsync(connection, transaction, context.IdEmpresa, presentacion.IdProductoServicio);
                if (producto != null && EsPresentacionBase(presentacion, producto.IdUnidadMedida)) { transaction.Rollback(); return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "La presentación base no se puede dar de baja; se mantiene desde el producto." }); }
                using SqlCommand archive = new SqlCommand(@"UPDATE dbo.ProductosServiciosPresentacionesVenta SET Activo = 0, FechaArchivado = @Ahora, FechaActualizacion = @Ahora WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
                archive.Parameters.AddWithValue("@Ahora", DateTime.UtcNow); archive.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa); archive.Parameters.AddWithValue("@Id", idPresentacionVenta);
                await archive.ExecuteNonQueryAsync(); transaction.Commit();
                return Ok(new ProductoServicioOperacionResponse { Mensaje = "La presentación fue dada de baja." });
            }
            catch (Exception ex) { return HandleException(ex, "BajaPresentacionVentaProductoServicio", "No fue posible dar de baja la presentación de venta."); }
        }

        [HttpPost("CalcularPresentacionesVentaProductoServicio")]
        public async Task<IActionResult> CalcularPresentacionesVentaProductoServicio([FromBody] ProductoServicioPresentacionesVentaCalcularRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read)) return error!;
            try
            {
                using SqlConnection connection = CreateConnection(context); await connection.OpenAsync();
                ProductoServicioDetalleDto? producto = await ObtenerProductoServicioParaCalculoAsync(connection, context.IdEmpresa, request.IdProductoServicio);
                if (producto == null || producto.Tipo != TipoProducto) return NotFound(new ProductoServicioOperacionResponse { Mensaje = "El producto no está disponible." });
                List<ProductoServicioPresentacionVentaDto> presentaciones = await ObtenerPresentacionesVentaAsync(connection, null, context.IdEmpresa, producto.Id, true);
                return Ok(new ProductoPresentacionVentaPricingEngine().Calcular(request.CantidadUnidadBase, producto.UnidadPermiteDecimales, presentaciones));
            }
            catch (Exception ex) { return HandleException(ex, "CalcularPresentacionesVentaProductoServicio", "No fue posible calcular las presentaciones de venta."); }
        }

        [HttpGet("ObtenerCombosProductosServicios")]
        public async Task<IActionResult> ObtenerCombosProductosServicios(Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                ProductoServicioCombosDto response = new ProductoServicioCombosDto
                {
                    FactorVolumetrico = ResolveFactorVolumetrico(context.IdEmpresa),
                    Categorias = await ObtenerCategoriasComboAsync(context, context.IdEmpresa, null),
                    Marcas = await ObtenerCatalogoBasicoComboAsync(context, context.IdEmpresa, "dbo.ProductosServiciosMarcas"),
                    UnidadesMedida = await ObtenerUnidadesComboAsync(context, context.IdEmpresa),
                    Colecciones = await ObtenerColeccionesComboAsync(context, context.IdEmpresa),
                    Paquetes = await ObtenerPaquetesComboAsync(context, context.IdEmpresa),
                    Atributos = await ObtenerAtributosComboAsync(context, context.IdEmpresa),
                    Tags = await ObtenerTagsCatalogoAsync(context, context.IdEmpresa),
                    Tipos = new List<ProductoServicioOpcionDto>
                    {
                        new ProductoServicioOpcionDto { Clave = TipoProducto.ToString(), Nombre = "Producto" },
                        new ProductoServicioOpcionDto { Clave = TipoServicio.ToString(), Nombre = "Servicio" }
                    },
                    Estatus = new List<ProductoServicioOpcionDto>
                    {
                        new ProductoServicioOpcionDto { Clave = "activos", Nombre = "Activos" },
                        new ProductoServicioOpcionDto { Clave = "inactivos", Nombre = "Inactivos" },
                        new ProductoServicioOpcionDto { Clave = "todos", Nombre = "Todos" }
                    },
                    ObjetosImpuesto = new List<ProductoServicioOpcionDto>
                    {
                        new ProductoServicioOpcionDto { Clave = "01", Nombre = "01 · No objeto de impuesto" },
                        new ProductoServicioOpcionDto { Clave = "02", Nombre = "02 · Sí objeto de impuesto" },
                        new ProductoServicioOpcionDto { Clave = "03", Nombre = "03 · Sí objeto y no obligado al desglose" },
                        new ProductoServicioOpcionDto { Clave = "04", Nombre = "04 · Sí objeto y no causa impuesto" }
                    },
                    TiposPaquete = new List<ProductoServicioOpcionDto>
                    {
                        new ProductoServicioOpcionDto { Clave = "caja", Nombre = "Caja" },
                        new ProductoServicioOpcionDto { Clave = "sobre", Nombre = "Sobre" },
                        new ProductoServicioOpcionDto { Clave = "flexible", Nombre = "Paquete flexible" }
                    },
                    UnidadesPrecioUnitario = new List<ProductoServicioOpcionDto>
                    {
                        new ProductoServicioOpcionDto { Clave = "kg", Nombre = "kg" },
                        new ProductoServicioOpcionDto { Clave = "g", Nombre = "g" },
                        new ProductoServicioOpcionDto { Clave = "lb", Nombre = "lb" },
                        new ProductoServicioOpcionDto { Clave = "l", Nombre = "l" },
                        new ProductoServicioOpcionDto { Clave = "ml", Nombre = "ml" },
                        new ProductoServicioOpcionDto { Clave = "pz", Nombre = "pz" },
                        new ProductoServicioOpcionDto { Clave = "m", Nombre = "m" }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCombosProductosServicios", "No fue posible cargar los catálogos del módulo.");
            }
        }

        [HttpPost("GuardarTagProductoServicio")]
        public async Task<IActionResult> GuardarTagProductoServicio([FromBody] ProductoServicioTagGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                string nombre = Truncate(request.Nombre ?? string.Empty, TagLength).Trim();
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Captura un nombre de etiqueta." });
                }

                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                ProductoServicioTagDto tag = await ResolveOrCreateTagAsync(connection, transaction, context.IdEmpresa, nombre, DateTime.UtcNow);
                transaction.Commit();

                return Ok(new ProductoServicioTagOperacionResponse
                {
                    Mensaje = "La etiqueta fue guardada.",
                    Tag = tag
                });
            }
            catch (ProductoServicioValidationException validationEx)
            {
                return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validationEx.Message });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarTagProductoServicio", "No fue posible guardar la etiqueta.");
            }
        }

        [HttpGet("BuscarCatalogosSatProductoServicio")]
        public async Task<IActionResult> BuscarCatalogosSatProductoServicio(Guid idEmpresa, string tipo = "", string q = "", int take = 40)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            _ = context;

            try
            {
                string tipoNormalizado = (tipo ?? string.Empty).Trim().ToLowerInvariant();
                if (tipoNormalizado != "producto" && tipoNormalizado != "unidad")
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Selecciona un catálogo SAT válido." });
                }

                int top = Math.Clamp(take, 20, 120);
                List<ProductoServicioOpcionDto> items;

                if (tipoNormalizado == "producto")
                {
                    string[] terms = ParseSatTerms(q);
                    if (terms.Length == 0)
                    {
                        return Ok(new ProductoServicioSatCatalogosResponseDto());
                    }

                    items = await BuscarClavesProductoSatAsync(terms, top);
                }
                else
                {
                    items = await BuscarClavesUnidadSatAsync(q, top);
                }

                return Ok(new ProductoServicioSatCatalogosResponseDto { Items = items });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "BuscarCatalogosSatProductoServicio", "No fue posible consultar el catálogo SAT.");
            }
        }

        [HttpGet("ObtenerResumenProductosServicios")]
        public async Task<IActionResult> ObtenerResumenProductosServicios(Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
SELECT
    COUNT(1) AS TotalRegistros,
    SUM(CASE WHEN ps.Activo = 1 THEN 1 ELSE 0 END) AS TotalActivos,
    SUM(CASE WHEN ps.Activo = 0 THEN 1 ELSE 0 END) AS TotalInactivos,
    SUM(CASE WHEN ps.Tipo = 1 THEN 1 ELSE 0 END) AS TotalProductos,
    SUM(CASE WHEN ps.Tipo = 2 THEN 1 ELSE 0 END) AS TotalServicios,
    SUM(CASE WHEN ps.CausaInventario = 1 THEN 1 ELSE 0 END) AS TotalConInventario,
    SUM(CASE WHEN ps.CausaInventario = 0 THEN 1 ELSE 0 END) AS TotalSinInventario,
    SUM(CASE WHEN ps.PermiteVentaSinExistencia = 1 THEN 1 ELSE 0 END) AS TotalInventarioNegativoPermitido,
    SUM(CASE WHEN ps.CausaInventario = 1 AND ISNULL(inv.ExistenciaActual, 0) <= 0 THEN 1 ELSE 0 END) AS TotalBajoMinimo,
    SUM(CASE WHEN ps.CausaInventario = 1 THEN ISNULL(inv.ExistenciaActual, 0) * ISNULL(ps.Costo, 0) ELSE 0 END) AS ValorInventarioEstimado
FROM dbo.ProductosServicios ps
OUTER APPLY (
    SELECT SUM(s.CantidadBaseActual) AS ExistenciaActual
    FROM dbo.InventarioSaldos s
    WHERE s.idEmpresa = ps.idEmpresa AND s.idProductoServicio = ps.id
) inv
WHERE ps.idEmpresa = @IdEmpresa", connection);

                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);

                using SqlDataReader reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return Ok(new ProductoServicioKpiResumenDto());
                }

                return Ok(new ProductoServicioKpiResumenDto
                {
                    TotalRegistros = ReadInt(reader, "TotalRegistros"),
                    TotalActivos = ReadInt(reader, "TotalActivos"),
                    TotalInactivos = ReadInt(reader, "TotalInactivos"),
                    TotalProductos = ReadInt(reader, "TotalProductos"),
                    TotalServicios = ReadInt(reader, "TotalServicios"),
                    TotalConInventario = ReadInt(reader, "TotalConInventario"),
                    TotalSinInventario = ReadInt(reader, "TotalSinInventario"),
                    TotalInventarioNegativoPermitido = ReadInt(reader, "TotalInventarioNegativoPermitido"),
                    TotalBajoMinimo = ReadInt(reader, "TotalBajoMinimo"),
                    ValorInventarioEstimado = ReadDecimal(reader, "ValorInventarioEstimado")
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerResumenProductosServicios", "No fue posible cargar el resumen del módulo.");
            }
        }

        [HttpGet("ExportarProductosServicios")]
        public async Task<IActionResult> ExportarProductosServicios(
            Guid idEmpresa,
            string busqueda = "",
            byte? tipo = null,
            Guid? idCategoria = null,
            Guid? idMarca = null,
            Guid? idUnidadMedida = null,
            bool? causaInventario = null,
            string estatus = "")
        {
            IActionResult listadoResult = await ObtenerProductosServicios(idEmpresa, busqueda, tipo, idCategoria, idMarca, idUnidadMedida, causaInventario, estatus);
            if (listadoResult is not OkObjectResult ok || ok.Value is not List<ProductoServicioListadoDto> items)
            {
                return listadoResult;
            }

            return Ok(items.Select(item => new ProductoServicioExportacionDto
            {
                Tipo = item.TipoNombre,
                Codigo = item.Codigo,
                Tag = item.Tag,
                Nombre = item.Nombre,
                Descripcion = item.Descripcion,
                Categoria = item.Categoria,
                Marca = item.Marca,
                UnidadMedida = item.UnidadMedida,
                UnidadAbreviatura = item.UnidadAbreviatura,
                Costo = item.Costo,
                PrecioPublico = item.PrecioPublico,
                CausaInventario = item.CausaInventario,
                PermiteVentaSinExistencia = item.PermiteVentaSinExistencia,
                ExistenciaActual = item.ExistenciaActual,
                ExistenciaMinima = item.ExistenciaMinima,
                Activo = item.Activo,
                FechaCreacion = item.FechaCreacion,
                FechaActualizacion = item.FechaActualizacion
            }).ToList());
        }

        [HttpGet("ObtenerCategoriasProductosServicios")]
        public async Task<IActionResult> ObtenerCategoriasProductosServicios(Guid idEmpresa, string busqueda = "", string estatus = "")
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                return Ok(await ObtenerCategoriasListadoAsync(context, context.IdEmpresa, busqueda, estatus));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCategoriasProductosServicios", "No fue posible cargar las categorías.");
            }
        }

        [HttpGet("ObtenerCategoriaProductoServicio")]
        public async Task<IActionResult> ObtenerCategoriaProductoServicio(Guid idEmpresa, Guid idCategoria)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                ProductoServicioCategoriaDto? item = await ObtenerCategoriaAsync(context, context.IdEmpresa, idCategoria);
                if (item == null)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "La categoría no está disponible." });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCategoriaProductoServicio", "No fue posible cargar la categoría.");
            }
        }

        [HttpPost("GuardarCategoriaProductoServicio")]
        public async Task<IActionResult> GuardarCategoriaProductoServicio([FromBody] ProductoServicioCategoriaGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                string validacion = ValidateCategoriaRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validacion });
                }

                return await GuardarCategoriaAsync(context, request, context.IdEmpresa);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarCategoriaProductoServicio", "No fue posible guardar la categoría.");
            }
        }

        [HttpPost("BajaCategoriaProductoServicio")]
        public async Task<IActionResult> BajaCategoriaProductoServicio(Guid idEmpresa, Guid idCategoria)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            return await CambiarEstatusCatalogoBasicoAsync(context, context.IdEmpresa, idCategoria, "dbo.ProductosServiciosCategorias", "la categoría", false);
        }

        [HttpPost("ActivarCategoriaProductoServicio")]
        public async Task<IActionResult> ActivarCategoriaProductoServicio(Guid idEmpresa, Guid idCategoria)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            return await CambiarEstatusCatalogoBasicoAsync(context, context.IdEmpresa, idCategoria, "dbo.ProductosServiciosCategorias", "la categoría", true);
        }

        [HttpGet("ObtenerCatalogoCategoriasProductosServicios")]
        public async Task<IActionResult> ObtenerCatalogoCategoriasProductosServicios(Guid idEmpresa, byte? tipo = null, string busqueda = "")
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                return Ok(await ObtenerCategoriasComboAsync(context, context.IdEmpresa, tipo, busqueda));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCatalogoCategoriasProductosServicios", "No fue posible cargar el catálogo de categorías.");
            }
        }

        [HttpGet("ExportarCategoriasProductosServicios")]
        public async Task<IActionResult> ExportarCategoriasProductosServicios(Guid idEmpresa, string busqueda = "", string estatus = "")
        {
            return await ObtenerCategoriasProductosServicios(idEmpresa, busqueda, estatus);
        }

        [HttpGet("ObtenerMarcasProductosServicios")]
        public async Task<IActionResult> ObtenerMarcasProductosServicios(Guid idEmpresa, string busqueda = "", string estatus = "")
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                return Ok(await ObtenerMarcasListadoAsync(context, context.IdEmpresa, busqueda, estatus));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerMarcasProductosServicios", "No fue posible cargar las marcas.");
            }
        }

        [HttpGet("ObtenerMarcaProductoServicio")]
        public async Task<IActionResult> ObtenerMarcaProductoServicio(Guid idEmpresa, Guid idMarca)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                ProductoServicioMarcaDto? item = await ObtenerMarcaAsync(context, context.IdEmpresa, idMarca);
                if (item == null)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "La marca no está disponible." });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerMarcaProductoServicio", "No fue posible cargar la marca.");
            }
        }

        [HttpPost("GuardarMarcaProductoServicio")]
        public async Task<IActionResult> GuardarMarcaProductoServicio([FromBody] ProductoServicioMarcaGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                string validacion = ValidateMarcaRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validacion });
                }

                return await GuardarCatalogoBasicoAsync(
                    context,
                    request.Id,
                    context.IdEmpresa,
                    "dbo.ProductosServiciosMarcas",
                    "la marca",
                    "Ya existe una marca con este código.",
                    request.Codigo,
                    request.Nombre,
                    request.Descripcion,
                    null,
                    null,
                    false,
                    duplicateNameMessage: "Ya existe una marca con este nombre.");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarMarcaProductoServicio", "No fue posible guardar la marca.");
            }
        }

        [HttpPost("BajaMarcaProductoServicio")]
        public async Task<IActionResult> BajaMarcaProductoServicio(Guid idEmpresa, Guid idMarca)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            return await CambiarEstatusCatalogoBasicoAsync(context, context.IdEmpresa, idMarca, "dbo.ProductosServiciosMarcas", "la marca", false);
        }

        [HttpPost("ActivarMarcaProductoServicio")]
        public async Task<IActionResult> ActivarMarcaProductoServicio(Guid idEmpresa, Guid idMarca)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            return await CambiarEstatusCatalogoBasicoAsync(context, context.IdEmpresa, idMarca, "dbo.ProductosServiciosMarcas", "la marca", true);
        }

        [HttpGet("ObtenerCatalogoMarcasProductosServicios")]
        public async Task<IActionResult> ObtenerCatalogoMarcasProductosServicios(Guid idEmpresa, string busqueda = "")
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                return Ok(await ObtenerCatalogoBasicoComboAsync(context, context.IdEmpresa, "dbo.ProductosServiciosMarcas", busqueda));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCatalogoMarcasProductosServicios", "No fue posible cargar el catálogo de marcas.");
            }
        }

        [HttpGet("ExportarMarcasProductosServicios")]
        public async Task<IActionResult> ExportarMarcasProductosServicios(Guid idEmpresa, string busqueda = "", string estatus = "")
        {
            return await ObtenerMarcasProductosServicios(idEmpresa, busqueda, estatus);
        }

        [HttpGet("ObtenerUnidadesMedidaProductosServicios")]
        public async Task<IActionResult> ObtenerUnidadesMedidaProductosServicios(Guid idEmpresa, string busqueda = "", string estatus = "")
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                return Ok(await ObtenerUnidadesListadoAsync(context, context.IdEmpresa, busqueda, estatus));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerUnidadesMedidaProductosServicios", "No fue posible cargar las unidades de medida.");
            }
        }

        [HttpGet("ObtenerUnidadMedidaProductoServicio")]
        public async Task<IActionResult> ObtenerUnidadMedidaProductoServicio(Guid idEmpresa, Guid idUnidadMedida)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                ProductoServicioUnidadMedidaDto? item = await ObtenerUnidadAsync(context, context.IdEmpresa, idUnidadMedida);
                if (item == null)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "La unidad de medida no está disponible." });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerUnidadMedidaProductoServicio", "No fue posible cargar la unidad de medida.");
            }
        }

        [HttpPost("GuardarUnidadMedidaProductoServicio")]
        public async Task<IActionResult> GuardarUnidadMedidaProductoServicio([FromBody] ProductoServicioUnidadMedidaGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                string validacion = ValidateUnidadRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validacion });
                }

                return await GuardarUnidadControladaAsync(context, request, context.IdEmpresa);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarUnidadMedidaProductoServicio", "No fue posible guardar la unidad de medida.");
            }
        }

        [HttpPost("BajaUnidadMedidaProductoServicio")]
        public async Task<IActionResult> BajaUnidadMedidaProductoServicio(Guid idEmpresa, Guid idUnidadMedida)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            return await CambiarEstatusCatalogoBasicoAsync(context, context.IdEmpresa, idUnidadMedida, "dbo.ProductosServiciosUnidadesMedida", "la unidad de medida", false);
        }

        [HttpPost("ActivarUnidadMedidaProductoServicio")]
        public async Task<IActionResult> ActivarUnidadMedidaProductoServicio(Guid idEmpresa, Guid idUnidadMedida)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            return await CambiarEstatusCatalogoBasicoAsync(context, context.IdEmpresa, idUnidadMedida, "dbo.ProductosServiciosUnidadesMedida", "la unidad de medida", true);
        }

        [HttpGet("ObtenerCatalogoUnidadesMedidaProductosServicios")]
        public async Task<IActionResult> ObtenerCatalogoUnidadesMedidaProductosServicios(Guid idEmpresa, string busqueda = "")
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                return Ok(await ObtenerUnidadesComboAsync(context, context.IdEmpresa, busqueda));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCatalogoUnidadesMedidaProductosServicios", "No fue posible cargar el catálogo de unidades de medida.");
            }
        }

        [HttpGet("ExportarUnidadesMedidaProductosServicios")]
        public async Task<IActionResult> ExportarUnidadesMedidaProductosServicios(Guid idEmpresa, string busqueda = "", string estatus = "")
        {
            return await ObtenerUnidadesMedidaProductosServicios(idEmpresa, busqueda, estatus);
        }

        [HttpPost("GuardarColeccionProductoServicio")]
        public async Task<IActionResult> GuardarColeccionProductoServicio([FromBody] ProductoServicioColeccionGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                string validation = ValidateColeccionRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validation });
                }

                request.Descripcion = NormalizeDescripcionCatalogo(request.Descripcion);

                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                Guid id = request.Id ?? Guid.NewGuid();
                bool esNuevo = !request.Id.HasValue || request.Id.Value == Guid.Empty;
                string numeroPersistido = esNuevo
                    ? await GenerateNextCollectionNumberAsync(connection, transaction, context.IdEmpresa)
                    : await ObtenerNumeroColeccionAsync(connection, transaction, context.IdEmpresa, id);

                if (string.IsNullOrWhiteSpace(numeroPersistido))
                {
                    transaction.Rollback();
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "No fue posible guardar la colección." });
                }

                if (await ExisteNumeroColeccionAsync(connection, transaction, context.IdEmpresa, numeroPersistido, esNuevo ? null : id))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Ya existe una colección con este número." });
                }

                if (await ExisteNombreColeccionAsync(connection, transaction, context.IdEmpresa, request.Nombre.Trim(), esNuevo ? null : id))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Ya existe una colección con este nombre." });
                }

                DateTime ahora = DateTime.UtcNow;
                if (esNuevo)
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosColecciones
    (id, idEmpresa, identityKey, Numero, Nombre, Descripcion, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Numero, @Nombre, @Descripcion, 1, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);
                    insert.Parameters.AddWithValue("@Id", id);
                    insert.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insert.Parameters.AddWithValue("@Numero", numeroPersistido);
                    insert.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    insert.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(request.Descripcion) ? DBNull.Value : request.Descripcion);
                    insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServiciosColecciones
SET Numero = @Numero,
    Nombre = @Nombre,
    Descripcion = @Descripcion,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
                    update.Parameters.AddWithValue("@Id", id);
                    update.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    update.Parameters.AddWithValue("@Numero", numeroPersistido);
                    update.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    update.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(request.Descripcion) ? DBNull.Value : request.Descripcion);
                    update.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await update.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return Ok(new ProductoServicioColeccionOperacionResponse
                {
                    Mensaje = esNuevo ? "Se registró la colección." : "Se actualizó la colección.",
                    Coleccion = new ProductoServicioCatalogoComboDto
                    {
                        Id = id,
                        Numero = numeroPersistido,
                        Nombre = request.Nombre.Trim(),
                        Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? string.Empty : request.Descripcion,
                        Activo = true
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarColeccionProductoServicio", "No fue posible guardar la colección.");
            }
        }

        [HttpPost("GuardarPaqueteProductoServicio")]
        public async Task<IActionResult> GuardarPaqueteProductoServicio([FromBody] ProductoServicioPaqueteGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                string validation = ValidatePaqueteRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validation });
                }

                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                Guid id = request.Id ?? Guid.NewGuid();
                bool esNuevo = !request.Id.HasValue || request.Id.Value == Guid.Empty;
                DateTime ahora = DateTime.UtcNow;

                if (await ExistePaqueteMismoNombreTipoAsync(connection, transaction, context.IdEmpresa, request.Nombre.Trim(), request.TipoPaquete.Trim(), esNuevo ? null : id))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Ya existe un paquete con el mismo nombre y tipo." });
                }

                if (request.EsPredeterminado)
                {
                    using SqlCommand clearDefaults = new SqlCommand(@"
UPDATE dbo.ProductosServiciosPaquetes
SET EsPredeterminado = 0,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND EsPredeterminado = 1 AND (@IdActual IS NULL OR id <> @IdActual)", connection, transaction);
                    clearDefaults.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    clearDefaults.Parameters.AddWithValue("@IdActual", esNuevo ? DBNull.Value : id);
                    clearDefaults.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await clearDefaults.ExecuteNonQueryAsync();
                }

                if (esNuevo)
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosPaquetes
    (id, idEmpresa, identityKey, Nombre, TipoPaquete, LargoCm, AnchoCm, AltoCm, PesoEmpaqueVacioKg, EsPredeterminado, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Nombre, @TipoPaquete, @LargoCm, @AnchoCm, @AltoCm, @PesoEmpaqueVacioKg, @EsPredeterminado, 1, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);
                    AddPaqueteParameters(insert, id, context.IdEmpresa, request, ahora, true);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServiciosPaquetes
SET Nombre = @Nombre,
    TipoPaquete = @TipoPaquete,
    LargoCm = @LargoCm,
    AnchoCm = @AnchoCm,
    AltoCm = @AltoCm,
    PesoEmpaqueVacioKg = @PesoEmpaqueVacioKg,
    EsPredeterminado = @EsPredeterminado,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
                    AddPaqueteParameters(update, id, context.IdEmpresa, request, ahora, false);
                    await update.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return Ok(new ProductoServicioPaqueteOperacionResponse
                {
                    Mensaje = esNuevo ? "Se registró el paquete." : "Se actualizó el paquete.",
                    Paquete = new ProductoServicioCatalogoComboDto
                    {
                        Id = id,
                        Nombre = request.Nombre.Trim(),
                        TipoPaquete = request.TipoPaquete.Trim(),
                        LargoCm = request.LargoCm,
                        AnchoCm = request.AnchoCm,
                        AltoCm = request.AltoCm,
                        PesoEmpaqueVacioKg = request.PesoEmpaqueVacioKg,
                        EsPredeterminado = request.EsPredeterminado,
                        Descripcion = string.Empty,
                        Activo = true
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarPaqueteProductoServicio", "No fue posible guardar el paquete.");
            }
        }

        [HttpPost("GuardarAtributoProductoServicio")]
        public async Task<IActionResult> GuardarAtributoProductoServicio([FromBody] ProductoServicioAtributoCatalogoGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                string validation = ValidateAtributoCatalogoRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validation });
                }

                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                Guid id = request.Id ?? Guid.NewGuid();
                bool esNuevo = !request.Id.HasValue || request.Id.Value == Guid.Empty;
                if (await ExisteNombreAtributoAsync(connection, transaction, context.IdEmpresa, request.Nombre.Trim(), esNuevo ? null : id))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Ya existe un atributo con el mismo nombre." });
                }

                DateTime ahora = DateTime.UtcNow;
                if (esNuevo)
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosAtributos
    (id, idEmpresa, identityKey, Nombre, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Nombre, 1, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);
                    insert.Parameters.AddWithValue("@Id", id);
                    insert.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insert.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServiciosAtributos
SET Nombre = @Nombre,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
                    update.Parameters.AddWithValue("@Id", id);
                    update.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    update.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    update.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await update.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return Ok(new ProductoServicioAtributoOperacionResponse
                {
                    Mensaje = esNuevo ? "Se registró el atributo." : "Se actualizó el atributo.",
                    Atributo = new ProductoServicioCatalogoComboDto
                    {
                        Id = id,
                        Nombre = request.Nombre.Trim(),
                        Descripcion = string.Empty,
                        Activo = true
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarAtributoProductoServicio", "No fue posible guardar el atributo.");
            }
        }

        [HttpGet("ObtenerValoresAtributoProductoServicio")]
        public async Task<IActionResult> ObtenerValoresAtributoProductoServicio(Guid idEmpresa, Guid idAtributo)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            if (idAtributo == Guid.Empty)
            {
                return Ok(new List<ProductoServicioAtributoValorDto>());
            }

            try
            {
                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, idAtributo, Valor, Orden, Activo
FROM dbo.ProductosServiciosAtributosValores
WHERE idEmpresa = @IdEmpresa AND idAtributo = @IdAtributo AND Activo = 1
ORDER BY Orden, Valor", connection);
                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@IdAtributo", idAtributo);

                List<ProductoServicioAtributoValorDto> items = new List<ProductoServicioAtributoValorDto>();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new ProductoServicioAtributoValorDto
                    {
                        Id = ReadGuid(reader, "id"),
                        IdEmpresa = ReadGuid(reader, "idEmpresa"),
                        IdAtributo = ReadGuid(reader, "idAtributo"),
                        Valor = ReadString(reader, "Valor"),
                        Orden = ReadInt(reader, "Orden"),
                        Activo = ReadBool(reader, "Activo")
                    });
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerValoresAtributoProductoServicio", "No fue posible cargar los valores del atributo.");
            }
        }

        [HttpPost("GuardarValorAtributoProductoServicio")]
        public async Task<IActionResult> GuardarValorAtributoProductoServicio([FromBody] ProductoServicioAtributoValorCatalogoGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            try
            {
                string validation = ValidateAtributoValorCatalogoRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validation });
                }

                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                if (!await ExisteAtributoActivoAsync(connection, transaction, context.IdEmpresa, request.IdAtributo))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Selecciona un atributo válido para registrar el elemento." });
                }

                Guid id = request.Id ?? Guid.NewGuid();
                bool esNuevo = !request.Id.HasValue || request.Id.Value == Guid.Empty;
                if (await ExisteValorAtributoAsync(connection, transaction, context.IdEmpresa, request.IdAtributo, request.Valor.Trim(), esNuevo ? null : id))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Ya existe un elemento con el mismo nombre dentro de este atributo." });
                }

                DateTime ahora = DateTime.UtcNow;
                if (esNuevo)
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosAtributosValores
    (id, idEmpresa, identityKey, idAtributo, Valor, Orden, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdAtributo, @Valor, @Orden, 1, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);
                    insert.Parameters.AddWithValue("@Id", id);
                    insert.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insert.Parameters.AddWithValue("@IdAtributo", request.IdAtributo);
                    insert.Parameters.AddWithValue("@Valor", request.Valor.Trim());
                    insert.Parameters.AddWithValue("@Orden", request.Orden <= 0 ? 1 : request.Orden);
                    insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServiciosAtributosValores
SET Valor = @Valor,
    Orden = @Orden,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id AND idAtributo = @IdAtributo", connection, transaction);
                    update.Parameters.AddWithValue("@Id", id);
                    update.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    update.Parameters.AddWithValue("@IdAtributo", request.IdAtributo);
                    update.Parameters.AddWithValue("@Valor", request.Valor.Trim());
                    update.Parameters.AddWithValue("@Orden", request.Orden <= 0 ? 1 : request.Orden);
                    update.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await update.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return Ok(new ProductoServicioAtributoValorOperacionResponse
                {
                    Mensaje = esNuevo ? "Se registró el elemento del atributo." : "Se actualizó el elemento del atributo.",
                    Valor = new ProductoServicioAtributoValorDto
                    {
                        Id = id,
                        IdEmpresa = context.IdEmpresa,
                        IdAtributo = request.IdAtributo,
                        Valor = request.Valor.Trim(),
                        Orden = request.Orden <= 0 ? 1 : request.Orden,
                        Activo = true
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarValorAtributoProductoServicio", "No fue posible guardar el elemento del atributo.");
            }
        }

        [HttpGet("ObtenerExistenciaProductoServicio")]
        public async Task<IActionResult> ObtenerExistenciaProductoServicio(Guid idEmpresa, Guid idProductoServicio)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();

                ProductoServicioExistenciaDto? item = await ObtenerExistenciaInternaAsync(connection, null, context.IdEmpresa, idProductoServicio);
                if (item == null)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "La existencia no está disponible para el producto solicitado." });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerExistenciaProductoServicio", "No fue posible cargar la existencia del producto.");
            }
        }

        [HttpGet("ObtenerMovimientosInventarioProductoServicio")]
        public async Task<IActionResult> ObtenerMovimientosInventarioProductoServicio(Guid idEmpresa, Guid idProductoServicio)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Read))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();

                return Ok(await ObtenerMovimientosInventarioInternoAsync(connection, context.IdEmpresa, idProductoServicio, null));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerMovimientosInventarioProductoServicio", "No fue posible cargar los movimientos del producto.");
            }
        }

        [HttpPost("RegistrarEntradaInventarioProductoServicio")]
        public async Task<IActionResult> RegistrarEntradaInventarioProductoServicio([FromBody] ProductoServicioMovimientoGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            return await RegistrarMovimientoInventarioAsync(context, request, context.IdEmpresa, MovimientoEntrada);
        }

        [HttpPost("RegistrarSalidaInventarioProductoServicio")]
        public async Task<IActionResult> RegistrarSalidaInventarioProductoServicio([FromBody] ProductoServicioMovimientoGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            return await RegistrarMovimientoInventarioAsync(context, request, context.IdEmpresa, MovimientoSalida);
        }

        [HttpPost("RegistrarAjustePositivoInventarioProductoServicio")]
        public async Task<IActionResult> RegistrarAjustePositivoInventarioProductoServicio([FromBody] ProductoServicioMovimientoGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            return await RegistrarMovimientoInventarioAsync(context, request, context.IdEmpresa, MovimientoAjustePositivo);
        }

        [HttpPost("RegistrarAjusteNegativoInventarioProductoServicio")]
        public async Task<IActionResult> RegistrarAjusteNegativoInventarioProductoServicio([FromBody] ProductoServicioMovimientoGuardarRequest request, Guid idEmpresa)
        {
            if (!await TryResolveRequestContextAsync(idEmpresa, null, out RequestContext context, out IActionResult? error, ProductosServiciosPermissionRequirement.Write))
            {
                return error!;
            }

            return await RegistrarMovimientoInventarioAsync(context, request, context.IdEmpresa, MovimientoAjusteNegativo);
        }

        private async Task<IActionResult> RegistrarMovimientoInventarioAsync(RequestContext context, ProductoServicioMovimientoGuardarRequest request, Guid effectiveEmpresaId, byte tipoMovimiento)
        {
            try
            {
                string validacion = ValidateMovimientoRequest(request, effectiveEmpresaId);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validacion });
                }

                return BadRequest(new ProductoServicioOperacionResponse { Mensaje = InventarioV1MovimientoBloqueadoMensaje });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "RegistrarMovimientoInventario", "No fue posible registrar el movimiento de inventario.");
            }
        }

        private async Task<IActionResult> CambiarEstatusProductoServicioAsync(RequestContext context, Guid effectiveEmpresaId, Guid idProductoServicio, bool activar)
        {
            try
            {
                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
UPDATE dbo.ProductosServicios
SET
    Activo = @Activo,
    FechaActualizacion = @FechaActualizacion,
    FechaArchivado = @FechaArchivado
WHERE idEmpresa = @IdEmpresa AND id = @Id AND Activo <> @Activo", connection);

                command.Parameters.AddWithValue("@IdEmpresa", effectiveEmpresaId);
                command.Parameters.AddWithValue("@Id", idProductoServicio);
                command.Parameters.AddWithValue("@Activo", activar);
                command.Parameters.AddWithValue("@FechaActualizacion", DateTime.UtcNow);
                command.Parameters.AddWithValue("@FechaArchivado", activar ? DBNull.Value : DateTime.UtcNow);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "El producto o servicio no está disponible para actualizar su estatus." });
                }

                return Ok(new ProductoServicioOperacionResponse { Mensaje = activar ? "El producto o servicio fue activado." : "El producto o servicio fue dado de baja." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "CambiarEstatusProductoServicio", "No fue posible actualizar el estatus del producto o servicio.");
            }
        }

        private async Task<List<ProductoServicioCategoriaDto>> ObtenerCategoriasListadoAsync(RequestContext context, Guid idEmpresa, string busqueda, string estatus)
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, AplicaA, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosCategorias
WHERE idEmpresa = @IdEmpresa");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            AppendBusquedaCatalogo(query, command, busqueda);
            AppendEstatusFilter(query, "Activo", estatus);
            query.Append(" ORDER BY Activo DESC, Nombre, Codigo");
            command.CommandText = query.ToString();

            List<ProductoServicioCategoriaDto> items = new List<ProductoServicioCategoriaDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCategoria(reader));
            }

            return items;
        }

        private async Task<ProductoServicioCategoriaDto?> ObtenerCategoriaAsync(RequestContext context, Guid idEmpresa, Guid idCategoria)
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();

            using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, AplicaA, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosCategorias
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idCategoria);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapCategoria(reader) : null;
        }

        private async Task<List<ProductoServicioMarcaDto>> ObtenerMarcasListadoAsync(RequestContext context, Guid idEmpresa, string busqueda, string estatus)
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosMarcas
WHERE idEmpresa = @IdEmpresa");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            AppendBusquedaCatalogo(query, command, busqueda);
            AppendEstatusFilter(query, "Activo", estatus);
            query.Append(" ORDER BY Activo DESC, Nombre, Codigo");
            command.CommandText = query.ToString();

            List<ProductoServicioMarcaDto> items = new List<ProductoServicioMarcaDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapMarca(reader));
            }

            return items;
        }

        private async Task<ProductoServicioMarcaDto?> ObtenerMarcaAsync(RequestContext context, Guid idEmpresa, Guid idMarca)
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();

            using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosMarcas
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idMarca);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapMarca(reader) : null;
        }

        private async Task<List<ProductoServicioUnidadMedidaDto>> ObtenerUnidadesListadoAsync(RequestContext context, Guid idEmpresa, string busqueda, string estatus)
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, N'' AS Descripcion, Abreviatura, PermiteDecimales, TipoUnidad, EsSistema, EsPersonalizada, FactorConversion, Convertible, ISNULL(ClaveSistema, '') AS ClaveSistema, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosUnidadesMedida
WHERE idEmpresa = @IdEmpresa");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR Abreviatura LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }
            AppendEstatusFilter(query, "Activo", estatus);
            query.Append(" ORDER BY Activo DESC, Nombre, Codigo");
            command.CommandText = query.ToString();

            List<ProductoServicioUnidadMedidaDto> items = new List<ProductoServicioUnidadMedidaDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapUnidad(reader));
            }

            return items;
        }

        private async Task<ProductoServicioUnidadMedidaDto?> ObtenerUnidadAsync(RequestContext context, Guid idEmpresa, Guid idUnidadMedida)
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();

            using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, N'' AS Descripcion, Abreviatura, PermiteDecimales, TipoUnidad, EsSistema, EsPersonalizada, FactorConversion, Convertible, ISNULL(ClaveSistema, '') AS ClaveSistema, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosUnidadesMedida
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idUnidadMedida);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapUnidad(reader) : null;
        }

        private async Task<IActionResult> GuardarCategoriaAsync(RequestContext context, ProductoServicioCategoriaGuardarRequest request, Guid idEmpresa)
        {
            return await GuardarCatalogoBasicoAsync(
                context,
                request.Id,
                idEmpresa,
                "dbo.ProductosServiciosCategorias",
                "la categoría",
                "Ya existe una categoría con este código.",
                request.Codigo,
                request.Nombre,
                request.Descripcion,
                null,
                null,
                false,
                request.AplicaA,
                duplicateNameMessage: "Ya existe una categoría con este nombre.");
        }

        private async Task<IActionResult> GuardarCatalogoBasicoAsync(
            RequestContext context,
            Guid? id,
            Guid idEmpresa,
            string tableName,
            string label,
            string duplicateCodeMessage,
            string codigo,
            string nombre,
            string descripcion,
            string? abreviatura,
            bool? permiteDecimales,
            bool includeUnidadFields,
            byte? aplicaA = null,
            string? duplicateNameMessage = null)
        {
            descripcion = NormalizeDescripcionCatalogo(descripcion);

            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();
            using SqlTransaction transaction = connection.BeginTransaction();

            Guid itemId = id ?? Guid.NewGuid();
            bool esNuevo = !id.HasValue || id.Value == Guid.Empty;
            string codigoPersistido = esNuevo
                ? await GenerateNextCatalogCodeAsync(connection, transaction, idEmpresa, tableName)
                : await ObtenerCodigoCatalogoAsync(connection, transaction, idEmpresa, itemId, tableName);

            if (string.IsNullOrWhiteSpace(codigoPersistido))
            {
                transaction.Rollback();
                return NotFound(new ProductoServicioOperacionResponse { Mensaje = $"No fue posible actualizar {label}." });
            }

            if (!string.IsNullOrWhiteSpace(duplicateNameMessage))
            {
                bool nombreDuplicado = aplicaA.HasValue
                    ? await ExisteNombreCategoriaAsync(connection, transaction, idEmpresa, nombre, aplicaA.Value, esNuevo ? null : itemId)
                    : await ExisteNombreCatalogoAsync(connection, transaction, idEmpresa, nombre, esNuevo ? null : itemId, tableName);

                if (nombreDuplicado)
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = duplicateNameMessage });
                }
            }

            DateTime ahora = DateTime.UtcNow;
            if (esNuevo)
            {
                string insertSql = includeUnidadFields
                    ? $@"
INSERT INTO {tableName}
    (id, idEmpresa, identityKey, Codigo, Nombre, Abreviatura, PermiteDecimales, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Codigo, @Nombre, @Abreviatura, @PermiteDecimales, 1, @FechaCreacion, @FechaActualizacion, NULL)"
                    : aplicaA.HasValue
                        ? $@"
INSERT INTO {tableName}
    (id, idEmpresa, identityKey, Codigo, Nombre, Descripcion, AplicaA, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Codigo, @Nombre, @Descripcion, @AplicaA, 1, @FechaCreacion, @FechaActualizacion, NULL)"
                        : $@"
INSERT INTO {tableName}
    (id, idEmpresa, identityKey, Codigo, Nombre, Descripcion, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Codigo, @Nombre, @Descripcion, 1, @FechaCreacion, @FechaActualizacion, NULL)";

                using SqlCommand insert = new SqlCommand(insertSql, connection, transaction);
                AddCatalogoParameters(insert, itemId, idEmpresa, codigoPersistido, nombre, descripcion, ahora, abreviatura, permiteDecimales, aplicaA);
                await insert.ExecuteNonQueryAsync();
            }
            else
            {
                string updateSql = includeUnidadFields
                    ? $@"
UPDATE {tableName}
SET
    Nombre = @Nombre,
    Abreviatura = @Abreviatura,
    PermiteDecimales = @PermiteDecimales,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id"
                    : aplicaA.HasValue
                        ? $@"
UPDATE {tableName}
SET
    Nombre = @Nombre,
    Descripcion = @Descripcion,
    AplicaA = @AplicaA,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id"
                        : $@"
UPDATE {tableName}
SET
    Nombre = @Nombre,
    Descripcion = @Descripcion,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id";

                using SqlCommand update = new SqlCommand(updateSql, connection, transaction);
                AddCatalogoParameters(update, itemId, idEmpresa, codigoPersistido, nombre, descripcion, ahora, abreviatura, permiteDecimales, aplicaA);
                int rowsAffected = await update.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    transaction.Rollback();
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = $"No fue posible actualizar {label}." });
                }
            }

            transaction.Commit();
            return Ok(new ProductoServicioOperacionResponse
            {
                Mensaje = esNuevo ? $"Se registró {label}." : $"Se actualizó {label}.",
                Id = itemId,
                Codigo = codigoPersistido,
                Nombre = nombre.Trim()
            });
        }

        private async Task<IActionResult> CambiarEstatusCatalogoBasicoAsync(RequestContext context, Guid idEmpresa, Guid id, string tableName, string label, bool activar)
        {
            try
            {
                using SqlConnection connection = CreateConnection(context);
                await connection.OpenAsync();

                if (tableName == "dbo.ProductosServiciosUnidadesMedida")
                {
                    using SqlCommand systemUnit = new SqlCommand("SELECT COUNT(1) FROM dbo.ProductosServiciosUnidadesMedida WHERE idEmpresa=@IdEmpresa AND id=@Id AND EsSistema=1", connection);
                    systemUnit.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    systemUnit.Parameters.AddWithValue("@Id", id);
                    if (Convert.ToInt32(await systemUnit.ExecuteScalarAsync()) > 0)
                    {
                        return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Las unidades del sistema no pueden cambiar de estatus." });
                    }
                }

                using SqlCommand command = new SqlCommand($@"
UPDATE {tableName}
SET
    Activo = @Activo,
    FechaActualizacion = @FechaActualizacion,
    FechaArchivado = @FechaArchivado
WHERE idEmpresa = @IdEmpresa AND id = @Id AND Activo <> @Activo", connection);

                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@Activo", activar);
                command.Parameters.AddWithValue("@FechaActualizacion", DateTime.UtcNow);
                command.Parameters.AddWithValue("@FechaArchivado", activar ? DBNull.Value : DateTime.UtcNow);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = $"No fue posible actualizar el estatus de {label}." });
                }

                return Ok(new ProductoServicioOperacionResponse { Mensaje = activar ? $"Se activó {label}." : $"Se dio de baja {label}." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "CambiarEstatusCatalogoBasico", "No fue posible actualizar el estatus del catálogo.");
            }
        }

        private async Task<List<ProductoServicioCatalogoComboDto>> ObtenerCategoriasComboAsync(RequestContext context, Guid idEmpresa, byte? tipo, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder(@"
SELECT id, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo, AplicaA, '' AS Abreviatura, CAST(NULL AS bit) AS PermiteDecimales
FROM dbo.ProductosServiciosCategorias
WHERE idEmpresa = @IdEmpresa AND Activo = 1");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            if (tipo.HasValue)
            {
                query.Append(" AND (AplicaA = @AplicaATodos OR AplicaA = @AplicaAEspecifico)");
                command.Parameters.AddWithValue("@AplicaATodos", AplicaATodos);
                command.Parameters.AddWithValue("@AplicaAEspecifico", tipo.Value == TipoProducto ? AplicaAProductos : AplicaAServicios);
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR ISNULL(Descripcion, '') LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY Nombre, Codigo");
            command.CommandText = query.ToString();

            List<ProductoServicioCatalogoComboDto> items = new List<ProductoServicioCatalogoComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCatalogoCombo(reader));
            }

            return items;
        }

        private async Task<List<ProductoServicioCatalogoComboDto>> ObtenerCatalogoBasicoComboAsync(RequestContext context, Guid idEmpresa, string tableName, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder($@"
SELECT id, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo, CAST(NULL AS tinyint) AS AplicaA, '' AS Abreviatura, CAST(NULL AS bit) AS PermiteDecimales
FROM {tableName}
WHERE idEmpresa = @IdEmpresa AND Activo = 1");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR ISNULL(Descripcion, '') LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY Nombre, Codigo");
            command.CommandText = query.ToString();

            List<ProductoServicioCatalogoComboDto> items = new List<ProductoServicioCatalogoComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCatalogoCombo(reader));
            }

            return items;
        }

        private async Task<List<ProductoServicioTagDto>> ObtenerTagsCatalogoAsync(RequestContext context, Guid idEmpresa, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder(@"
SELECT
    id,
    idEmpresa,
    identityKey,
    '' AS Codigo,
    Nombre,
    '' AS Descripcion,
    Activo,
    FechaCreacion,
    FechaActualizacion,
    FechaArchivado
FROM dbo.ProductosServiciosTags
WHERE idEmpresa = @IdEmpresa AND Activo = 1");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND Nombre LIKE @Busqueda");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY Nombre ASC");
            command.CommandText = query.ToString();

            List<ProductoServicioTagDto> items = new List<ProductoServicioTagDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new ProductoServicioTagDto
                {
                    Id = ReadGuid(reader, "id"),
                    IdEmpresa = ReadGuid(reader, "idEmpresa"),
                    IdentityKey = ReadGuid(reader, "identityKey"),
                    Codigo = ReadString(reader, "Codigo"),
                    Nombre = ReadString(reader, "Nombre"),
                    Descripcion = ReadString(reader, "Descripcion"),
                    Activo = ReadBool(reader, "Activo"),
                    FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                    FechaActualizacion = ReadDateTime(reader, "FechaActualizacion"),
                    FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
                });
            }

            return items;
        }

        private async Task<List<ProductoServicioTagSeleccionDto>> ObtenerTagsProductoAsync(SqlConnection connection, Guid idEmpresa, Guid idProductoServicio, string legacyTag)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT
    t.id,
    t.Nombre,
    t.Activo
FROM dbo.ProductosServiciosProductoTags pt
INNER JOIN dbo.ProductosServiciosTags t
    ON t.idEmpresa = pt.idEmpresa AND t.id = pt.idTag
WHERE pt.idEmpresa = @IdEmpresa AND pt.idProductoServicio = @IdProductoServicio
ORDER BY t.Nombre ASC", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            List<ProductoServicioTagSeleccionDto> items = new List<ProductoServicioTagSeleccionDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new ProductoServicioTagSeleccionDto
                {
                    Id = ReadGuid(reader, "id"),
                    Nombre = ReadString(reader, "Nombre"),
                    Activo = ReadBool(reader, "Activo"),
                    Legacy = false
                });
            }

            if (items.Count == 0 && !string.IsNullOrWhiteSpace(legacyTag))
            {
                items.Add(new ProductoServicioTagSeleccionDto
                {
                    Id = null,
                    Nombre = legacyTag.Trim(),
                    Activo = true,
                    Legacy = true
                });
            }

            return items;
        }

        private async Task<List<ProductoServicioCatalogoComboDto>> ObtenerUnidadesComboAsync(RequestContext context, Guid idEmpresa, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder(@"
SELECT id, Codigo, Nombre, N'' AS Descripcion, Activo, CAST(NULL AS tinyint) AS AplicaA, Abreviatura, PermiteDecimales, TipoUnidad, EsSistema, EsPersonalizada, FactorConversion, Convertible
FROM dbo.ProductosServiciosUnidadesMedida
WHERE idEmpresa = @IdEmpresa AND Activo = 1");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR Abreviatura LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY Nombre, Codigo");
            command.CommandText = query.ToString();

            List<ProductoServicioCatalogoComboDto> items = new List<ProductoServicioCatalogoComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCatalogoCombo(reader));
            }

            return items;
        }

        private async Task<List<ProductoServicioMovimientoDto>> ObtenerMovimientosInventarioInternoAsync(SqlConnection connection, Guid idEmpresa, Guid idProductoServicio, int? take)
        {
            StringBuilder query = new StringBuilder(@"
SELECT ");
            if (take.HasValue)
            {
                query.Append("TOP (@Take) ");
            }

            query.Append(@"
    id,
    idEmpresa,
    identityKey,
    idProductoServicio,
    CASE TipoMovimiento
        WHEN 1 THEN 2
        WHEN 2 THEN 3
        WHEN 3 THEN 4
        WHEN 4 THEN 5
        ELSE TipoMovimiento
    END AS TipoMovimiento,
    CantidadBase AS Cantidad,
    SaldoAnterior AS ExistenciaAnterior,
    SaldoPosterior AS ExistenciaPosterior,
    CAST(NULL AS decimal(18,4)) AS CostoUnitario,
    ISNULL(OrigenTipo, '') AS Referencia,
    ISNULL(Observaciones, '') AS Observaciones,
    idUsuario,
    FechaMovimiento
FROM dbo.InventarioMovimientos
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio
ORDER BY FechaMovimiento DESC, id DESC");

            using SqlCommand command = new SqlCommand(query.ToString(), connection);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            if (take.HasValue)
            {
                command.Parameters.AddWithValue("@Take", take.Value);
            }

            List<ProductoServicioMovimientoDto> items = new List<ProductoServicioMovimientoDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapMovimiento(reader));
            }

            return items;
        }

        private async Task<ProductoServicioSnapshot?> ObtenerProductoServicioSnapshotAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT
    ps.id,
    ps.idEmpresa,
    ps.Tipo,
    ps.Codigo,
    ps.idCategoria,
    ps.idMarca,
    ps.idUnidadMedida,
    ps.PrecioPublico,
    ps.idColeccion,
    ps.idPaquete,
    ps.EsProductoFisico,
    ps.CausaInventario,
    ps.PermiteVentaSinExistencia,
    ps.Activo,
    ISNULL(ps.ImagenUrl, '') AS ImagenUrl,
    ISNULL(ps.ImagenNombre, '') AS ImagenNombre,
    CAST(NULL AS uniqueidentifier) AS IdExistencia
FROM dbo.ProductosServicios ps
WHERE ps.idEmpresa = @IdEmpresa AND ps.id = @Id", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idProductoServicio);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new ProductoServicioSnapshot
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                Tipo = ReadByte(reader, "Tipo"),
                Codigo = ReadString(reader, "Codigo"),
                IdCategoria = ReadGuid(reader, "idCategoria"),
                IdMarca = ReadNullableGuid(reader, "idMarca"),
                IdUnidadMedida = ReadGuid(reader, "idUnidadMedida"),
                PrecioPublico = ReadDecimal(reader, "PrecioPublico"),
                IdColeccion = ReadNullableGuid(reader, "idColeccion"),
                IdPaquete = ReadNullableGuid(reader, "idPaquete"),
                EsProductoFisico = ReadBool(reader, "EsProductoFisico"),
                CausaInventario = ReadBool(reader, "CausaInventario"),
                PermiteVentaSinExistencia = ReadBool(reader, "PermiteVentaSinExistencia"),
                Activo = ReadBool(reader, "Activo"),
                ImagenUrl = ReadString(reader, "ImagenUrl"),
                ImagenNombre = ReadString(reader, "ImagenNombre"),
                IdExistencia = ReadNullableGuid(reader, "IdExistencia")
            };
        }

        private static void AddPresentacionParameters(SqlCommand command, Guid id, Guid idEmpresa, Guid productoId, ProductoServicioPresentacionVentaGuardarRequest request, bool predeterminada, DateTime ahora)
        {
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@ProductoId", productoId);
            command.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
            command.Parameters.AddWithValue("@CantidadVenta", request.CantidadVenta);
            command.Parameters.AddWithValue("@IdUnidadVenta", request.IdUnidadVenta);
            command.Parameters.AddWithValue("@EquivalenciaBase", request.EquivalenciaBase);
            command.Parameters.AddWithValue("@Precio", request.Precio);
            command.Parameters.AddWithValue("@Predeterminada", predeterminada);
            command.Parameters.AddWithValue("@Orden", request.Orden);
            command.Parameters.AddWithValue("@Ahora", ahora);
        }

        private async Task<int> ContarPresentacionesVentaActivasAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid productoId)
        {
            using SqlCommand command = new SqlCommand(@"SELECT COUNT(1) FROM dbo.ProductosServiciosPresentacionesVenta WITH (UPDLOCK, HOLDLOCK) WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @ProductoId AND Activo = 1", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@ProductoId", productoId);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        private const string UnidadBasePresentacionMensaje = "Esta unidad ya es la unidad base. La unidad base ya tiene su propia presentación. Selecciona una unidad de venta diferente.";
        private const string PresentacionDuplicadaMensaje = "Esta presentación ya existe. Ya existe una presentación con la misma cantidad, unidad y equivalencia en inventario. Edita la presentación existente si deseas cambiar su precio.";

        private static bool EsPresentacionDuplicada(ProductoServicioPresentacionVentaDto item, ProductoServicioPresentacionVentaGuardarRequest request)
            => item.IdUnidadVenta == request.IdUnidadVenta
                && item.CantidadVenta == decimal.Round(request.CantidadVenta, 4, MidpointRounding.AwayFromZero)
                && item.EquivalenciaBase == decimal.Round(request.EquivalenciaBase, 4, MidpointRounding.AwayFromZero);

        private static bool EsPresentacionBase(ProductoServicioPresentacionVentaDto item, Guid unidadId)
            => item.IdUnidadVenta == unidadId && item.CantidadVenta == 1 && item.EquivalenciaBase == 1;

        private async Task EnsureAndSynchronizeBasePresentationAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid productoId, byte tipo, Guid unidadId, decimal precioPublico, DateTime ahora)
        {
            if (tipo != TipoProducto) return;
            List<ProductoServicioPresentacionVentaDto> activas = await ObtenerPresentacionesVentaAsync(connection, transaction, idEmpresa, productoId, true);
            ProductoServicioPresentacionVentaDto? basePresentation = activas.FirstOrDefault(item => EsPresentacionBase(item, unidadId));
            // Keep the legacy flag as a derived base marker, never as a price source.
            using SqlCommand unset = new SqlCommand(@"UPDATE dbo.ProductosServiciosPresentacionesVenta SET EsPredeterminada = 0, FechaActualizacion = @Ahora WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @ProductoId AND Activo = 1 AND EsPredeterminada = 1 AND (@BaseId IS NULL OR id <> @BaseId)", connection, transaction);
            unset.Parameters.AddWithValue("@IdEmpresa", idEmpresa); unset.Parameters.AddWithValue("@ProductoId", productoId); unset.Parameters.AddWithValue("@Ahora", ahora); unset.Parameters.AddWithValue("@BaseId", (object?)basePresentation?.Id ?? DBNull.Value);
            await unset.ExecuteNonQueryAsync();
            if (basePresentation == null)
            {
                ProductoServicioUnidadMedidaDto? unidad = await ObtenerUnidadInternaAsync(connection, transaction, idEmpresa, unidadId);
                if (unidad == null) throw new ProductoServicioValidationException("La unidad base no está disponible.");
                using SqlCommand insert = new SqlCommand(@"INSERT INTO dbo.ProductosServiciosPresentacionesVenta (id, idEmpresa, identityKey, idProductoServicio, Nombre, CantidadVenta, idUnidadVenta, EquivalenciaBase, Precio, EsPredeterminada, Orden, Activo, FechaCreacion, FechaActualizacion) VALUES (NEWID(), @IdEmpresa, NEWID(), @ProductoId, @Nombre, 1, @UnidadId, 1, @Precio, 1, 0, 1, @Ahora, @Ahora)", connection, transaction);
                insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa); insert.Parameters.AddWithValue("@ProductoId", productoId); insert.Parameters.AddWithValue("@UnidadId", unidadId); insert.Parameters.AddWithValue("@Nombre", Truncate(unidad.Nombre, 100)); insert.Parameters.AddWithValue("@Precio", precioPublico); insert.Parameters.AddWithValue("@Ahora", ahora);
                await insert.ExecuteNonQueryAsync();
                return;
            }
            using SqlCommand update = new SqlCommand(@"UPDATE dbo.ProductosServiciosPresentacionesVenta SET Precio = @Precio, EsPredeterminada = 1, FechaActualizacion = @Ahora WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
            update.Parameters.AddWithValue("@Precio", precioPublico); update.Parameters.AddWithValue("@Ahora", ahora); update.Parameters.AddWithValue("@IdEmpresa", idEmpresa); update.Parameters.AddWithValue("@Id", basePresentation.Id);
            await update.ExecuteNonQueryAsync();
        }

        private async Task GuardarPresentacionesInicialesAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid productoId, Guid unidadId, List<ProductoServicioPresentacionVentaGuardarRequest> presentaciones, DateTime ahora)
        {
            ProductoServicioUnidadMedidaDto? unidad = await ObtenerUnidadInternaAsync(connection, transaction, idEmpresa, unidadId);
            if (unidad == null) throw new ProductoServicioValidationException("La unidad base no está disponible.");

            foreach (ProductoServicioPresentacionVentaGuardarRequest presentacion in presentaciones)
            {
                if (presentacion.IdUnidadVenta == unidadId) throw new ProductoServicioValidationException(UnidadBasePresentacionMensaje);
                ProductoServicioUnidadMedidaDto? unidadVenta = await ObtenerUnidadInternaAsync(connection, transaction, idEmpresa, presentacion.IdUnidadVenta);
                bool fisica = unidadVenta != null && unidad.Convertible && unidadVenta.Convertible && unidad.TipoUnidad == unidadVenta.TipoUnidad && unidad.TipoUnidad != "OTHER";
                bool manual = unidadVenta != null && unidadVenta.TipoUnidad == "OTHER";
                if (!fisica && !manual) throw new ProductoServicioValidationException("La unidad de venta no es compatible con la unidad base.");
                presentacion.Nombre = Truncate(unidadVenta!.Nombre, 100); // No manual presentation name is required.
                if (fisica) presentacion.EquivalenciaBase = decimal.Round(presentacion.CantidadVenta * unidadVenta!.FactorConversion!.Value / unidad.FactorConversion!.Value, 4, MidpointRounding.AwayFromZero);
                if (presentacion.IdUnidadVenta == unidadId) throw new ProductoServicioValidationException(UnidadBasePresentacionMensaje);
                if (!unidad.PermiteDecimales && decimal.Truncate(presentacion.EquivalenciaBase) != presentacion.EquivalenciaBase)
                {
                    throw new ProductoServicioValidationException("La equivalencia no cumple con la precisión de la unidad base.");
                }

                List<ProductoServicioPresentacionVentaDto> existentes = await ObtenerPresentacionesVentaAsync(connection, transaction, idEmpresa, productoId, true);
                if (existentes.Any(item => EsPresentacionDuplicada(item, presentacion)))
                    throw new ProductoServicioValidationException(PresentacionDuplicadaMensaje);

                presentacion.CantidadVenta = presentacion.CantidadVenta <= 0 ? 1 : presentacion.CantidadVenta;
                presentacion.IdUnidadVenta = presentacion.IdUnidadVenta == Guid.Empty ? unidadId : presentacion.IdUnidadVenta;
                using SqlCommand insert = new SqlCommand(@"INSERT INTO dbo.ProductosServiciosPresentacionesVenta (id, idEmpresa, identityKey, idProductoServicio, Nombre, CantidadVenta, idUnidadVenta, EquivalenciaBase, Precio, EsPredeterminada, Orden, Activo, FechaCreacion, FechaActualizacion) VALUES (@Id, @IdEmpresa, NEWID(), @ProductoId, @Nombre, @CantidadVenta, @IdUnidadVenta, @EquivalenciaBase, @Precio, @Predeterminada, @Orden, 1, @Ahora, @Ahora)", connection, transaction);
                AddPresentacionParameters(insert, Guid.NewGuid(), idEmpresa, productoId, presentacion, false, ahora);
                await insert.ExecuteNonQueryAsync();
            }
        }

        private async Task<List<ProductoServicioPresentacionVentaDto>> ObtenerPresentacionesVentaAsync(SqlConnection connection, SqlTransaction? transaction, Guid idEmpresa, Guid productoId, bool soloActivas)
        {
            using SqlCommand command = new SqlCommand(@"SELECT pv.id, pv.idProductoServicio, pv.Nombre, pv.CantidadVenta, pv.idUnidadVenta, uv.Nombre AS UnidadVenta, uv.Abreviatura AS UnidadVentaAbreviatura, pv.EquivalenciaBase, pv.Precio, pv.EsPredeterminada, pv.Orden, pv.Activo FROM dbo.ProductosServiciosPresentacionesVenta pv INNER JOIN dbo.ProductosServiciosUnidadesMedida uv ON uv.idEmpresa=pv.idEmpresa AND uv.id=pv.idUnidadVenta WHERE pv.idEmpresa = @IdEmpresa AND pv.idProductoServicio = @ProductoId AND (@SoloActivas = 0 OR pv.Activo = 1) ORDER BY pv.Activo DESC, pv.EsPredeterminada DESC, pv.Orden, pv.Nombre, pv.id", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa); command.Parameters.AddWithValue("@ProductoId", productoId); command.Parameters.AddWithValue("@SoloActivas", soloActivas);
            List<ProductoServicioPresentacionVentaDto> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new ProductoServicioPresentacionVentaDto { Id = ReadGuid(reader, "id"), IdProductoServicio = ReadGuid(reader, "idProductoServicio"), Nombre = ReadString(reader, "Nombre"), CantidadVenta = ReadDecimal(reader, "CantidadVenta"), IdUnidadVenta = ReadGuid(reader, "idUnidadVenta"), UnidadVenta = ReadString(reader, "UnidadVenta"), UnidadVentaAbreviatura = ReadString(reader, "UnidadVentaAbreviatura"), EquivalenciaBase = ReadDecimal(reader, "EquivalenciaBase"), Precio = ReadDecimal(reader, "Precio"), EsPredeterminada = ReadBool(reader, "EsPredeterminada"), Orden = reader.GetInt32(reader.GetOrdinal("Orden")), Activo = ReadBool(reader, "Activo") });
            }
            return result;
        }

        private async Task<List<ProductoServicioPresentacionVentaDto>> ObtenerPresentacionesVentaPorIdAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid id)
        {
            using SqlCommand command = new SqlCommand(@"SELECT id, idProductoServicio, Nombre, CantidadVenta, idUnidadVenta, EquivalenciaBase, Precio, EsPredeterminada, Orden, Activo FROM dbo.ProductosServiciosPresentacionesVenta WHERE idEmpresa = @IdEmpresa AND id = @Id AND Activo = 1", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa); command.Parameters.AddWithValue("@Id", id);
            List<ProductoServicioPresentacionVentaDto> result = new(); using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) result.Add(new ProductoServicioPresentacionVentaDto { Id = ReadGuid(reader, "id"), IdProductoServicio = ReadGuid(reader, "idProductoServicio"), Nombre = ReadString(reader, "Nombre"), CantidadVenta = ReadDecimal(reader, "CantidadVenta"), IdUnidadVenta = ReadGuid(reader, "idUnidadVenta"), EquivalenciaBase = ReadDecimal(reader, "EquivalenciaBase"), Precio = ReadDecimal(reader, "Precio"), EsPredeterminada = ReadBool(reader, "EsPredeterminada"), Orden = reader.GetInt32(reader.GetOrdinal("Orden")), Activo = ReadBool(reader, "Activo") });
            return result;
        }

        private async Task<ProductoServicioDetalleDto?> ObtenerProductoServicioParaCalculoAsync(SqlConnection connection, Guid idEmpresa, Guid productoId)
        {
            using SqlCommand command = new SqlCommand(@"SELECT ps.id, ps.Tipo, um.PermiteDecimales FROM dbo.ProductosServicios ps INNER JOIN dbo.ProductosServiciosUnidadesMedida um ON um.idEmpresa = ps.idEmpresa AND um.id = ps.idUnidadMedida WHERE ps.idEmpresa = @IdEmpresa AND ps.id = @Id", connection);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa); command.Parameters.AddWithValue("@Id", productoId);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? new ProductoServicioDetalleDto { Id = ReadGuid(reader, "id"), Tipo = ReadByte(reader, "Tipo"), UnidadPermiteDecimales = ReadBool(reader, "PermiteDecimales") } : null;
        }

        private async Task<ProductoServicioExistenciaDto?> ObtenerExistenciaInternaAsync(SqlConnection connection, SqlTransaction? transaction, Guid idEmpresa, Guid idProductoServicio, bool lockRow = false)
        {
            using SqlCommand command = new SqlCommand($@"
SELECT
    CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier) AS id,
    ps.idEmpresa,
    ps.identityKey,
    ps.id AS idProductoServicio,
    ISNULL(SUM(s.CantidadBaseActual), 0) AS ExistenciaActual,
    CAST(0 AS decimal(18,4)) AS ExistenciaMinima,
    CAST(NULL AS decimal(18,4)) AS CostoPromedio,
    COALESCE(MIN(s.FechaCreacion), SYSUTCDATETIME()) AS FechaCreacion,
    COALESCE(MAX(s.FechaActualizacion), SYSUTCDATETIME()) AS FechaActualizacion
FROM dbo.ProductosServicios ps
LEFT JOIN dbo.InventarioSaldos s
    ON s.idEmpresa = ps.idEmpresa AND s.idProductoServicio = ps.id
WHERE ps.idEmpresa = @IdEmpresa AND ps.id = @IdProductoServicio
GROUP BY ps.idEmpresa, ps.identityKey, ps.id", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new ProductoServicioExistenciaDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                ExistenciaActual = ReadDecimal(reader, "ExistenciaActual"),
                ExistenciaMinima = ReadDecimal(reader, "ExistenciaMinima"),
                CostoPromedio = ReadNullableDecimal(reader, "CostoPromedio"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadDateTime(reader, "FechaActualizacion")
            };
        }

        private async Task<int> ContarMovimientosInventarioAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.InventarioMovimientos
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        private async Task SynchronizeInventoryForSaveAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid productoId,
            Guid idEmpresa,
            NormalizedProductoServicioRequest request,
            ProductoServicioSnapshot? existente,
            ProductoServicioExistenciaDto? existenciaActual,
            int movimientosHistoricos,
            Guid? usuarioId,
            DateTime ahora)
        {
            await Task.CompletedTask;
        }

        private async Task<string> ValidateCatalogReferencesAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, NormalizedProductoServicioRequest request, bool esNuevo, ProductoServicioSnapshot? existente)
        {
            ProductoServicioCategoriaDto? categoria = await ObtenerCategoriaInternaAsync(connection, transaction, idEmpresa, request.IdCategoria);
            if (categoria == null || (esNuevo && !categoria.Activo))
            {
                return "Selecciona una categoría válida de la empresa activa.";
            }

            if (!CategoriaAplicaATipo(categoria.AplicaA, request.Tipo))
            {
                return request.Tipo == TipoProducto
                    ? "La categoría seleccionada no aplica a productos."
                    : "La categoría seleccionada no aplica a servicios.";
            }

            ProductoServicioUnidadMedidaDto? unidad = await ObtenerUnidadInternaAsync(connection, transaction, idEmpresa, request.IdUnidadMedida);
            if (unidad == null || (esNuevo && !unidad.Activo))
            {
                return "Selecciona una unidad de medida válida de la empresa activa.";
            }

            if (request.Tipo == TipoProducto && request.IdMarca.HasValue)
            {
                ProductoServicioMarcaDto? marca = await ObtenerMarcaInternaAsync(connection, transaction, idEmpresa, request.IdMarca.Value);
                if (marca == null || (esNuevo && !marca.Activo))
                {
                    return "Selecciona una marca válida de la empresa activa.";
                }
            }

            if (request.Tipo == TipoServicio && request.IdMarca.HasValue)
            {
                return "Los servicios no pueden asociarse a una marca.";
            }

            if (!esNuevo && existente != null && existente.IdEmpresa != idEmpresa)
            {
                return "El contexto de empresa no coincide con el registro solicitado.";
            }

            return string.Empty;
        }

        private async Task<ProductoServicioCategoriaDto?> ObtenerCategoriaInternaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idCategoria)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, AplicaA, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosCategorias
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idCategoria);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapCategoria(reader) : null;
        }

        private async Task<ProductoServicioMarcaDto?> ObtenerMarcaInternaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idMarca)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosMarcas
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idMarca);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapMarca(reader) : null;
        }

        private async Task<ProductoServicioUnidadMedidaDto?> ObtenerUnidadInternaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idUnidadMedida)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, N'' AS Descripcion, Abreviatura, PermiteDecimales, TipoUnidad, EsSistema, EsPersonalizada, FactorConversion, Convertible, ISNULL(ClaveSistema, '') AS ClaveSistema, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosUnidadesMedida
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idUnidadMedida);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapUnidad(reader) : null;
        }

        private async Task<bool> ExisteCodigoProductoServicioAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string codigo, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServicios
WHERE idEmpresa = @IdEmpresa AND Codigo = @Codigo AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Codigo", codigo.Trim());
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteCodigoCatalogoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string codigo, Guid? excludeId, string tableName)
        {
            using SqlCommand command = new SqlCommand($@"
SELECT COUNT(1)
FROM {tableName}
WHERE idEmpresa = @IdEmpresa AND Codigo = @Codigo AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Codigo", codigo.Trim());
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<string> GenerateNextCatalogCodeAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string tableName)
        {
            string lockResource = $"ticket05:{tableName}:{idEmpresa:D}";
            using (SqlCommand lockCommand = new SqlCommand(@"
EXEC sp_getapplock
    @Resource = @Resource,
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = 10000;", connection, transaction))
            {
                lockCommand.Parameters.AddWithValue("@Resource", lockResource);
                await lockCommand.ExecuteNonQueryAsync();
            }

            using SqlCommand command = new SqlCommand($@"
SELECT ISNULL(MAX(TRY_CONVERT(int, Codigo)), 0)
FROM {tableName}
WHERE idEmpresa = @IdEmpresa", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            int nextValue = Convert.ToInt32(await command.ExecuteScalarAsync()) + 1;
            return nextValue.ToString(nextValue < 1000 ? "D3" : "0", CultureInfo.InvariantCulture);
        }

        private async Task<string> ObtenerCodigoCatalogoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid itemId, string tableName)
        {
            using SqlCommand command = new SqlCommand($@"
SELECT Codigo
FROM {tableName}
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", itemId);
            object? result = await command.ExecuteScalarAsync();
            return Convert.ToString(result)?.Trim() ?? string.Empty;
        }

        private async Task<string> GenerateNextCollectionNumberAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa)
        {
            string lockResource = $"productos-servicios-colecciones-numero-{idEmpresa:D}";
            using (SqlCommand lockCommand = new SqlCommand(@"
EXEC sp_getapplock
    @Resource = @Resource,
    @LockMode = 'Exclusive',
    @LockOwner = 'Transaction',
    @LockTimeout = 10000;", connection, transaction))
            {
                lockCommand.Parameters.AddWithValue("@Resource", lockResource);
                await lockCommand.ExecuteNonQueryAsync();
            }

            using SqlCommand command = new SqlCommand(@"
SELECT ISNULL(MAX(TRY_CONVERT(int, Numero)), 0)
FROM dbo.ProductosServiciosColecciones
WHERE idEmpresa = @IdEmpresa", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            int nextValue = Convert.ToInt32(await command.ExecuteScalarAsync()) + 1;
            return nextValue.ToString(nextValue < 1000 ? "D3" : "0", CultureInfo.InvariantCulture);
        }

        private async Task<string> ObtenerNumeroColeccionAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid itemId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT Numero
FROM dbo.ProductosServiciosColecciones
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", itemId);
            object? result = await command.ExecuteScalarAsync();
            return Convert.ToString(result)?.Trim() ?? string.Empty;
        }

        private async Task<bool> ExisteNombreCatalogoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string nombre, Guid? excludeId, string tableName)
        {
            using SqlCommand command = new SqlCommand($@"
SELECT COUNT(1)
FROM {tableName}
WHERE idEmpresa = @IdEmpresa
  AND LTRIM(RTRIM(Nombre)) = @Nombre
  AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Nombre", nombre.Trim());
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteNombreCategoriaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string nombre, byte aplicaA, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosCategorias
WHERE idEmpresa = @IdEmpresa
  AND LTRIM(RTRIM(Nombre)) = @Nombre
  AND AplicaA = @AplicaA
  AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Nombre", nombre.Trim());
            command.Parameters.AddWithValue("@AplicaA", aplicaA);
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private static string ValidateProductoServicioRequest(ProductoServicioGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (request.Tipo != TipoProducto && request.Tipo != TipoServicio)
            {
                return "Selecciona un tipo válido: Producto o Servicio.";
            }

            if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > CodigoLength)
            {
                return $"Captura un código válido de hasta {CodigoLength} caracteres.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > NombreLength)
            {
                return $"Captura un nombre válido de hasta {NombreLength} caracteres.";
            }

            if (request.IdCategoria == Guid.Empty)
            {
                return "Selecciona una categoría.";
            }

            if (request.IdUnidadMedida == Guid.Empty)
            {
                return "Selecciona una unidad de medida.";
            }

            if ((request.Tag ?? string.Empty).Trim().Length > TagLength)
            {
                return $"La etiqueta no puede exceder {TagLength} caracteres.";
            }

            if (request.Tags != null)
            {
                foreach (ProductoServicioTagGuardarRequest tag in request.Tags)
                {
                    if ((tag?.Nombre ?? string.Empty).Trim().Length > TagLength)
                    {
                        return $"Cada etiqueta debe tener como máximo {TagLength} caracteres.";
                    }
                }
            }

            if ((request.ClaveProductoSat ?? string.Empty).Trim().Length > ClaveSatProductoLength)
            {
                return $"La clave SAT del producto no puede exceder {ClaveSatProductoLength} caracteres.";
            }

            if ((request.ClaveUnidadSat ?? string.Empty).Trim().Length > ClaveSatUnidadLength)
            {
                return $"La clave SAT de la unidad no puede exceder {ClaveSatUnidadLength} caracteres.";
            }

            if ((request.ObjetoImpuesto ?? string.Empty).Trim().Length > ObjetoImpuestoLength)
            {
                return $"El objeto de impuesto no puede exceder {ObjetoImpuestoLength} caracteres.";
            }

            string objetoImpuesto = (request.ObjetoImpuesto ?? string.Empty).Trim();
            if (objetoImpuesto == "01" && request.PorcentajeIVA != 0)
            {
                return "Cuando no aplica IVA, el porcentaje debe ser 0.";
            }

            if (objetoImpuesto == "02" && (request.PorcentajeIVA < 0 || request.PorcentajeIVA > 100))
            {
                return "El porcentaje de IVA debe estar entre 0 y 100.";
            }

            if (request.PrecioPublico < 0)
            {
                return "El precio público no puede ser negativo.";
            }

            if (request.Costo.HasValue && request.Costo.Value < 0)
            {
                return "El costo no puede ser negativo.";
            }

            if (request.PrecioComparacion.HasValue && request.PrecioComparacion.Value < 0)
            {
                return "El precio de comparación no puede ser negativo.";
            }

            if (request.PrecioComparacion.HasValue && request.PrecioComparacion.Value > 0 && request.PrecioComparacion.Value <= request.PrecioPublico)
            {
                return "El precio de comparación debe ser mayor que el precio público.";
            }

            List<ProductoServicioPresentacionVentaGuardarRequest> presentaciones = request.PresentacionesVenta ?? new List<ProductoServicioPresentacionVentaGuardarRequest>();
            if (presentaciones.Count > 0 && request.Tipo != TipoProducto)
            {
                return "Las presentaciones de venta solo aplican a productos.";
            }

            if (presentaciones.Any(p => p == null || p.CantidadVenta <= 0 || p.IdUnidadVenta == Guid.Empty || p.EquivalenciaBase <= 0 || p.Precio < 0 || p.Orden < 0))
            {
                return "Los datos de la presentación no son válidos.";
            }

            // LEGACY - REEMPLAZADO POR PRESENTACIONES DE VENTA. No validar ni usar estos campos en guardados nuevos.

            if (request.ExistenciaInicial.HasValue && request.ExistenciaInicial.Value < 0)
            {
                return "La existencia inicial no puede ser negativa.";
            }

            if (request.ExistenciaMinima.HasValue && request.ExistenciaMinima.Value < 0)
            {
                return "La existencia mínima no puede ser negativa.";
            }

            if (request.Tipo == TipoServicio && request.IdMarca.HasValue && request.IdMarca.Value != Guid.Empty)
            {
                return "Los servicios no pueden asociarse a una marca.";
            }

            if (request.Tipo == TipoProducto && request.CausaInventario == false && request.PermiteVentaSinExistencia)
            {
                return "La venta sin existencia solo aplica a productos inventariables.";
            }

            if ((request.Tipo == TipoServicio || !request.CausaInventario) &&
                HasContradictoryInventoryValues(request.ExistenciaInicial, request.ExistenciaMinima))
            {
                return "No envíes existencias para servicios o productos sin inventario.";
            }

            if (!request.EsProductoFisico &&
                (request.PesoKg.HasValue || request.LargoCm.HasValue || request.AnchoCm.HasValue || request.AltoCm.HasValue || request.IdPaquete.HasValue))
            {
                return "No envíes logística física para registros marcados como no físicos.";
            }

            if (request.PesoKg.HasValue && request.PesoKg.Value < 0)
            {
                return "El peso no puede ser negativo.";
            }

            if ((request.LargoCm.HasValue && request.LargoCm.Value < 0) ||
                (request.AnchoCm.HasValue && request.AnchoCm.Value < 0) ||
                (request.AltoCm.HasValue && request.AltoCm.Value < 0))
            {
                return "Las dimensiones no pueden ser negativas.";
            }

            if (request.Atributos.Any(a => a.IdAtributo == Guid.Empty))
            {
                return "Todos los atributos deben estar definidos.";
            }

            if (request.Tags != null)
            {
                foreach (ProductoServicioTagGuardarRequest tag in request.Tags)
                {
                    string nombreTag = (tag?.Nombre ?? string.Empty).Trim();
                    bool hasId = tag != null && tag.Id.HasValue && tag.Id.Value != Guid.Empty;
                    if (!hasId && string.IsNullOrWhiteSpace(nombreTag))
                    {
                        return "No envíes etiquetas vacías.";
                    }

                    if (nombreTag.Length > TagLength)
                    {
                        return $"Cada etiqueta debe tener como máximo {TagLength} caracteres.";
                    }
                }
            }

            if (request.Atributos.Any(a => a.Valores == null || !a.Valores.Any()))
            {
                return "Cada asociación de atributo debe incluir al menos un elemento seleccionado.";
            }

            if (request.OpcionesVariante.Any(o => string.IsNullOrWhiteSpace(o.Nombre)))
            {
                return "Todas las opciones de variante deben tener nombre.";
            }

            if (request.Variantes.Any(v => string.IsNullOrWhiteSpace(v.ClaveCombinacion)))
            {
                return "Todas las variantes deben incluir una clave de combinación.";
            }

            if (request.Variantes.Any(v => v.Costo.HasValue && v.Costo.Value < 0))
            {
                return "El costo por variante no puede ser negativo.";
            }

            return string.Empty;
        }

        private static string ValidateCategoriaRequest(ProductoServicioCategoriaGuardarRequest request, Guid idEmpresa)
        {
            string baseValidation = ValidateCatalogoBasico(idEmpresa, request.IdEmpresa, request.Nombre, request.Descripcion);
            if (!string.IsNullOrWhiteSpace(baseValidation))
            {
                return baseValidation;
            }

            if (request.AplicaA != AplicaATodos && request.AplicaA != AplicaAProductos && request.AplicaA != AplicaAServicios)
            {
                return "Selecciona un valor válido para 'Aplica a'.";
            }

            return string.Empty;
        }

        private static string ValidateMarcaRequest(ProductoServicioMarcaGuardarRequest request, Guid idEmpresa)
        {
            return ValidateCatalogoBasico(idEmpresa, request.IdEmpresa, request.Nombre, request.Descripcion);
        }

        private static string ValidateUnidadRequest(ProductoServicioUnidadMedidaGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > UnidadNombreLength)
            {
                return $"Captura un nombre válido de hasta {UnidadNombreLength} caracteres.";
            }

            if ((request.Descripcion ?? string.Empty).Trim().Length > DescripcionCatalogoLength)
            {
                return $"La descripción no puede exceder {DescripcionCatalogoLength} caracteres.";
            }

            if (string.IsNullOrWhiteSpace(request.Abreviatura) || request.Abreviatura.Trim().Length > AbreviaturaLength)
            {
                return $"Captura una abreviatura válida de hasta {AbreviaturaLength} caracteres.";
            }

            return string.Empty;
        }

        private async Task<IActionResult> GuardarUnidadControladaAsync(RequestContext context, ProductoServicioUnidadMedidaGuardarRequest request, Guid idEmpresa)
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            Guid id = request.Id.GetValueOrDefault();
            bool nueva = id == Guid.Empty;
            ProductoServicioUnidadMedidaDto? existente = nueva ? null : await ObtenerUnidadInternaAsync(connection, transaction, idEmpresa, id);
            if (!nueva && existente == null) { transaction.Rollback(); return NotFound(new ProductoServicioOperacionResponse { Mensaje = "La unidad de medida no está disponible." }); }
            if (existente?.EsSistema == true) { transaction.Rollback(); return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Las unidades del sistema no pueden modificarse." }); }
            string nombre = request.Nombre.Trim();
            string abreviatura = request.Abreviatura.Trim();
            using SqlCommand duplicate = new SqlCommand(@"SELECT TOP (1) CAST(EsSistema AS int) FROM dbo.ProductosServiciosUnidadesMedida WHERE idEmpresa=@Empresa AND (UPPER(LTRIM(RTRIM(Nombre)))=UPPER(@Nombre) OR UPPER(LTRIM(RTRIM(Abreviatura)))=UPPER(@Abreviatura)) AND (@Id IS NULL OR id<>@Id) ORDER BY EsSistema DESC", connection, transaction);
            duplicate.Parameters.AddWithValue("@Empresa", idEmpresa); duplicate.Parameters.AddWithValue("@Nombre", nombre); duplicate.Parameters.AddWithValue("@Abreviatura", abreviatura); duplicate.Parameters.AddWithValue("@Id", nueva ? DBNull.Value : id);
            object? duplicadaSistema = await duplicate.ExecuteScalarAsync();
            if (duplicadaSistema != null)
            {
                transaction.Rollback();
                return BadRequest(new ProductoServicioOperacionResponse
                {
                    Mensaje = Convert.ToInt32(duplicadaSistema) == 1
                        ? "Esta unidad ya está disponible\nLa unidad que intentas crear ya forma parte de las unidades estándar de CheckApp."
                        : "Ya existe una unidad con ese nombre o abreviatura."
                });
            }
            DateTime ahora = DateTime.UtcNow;
            if (nueva)
            {
                id = Guid.NewGuid();
                string codigo = await GenerateNextUnitCodeAsync(connection, transaction, idEmpresa);
                using SqlCommand insert = new SqlCommand(@"INSERT INTO dbo.ProductosServiciosUnidadesMedida (id,idEmpresa,identityKey,Codigo,Nombre,Abreviatura,PermiteDecimales,TipoUnidad,EsSistema,EsPersonalizada,FactorConversion,Convertible,ClaveSistema,Activo,FechaCreacion,FechaActualizacion) VALUES (@Id,@Empresa,NEWID(),@Codigo,@Nombre,@Abreviatura,@Decimales,N'OTHER',0,1,NULL,0,NULL,1,@Ahora,@Ahora)", connection, transaction);
                insert.Parameters.AddWithValue("@Id", id); insert.Parameters.AddWithValue("@Empresa", idEmpresa); insert.Parameters.AddWithValue("@Codigo", codigo); insert.Parameters.AddWithValue("@Nombre", nombre); insert.Parameters.AddWithValue("@Abreviatura", abreviatura); insert.Parameters.AddWithValue("@Decimales", request.PermiteDecimales); insert.Parameters.AddWithValue("@Ahora", ahora); await insert.ExecuteNonQueryAsync();
            }
            else
            {
                using SqlCommand update = new SqlCommand(@"UPDATE dbo.ProductosServiciosUnidadesMedida SET Nombre=@Nombre,Abreviatura=@Abreviatura,PermiteDecimales=@Decimales,FechaActualizacion=@Ahora WHERE idEmpresa=@Empresa AND id=@Id", connection, transaction);
                update.Parameters.AddWithValue("@Id", id); update.Parameters.AddWithValue("@Empresa", idEmpresa); update.Parameters.AddWithValue("@Nombre", nombre); update.Parameters.AddWithValue("@Abreviatura", abreviatura); update.Parameters.AddWithValue("@Decimales", request.PermiteDecimales); update.Parameters.AddWithValue("@Ahora", ahora); await update.ExecuteNonQueryAsync();
            }
            transaction.Commit();
            return Ok(new ProductoServicioOperacionResponse { Id = id, Mensaje = nueva ? "La unidad personalizada fue registrada." : "La unidad personalizada fue actualizada." });
        }

        private static bool EsTipoUnidadValido(string? tipo) => new[] { "WEIGHT", "VOLUME", "LENGTH", "AREA", "ITEM", "TIME", "OTHER" }.Contains((tipo ?? string.Empty).Trim().ToUpperInvariant());
        private static string GetTipoUnidadNombre(string tipo) => tipo switch { "WEIGHT" => "Peso", "VOLUME" => "Volumen", "LENGTH" => "Longitud", "AREA" => "Área", "ITEM" => "Por artículo", "TIME" => "Tiempo", _ => "Otra" };

        private static async Task<string> GenerateNextUnitCodeAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa)
        {
            using SqlCommand command = new SqlCommand(@"SELECT ISNULL(MAX(TRY_CONVERT(int, Codigo)), 0) + 1 FROM dbo.ProductosServiciosUnidadesMedida WITH (UPDLOCK,HOLDLOCK) WHERE idEmpresa=@Empresa", connection, transaction);
            command.Parameters.AddWithValue("@Empresa", idEmpresa);
            return Convert.ToInt32(await command.ExecuteScalarAsync()).ToString("D3");
        }

        private static string ValidateCatalogoBasico(Guid idEmpresaEsperado, Guid idEmpresaRequest, string nombre, string descripcion)
        {
            if (idEmpresaRequest == Guid.Empty || idEmpresaEsperado != idEmpresaRequest)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > NombreLength)
            {
                return $"Captura un nombre válido de hasta {NombreLength} caracteres.";
            }

            if ((descripcion ?? string.Empty).Trim().Length > DescripcionCatalogoLength)
            {
                return $"La descripción no puede exceder {DescripcionCatalogoLength} caracteres.";
            }

            return string.Empty;
        }

        private static string ValidateAtributoValorCatalogoRequest(ProductoServicioAtributoValorCatalogoGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (request.IdAtributo == Guid.Empty)
            {
                return "Selecciona el atributo al que pertenecerá el elemento.";
            }

            if (string.IsNullOrWhiteSpace(request.Valor) || request.Valor.Trim().Length > AtributoValorLength)
            {
                return $"Captura un elemento válido de hasta {AtributoValorLength} caracteres.";
            }

            return string.Empty;
        }

        private static string ValidateMovimientoRequest(ProductoServicioMovimientoGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (request.IdProductoServicio == Guid.Empty)
            {
                return "Selecciona un producto válido para inventario.";
            }

            if (request.Cantidad <= 0)
            {
                return "La cantidad del movimiento debe ser mayor que cero.";
            }

            if (request.CostoUnitario.HasValue && request.CostoUnitario.Value < 0)
            {
                return "El costo unitario no puede ser negativo.";
            }

            if ((request.Referencia ?? string.Empty).Trim().Length > ReferenciaLength)
            {
                return $"La referencia no puede exceder {ReferenciaLength} caracteres.";
            }

            if ((request.Observaciones ?? string.Empty).Trim().Length > ObservacionesLength)
            {
                return $"Las observaciones no pueden exceder {ObservacionesLength} caracteres.";
            }

            return string.Empty;
        }

        private static string ValidateInventoryTransition(ProductoServicioSnapshot existente, NormalizedProductoServicioRequest request, ProductoServicioExistenciaDto? existenciaActual, int movimientosHistoricos)
        {
            bool targetInventariable = request.Tipo == TipoProducto && request.CausaInventario;
            if (existente.CausaInventario && !targetInventariable)
            {
                bool tieneExistenciaDistintaDeCero = existenciaActual != null && existenciaActual.ExistenciaActual != 0m;
                if (movimientosHistoricos > 0 || tieneExistenciaDistintaDeCero)
                {
                    return "No es posible convertir a no inventariable un producto con historial o existencia distinta de cero.";
                }
            }

            return string.Empty;
        }

        private static string ValidateProductoInventariable(ProductoServicioSnapshot producto)
        {
            if (producto.Tipo != TipoProducto)
            {
                return "Los servicios no pueden generar movimientos de inventario.";
            }

            if (!producto.CausaInventario)
            {
                return "El producto seleccionado no está configurado para inventario.";
            }

            return string.Empty;
        }

        private static string ValidateMovimientoAgainstExistencia(ProductoServicioSnapshot producto, ProductoServicioExistenciaDto existencia, ProductoServicioMovimientoGuardarRequest request, byte tipoMovimiento)
        {
            decimal existenciaPosterior = CalcularExistenciaPosterior(existencia.ExistenciaActual, request.Cantidad, tipoMovimiento);
            if (existenciaPosterior < 0 && !producto.PermiteVentaSinExistencia)
            {
                return "La operación dejaría la existencia en negativo y el producto no permite venta sin existencia.";
            }

            return string.Empty;
        }

        private static string ValidateImagenTemporalUpload(IFormFile archivo)
        {
            if (archivo.Length > ImagenMaxBytes)
            {
                return "La imagen excede el tamaño máximo permitido de 10 MB.";
            }

            string extension = (Path.GetExtension(archivo.FileName) ?? string.Empty).Trim().ToLowerInvariant();
            if (!ExtensionesImagenPermitidas.Contains(extension))
            {
                return "La imagen debe estar en formato JPG, PNG o WEBP.";
            }

            if (!MimeTypesImagenPermitidos.Contains((archivo.ContentType ?? string.Empty).Trim().ToLowerInvariant()))
            {
                return "El tipo MIME de la imagen no está soportado.";
            }

            return string.Empty;
        }

        private static string ValidateImageSignature(string fileName, string contentType, byte[] fileBytes)
        {
            if (fileBytes.Length < 12)
            {
                return "La imagen cargada no contiene una firma válida.";
            }

            string extension = (Path.GetExtension(fileName) ?? string.Empty).Trim().ToLowerInvariant();
            bool isJpeg = fileBytes[0] == 0xFF && fileBytes[1] == 0xD8;
            bool isPng = fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47;
            bool isWebp = Encoding.ASCII.GetString(fileBytes, 0, 4) == "RIFF" && Encoding.ASCII.GetString(fileBytes, 8, 4) == "WEBP";

            if (extension is ".jpg" or ".jpeg")
            {
                return isJpeg ? string.Empty : "La firma del archivo no corresponde a una imagen JPG.";
            }

            if (extension == ".png")
            {
                return isPng ? string.Empty : "La firma del archivo no corresponde a una imagen PNG.";
            }

            if (extension == ".webp")
            {
                return isWebp ? string.Empty : "La firma del archivo no corresponde a una imagen WEBP.";
            }

            return $"La extensión '{extension}' no está soportada para imágenes.";
        }

        private async Task<PreparedImageOperation> PrepareImageOperationAsync(RequestContext context, Guid productoId, NormalizedProductoServicioRequest request)
        {
            if (request.EliminarImagenPrincipal)
            {
                return PreparedImageOperation.ForRemove();
            }

            if (request.ImagenPrincipal == null || string.IsNullOrWhiteSpace(request.ImagenPrincipal.TemporalToken))
            {
                return PreparedImageOperation.None();
            }

            TemporalImageTokenPayload temporal = TryParseTemporalToken(request.ImagenPrincipal.TemporalToken)
                ?? throw new InvalidOperationException("La referencia temporal de la imagen principal es inválida o expiró.");

            if (!FolderBelongsToEmpresa(temporal.FolderName, context.EmpresaStorageKey))
            {
                throw new InvalidOperationException("La imagen temporal no pertenece a la empresa activa.");
            }

            UploadedImagePayload uploaded = await MoveTemporalImageToFinalAsync(context.EmpresaStorageKey, productoId, temporal);
            return PreparedImageOperation.ForNewImage(uploaded, new FirebaseCleanupItem
            {
                FolderName = temporal.FolderName,
                StoredName = temporal.NombreAlmacenado
            });
        }

        private async Task FinalizeImageOperationAfterCommitAsync(PreparedImageOperation preparedImage, FirebaseCleanupItem? previousImageCleanup)
        {
            List<FirebaseCleanupItem> cleanupItems = new List<FirebaseCleanupItem>();
            if (preparedImage.TemporalCleanup != null)
            {
                cleanupItems.Add(preparedImage.TemporalCleanup);
            }

            if (previousImageCleanup != null)
            {
                cleanupItems.Add(previousImageCleanup);
            }

            await CleanupUploadedFirebaseFilesAsync(cleanupItems);
        }

        private async Task CompensatePreparedImageAsync(PreparedImageOperation preparedImage)
        {
            if (preparedImage.NewImageCleanup == null)
            {
                return;
            }

            try
            {
                await CleanupUploadedFirebaseFilesAsync(new List<FirebaseCleanupItem> { preparedImage.NewImageCleanup });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo la compensacion de imagen final para productos y servicios.");
            }
        }

        private static ResolvedImageMutation ResolveImageMutation(string imagenUrl, string imagenNombre, PreparedImageOperation preparedImage)
        {
            FirebaseCleanupItem? previousCleanup = null;

            if (preparedImage.Mode == ImageOperationMode.Remove)
            {
                if (!string.IsNullOrWhiteSpace(imagenUrl))
                {
                    previousCleanup = TryBuildCleanupItemFromUrl(imagenUrl);
                }

                return new ResolvedImageMutation
                {
                    ImagenUrl = string.Empty,
                    ImagenNombre = string.Empty,
                    PreviousImageCleanup = previousCleanup
                };
            }

            if (preparedImage.Mode == ImageOperationMode.NewImage && preparedImage.UploadedImage != null)
            {
                if (!string.IsNullOrWhiteSpace(imagenUrl))
                {
                    previousCleanup = TryBuildCleanupItemFromUrl(imagenUrl);
                }

                return new ResolvedImageMutation
                {
                    ImagenUrl = preparedImage.UploadedImage.UrlFirebase,
                    ImagenNombre = preparedImage.UploadedImage.NombreOriginal,
                    PreviousImageCleanup = previousCleanup
                };
            }

            return new ResolvedImageMutation
            {
                ImagenUrl = imagenUrl,
                ImagenNombre = imagenNombre
            };
        }

        private static ResolvedImageMutation ResolveImageMutation(ProductoServicioSnapshot? existente, PreparedImageOperation preparedImage)
        {
            return ResolveImageMutation(existente?.ImagenUrl ?? string.Empty, existente?.ImagenNombre ?? string.Empty, preparedImage);
        }

        private static NormalizedProductoServicioRequest NormalizeRequest(ProductoServicioGuardarRequest request)
        {
            List<ProductoServicioTagGuardarRequest> normalizedTags = (request.Tags ?? new List<ProductoServicioTagGuardarRequest>())
                .Select(tag => new ProductoServicioTagGuardarRequest
                {
                    Id = tag?.Id.HasValue == true && tag.Id.Value != Guid.Empty ? tag.Id : null,
                    Nombre = Truncate(tag?.Nombre ?? string.Empty, TagLength).Trim()
                })
                .Where(tag => tag.Id.HasValue || !string.IsNullOrWhiteSpace(tag.Nombre))
                .ToList();

            NormalizedProductoServicioRequest normalized = new NormalizedProductoServicioRequest
            {
                Id = request.Id,
                Tipo = request.Tipo,
                Codigo = request.Codigo.Trim(),
                Tag = ResolveLegacyTagShadow(normalizedTags, request.Tag),
                Nombre = request.Nombre.Trim(),
                Descripcion = SanitizeRichTextHtml(request.Descripcion),
                IdCategoria = request.IdCategoria,
                IdMarca = request.IdMarca.HasValue && request.IdMarca.Value != Guid.Empty ? request.IdMarca : null,
                IdUnidadMedida = request.IdUnidadMedida,
                IdColeccion = request.IdColeccion.HasValue && request.IdColeccion.Value != Guid.Empty ? request.IdColeccion : null,
                IdPaquete = request.IdPaquete.HasValue && request.IdPaquete.Value != Guid.Empty ? request.IdPaquete : null,
                Costo = request.Costo,
                PrecioPublico = request.PrecioPublico,
                PrecioComparacion = request.PrecioComparacion,
                // LEGACY - REEMPLAZADO POR PRESENTACIONES DE VENTA. El update conserva los valores históricos existentes.
                PrecioUnitarioMonto = null,
                PrecioUnitarioCantidadTotal = null,
                PrecioUnitarioUnidadTotal = string.Empty,
                PrecioUnitarioBaseCantidad = null,
                PrecioUnitarioUnidad = string.Empty,
                PrecioUnitarioUnidadBase = string.Empty,
                // Legacy records without fiscal data are normalized as IVA OFF on every save.
                ObjetoImpuesto = string.Equals((request.ObjetoImpuesto ?? string.Empty).Trim(), "02", StringComparison.Ordinal) ? "02" : "01",
                PorcentajeIVA = string.Equals((request.ObjetoImpuesto ?? string.Empty).Trim(), "02", StringComparison.Ordinal)
                    ? Math.Round(request.PorcentajeIVA, 2, MidpointRounding.AwayFromZero)
                    : 0,
                ClaveProductoSat = Truncate(request.ClaveProductoSat ?? string.Empty, ClaveSatProductoLength),
                ClaveUnidadSat = Truncate(request.ClaveUnidadSat ?? string.Empty, ClaveSatUnidadLength),
                EsProductoFisico = request.EsProductoFisico,
                PesoKg = request.PesoKg,
                LargoCm = request.LargoCm,
                AnchoCm = request.AnchoCm,
                AltoCm = request.AltoCm,
                UsaNumeroSerie = request.UsaNumeroSerie,
                CausaInventario = request.CausaInventario,
                PermiteVentaSinExistencia = request.PermiteVentaSinExistencia,
                ExistenciaInicial = NormalizeInventoryInput(request.ExistenciaInicial),
                ExistenciaMinima = NormalizeInventoryInput(request.ExistenciaMinima),
                Activo = request.Activo,
                ImagenPrincipal = request.ImagenPrincipal,
                EliminarImagenPrincipal = request.EliminarImagenPrincipal,
                Tags = normalizedTags,
                Atributos = request.Atributos ?? new List<ProductoServicioAtributoGuardarRequest>(),
                OpcionesVariante = request.OpcionesVariante ?? new List<ProductoServicioOpcionVarianteGuardarRequest>(),
                Variantes = request.Variantes ?? new List<ProductoServicioVarianteGuardarRequest>(),
                Multimedia = request.Multimedia ?? new List<ProductoServicioMultimediaGuardarRequest>(),
                PresentacionesVenta = (request.PresentacionesVenta ?? new List<ProductoServicioPresentacionVentaGuardarRequest>())
                    .Select(presentacion => new ProductoServicioPresentacionVentaGuardarRequest
                    {
                        Nombre = string.Empty, // Derived from the authoritative sale unit when persisted.
                        CantidadVenta = presentacion.CantidadVenta,
                        IdUnidadVenta = presentacion.IdUnidadVenta,
                        EquivalenciaBase = presentacion.EquivalenciaBase,
                        Precio = presentacion.Precio,
                        EsPredeterminada = presentacion.EsPredeterminada,
                        Orden = presentacion.Orden
                    })
                    .OrderBy(presentacion => presentacion.Orden)
                    .ToList()
            };

            if (normalized.Tipo == TipoServicio)
            {
                normalized.IdMarca = null;
                normalized.CausaInventario = false;
                normalized.PermiteVentaSinExistencia = false;
                normalized.ExistenciaInicial = null;
                normalized.ExistenciaMinima = null;
            }
            else if (!normalized.CausaInventario)
            {
                normalized.PermiteVentaSinExistencia = false;
                normalized.ExistenciaInicial = null;
                normalized.ExistenciaMinima = null;
            }

            if (!normalized.EsProductoFisico)
            {
                normalized.IdPaquete = null;
                normalized.PesoKg = null;
                normalized.LargoCm = null;
                normalized.AnchoCm = null;
                normalized.AltoCm = null;
            }

            return normalized;
        }

        private static string ResolveLegacyTagShadow(List<ProductoServicioTagGuardarRequest> tags, string? fallback)
        {
            ProductoServicioTagGuardarRequest? firstTag = (tags ?? new List<ProductoServicioTagGuardarRequest>())
                .FirstOrDefault(tag => !string.IsNullOrWhiteSpace(tag.Nombre));

            if (firstTag != null)
            {
                return firstTag.Nombre.Trim();
            }

            return (fallback ?? string.Empty).Trim();
        }

        private static void AddProductoServicioParameters(SqlCommand command, Guid productoId, Guid idEmpresa, NormalizedProductoServicioRequest request, ResolvedImageMutation imageMutation, DateTime ahora, bool includeIdentityKey)
        {
            command.Parameters.AddWithValue("@Id", productoId);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (includeIdentityKey)
            {
                command.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                command.Parameters.AddWithValue("@FechaCreacion", ahora);
            }

            command.Parameters.AddWithValue("@Tipo", request.Tipo);
            command.Parameters.AddWithValue("@Codigo", request.Codigo);
            command.Parameters.AddWithValue("@Tag", string.IsNullOrWhiteSpace(request.Tag) ? DBNull.Value : request.Tag);
            command.Parameters.AddWithValue("@Nombre", request.Nombre);
            command.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(request.Descripcion) ? DBNull.Value : request.Descripcion);
            command.Parameters.AddWithValue("@IdCategoria", request.IdCategoria);
            command.Parameters.AddWithValue("@IdMarca", request.IdMarca.HasValue ? request.IdMarca.Value : DBNull.Value);
            command.Parameters.AddWithValue("@IdUnidadMedida", request.IdUnidadMedida);
            command.Parameters.AddWithValue("@IdColeccion", request.IdColeccion.HasValue ? request.IdColeccion.Value : DBNull.Value);
            command.Parameters.AddWithValue("@IdPaquete", request.IdPaquete.HasValue ? request.IdPaquete.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Costo", request.Costo.HasValue ? request.Costo.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PrecioPublico", request.PrecioPublico);
            command.Parameters.AddWithValue("@PrecioComparacion", request.PrecioComparacion.HasValue ? request.PrecioComparacion.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PrecioUnitarioMonto", request.PrecioUnitarioMonto.HasValue ? request.PrecioUnitarioMonto.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PrecioUnitarioCantidadTotal", request.PrecioUnitarioCantidadTotal.HasValue ? request.PrecioUnitarioCantidadTotal.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PrecioUnitarioUnidadTotal", string.IsNullOrWhiteSpace(request.PrecioUnitarioUnidadTotal) ? DBNull.Value : request.PrecioUnitarioUnidadTotal);
            command.Parameters.AddWithValue("@PrecioUnitarioBaseCantidad", request.PrecioUnitarioBaseCantidad.HasValue ? request.PrecioUnitarioBaseCantidad.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PrecioUnitarioUnidad", string.IsNullOrWhiteSpace(request.PrecioUnitarioUnidad) ? DBNull.Value : request.PrecioUnitarioUnidad);
            command.Parameters.AddWithValue("@PrecioUnitarioUnidadBase", string.IsNullOrWhiteSpace(request.PrecioUnitarioUnidadBase) ? DBNull.Value : request.PrecioUnitarioUnidadBase);
            command.Parameters.AddWithValue("@ObjetoImpuesto", string.IsNullOrWhiteSpace(request.ObjetoImpuesto) ? DBNull.Value : request.ObjetoImpuesto);
            command.Parameters.AddWithValue("@PorcentajeIVA", request.PorcentajeIVA);
            command.Parameters.AddWithValue("@ClaveProductoSat", string.IsNullOrWhiteSpace(request.ClaveProductoSat) ? DBNull.Value : request.ClaveProductoSat);
            command.Parameters.AddWithValue("@ClaveUnidadSat", string.IsNullOrWhiteSpace(request.ClaveUnidadSat) ? DBNull.Value : request.ClaveUnidadSat);
            command.Parameters.AddWithValue("@EsProductoFisico", request.EsProductoFisico);
            command.Parameters.AddWithValue("@PesoKg", request.PesoKg.HasValue ? request.PesoKg.Value : DBNull.Value);
            command.Parameters.AddWithValue("@LargoCm", request.LargoCm.HasValue ? request.LargoCm.Value : DBNull.Value);
            command.Parameters.AddWithValue("@AnchoCm", request.AnchoCm.HasValue ? request.AnchoCm.Value : DBNull.Value);
            command.Parameters.AddWithValue("@AltoCm", request.AltoCm.HasValue ? request.AltoCm.Value : DBNull.Value);
            command.Parameters.AddWithValue("@UsaNumeroSerie", request.UsaNumeroSerie);
            command.Parameters.AddWithValue("@CausaInventario", request.CausaInventario);
            command.Parameters.AddWithValue("@PermiteVentaSinExistencia", request.PermiteVentaSinExistencia);
            command.Parameters.AddWithValue("@ImagenUrl", string.IsNullOrWhiteSpace(imageMutation.ImagenUrl) ? DBNull.Value : imageMutation.ImagenUrl);
            command.Parameters.AddWithValue("@ImagenNombre", string.IsNullOrWhiteSpace(imageMutation.ImagenNombre) ? DBNull.Value : imageMutation.ImagenNombre);
            command.Parameters.AddWithValue("@Activo", request.Activo);
            command.Parameters.AddWithValue("@FechaActualizacion", ahora);
        }

        private static void AddCatalogoParameters(SqlCommand command, Guid id, Guid idEmpresa, string codigo, string nombre, string descripcion, DateTime ahora, string? abreviatura, bool? permiteDecimales, byte? aplicaA)
        {
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
            command.Parameters.AddWithValue("@Codigo", codigo.Trim());
            command.Parameters.AddWithValue("@Nombre", nombre.Trim());
            command.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(descripcion) ? DBNull.Value : descripcion.Trim());
            command.Parameters.AddWithValue("@FechaCreacion", ahora);
            command.Parameters.AddWithValue("@FechaActualizacion", ahora);

            if (abreviatura != null)
            {
                command.Parameters.AddWithValue("@Abreviatura", abreviatura.Trim());
            }

            if (permiteDecimales.HasValue)
            {
                command.Parameters.AddWithValue("@PermiteDecimales", permiteDecimales.Value);
            }

            if (aplicaA.HasValue)
            {
                command.Parameters.AddWithValue("@AplicaA", aplicaA.Value);
            }
        }

        private static void AddPaqueteParameters(SqlCommand command, Guid id, Guid idEmpresa, ProductoServicioPaqueteGuardarRequest request, DateTime ahora, bool includeIdentityKey)
        {
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (includeIdentityKey)
            {
                command.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                command.Parameters.AddWithValue("@FechaCreacion", ahora);
            }

            command.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
            command.Parameters.AddWithValue("@TipoPaquete", request.TipoPaquete.Trim().ToLowerInvariant());
            command.Parameters.AddWithValue("@LargoCm", request.LargoCm.HasValue ? request.LargoCm.Value : DBNull.Value);
            command.Parameters.AddWithValue("@AnchoCm", request.AnchoCm.HasValue ? request.AnchoCm.Value : DBNull.Value);
            command.Parameters.AddWithValue("@AltoCm", request.AltoCm.HasValue ? request.AltoCm.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PesoEmpaqueVacioKg", request.PesoEmpaqueVacioKg.HasValue ? request.PesoEmpaqueVacioKg.Value : DBNull.Value);
            command.Parameters.AddWithValue("@EsPredeterminado", request.EsPredeterminado);
            command.Parameters.AddWithValue("@FechaActualizacion", ahora);
        }

        private async Task<List<ProductoServicioCatalogoComboDto>> ObtenerColeccionesComboAsync(RequestContext context, Guid idEmpresa, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();
            StringBuilder query = new StringBuilder(@"
SELECT id, Numero AS Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo, CAST(NULL AS tinyint) AS AplicaA, '' AS Abreviatura, CAST(NULL AS bit) AS PermiteDecimales, Numero, '' AS TipoPaquete
FROM dbo.ProductosServiciosColecciones
WHERE idEmpresa = @IdEmpresa AND Activo = 1");
            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Numero LIKE @Busqueda OR Nombre LIKE @Busqueda OR ISNULL(Descripcion, '') LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY Nombre, Numero");
            command.CommandText = query.ToString();
            List<ProductoServicioCatalogoComboDto> items = new List<ProductoServicioCatalogoComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCatalogoCombo(reader));
            }

            return items;
        }

        private async Task<List<ProductoServicioCatalogoComboDto>> ObtenerPaquetesComboAsync(RequestContext context, Guid idEmpresa, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();
            StringBuilder query = new StringBuilder(@"
SELECT id, Nombre AS Codigo, Nombre, N'' AS Descripcion, Activo, CAST(NULL AS tinyint) AS AplicaA, '' AS Abreviatura, CAST(NULL AS bit) AS PermiteDecimales, '' AS Numero, TipoPaquete,
       LargoCm, AnchoCm, AltoCm, PesoEmpaqueVacioKg, EsPredeterminado
FROM dbo.ProductosServiciosPaquetes
WHERE idEmpresa = @IdEmpresa AND Activo = 1");
            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND Nombre LIKE @Busqueda");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY EsPredeterminado DESC, Nombre");
            command.CommandText = query.ToString();
            List<ProductoServicioCatalogoComboDto> items = new List<ProductoServicioCatalogoComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCatalogoCombo(reader));
            }

            return items;
        }

        private async Task<List<ProductoServicioCatalogoComboDto>> ObtenerAtributosComboAsync(RequestContext context, Guid idEmpresa, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection(context);
            await connection.OpenAsync();
            StringBuilder query = new StringBuilder(@"
SELECT id, Nombre AS Codigo, Nombre, N'' AS Descripcion, Activo, CAST(NULL AS tinyint) AS AplicaA, '' AS Abreviatura, CAST(NULL AS bit) AS PermiteDecimales, '' AS Numero, '' AS TipoPaquete
FROM dbo.ProductosServiciosAtributos
WHERE idEmpresa = @IdEmpresa AND Activo = 1");
            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND Nombre LIKE @Busqueda");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY Nombre");
            command.CommandText = query.ToString();
            List<ProductoServicioCatalogoComboDto> items = new List<ProductoServicioCatalogoComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCatalogoCombo(reader));
            }

            return items;
        }

        private async Task<bool> ExisteNumeroColeccionAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string numero, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosColecciones
WHERE idEmpresa = @IdEmpresa AND Numero = @Numero AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Numero", numero);
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteNombreColeccionAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string nombre, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosColecciones
WHERE idEmpresa = @IdEmpresa
  AND LTRIM(RTRIM(Nombre)) = @Nombre
  AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Nombre", nombre);
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteNombreAtributoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string nombre, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosAtributos
WHERE idEmpresa = @IdEmpresa AND Nombre = @Nombre AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Nombre", nombre);
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteAtributoActivoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idAtributo)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosAtributos
WHERE idEmpresa = @IdEmpresa AND id = @Id AND Activo = 1", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idAtributo);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteValorAtributoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idAtributo, string valor, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosAtributosValores
WHERE idEmpresa = @IdEmpresa
  AND idAtributo = @IdAtributo
  AND Valor = @Valor
  AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdAtributo", idAtributo);
            command.Parameters.AddWithValue("@Valor", valor);
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExistePaqueteMismoNombreTipoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string nombre, string tipoPaquete, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosPaquetes
WHERE idEmpresa = @IdEmpresa
  AND Nombre = @Nombre
  AND TipoPaquete = @TipoPaquete
  AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Nombre", nombre);
            command.Parameters.AddWithValue("@TipoPaquete", tipoPaquete.Trim().ToLowerInvariant());
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private static string ValidateColeccionRequest(ProductoServicioColeccionGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (!string.IsNullOrWhiteSpace(request.Numero) && request.Numero.Trim().Length > NumeroColeccionLength)
            {
                return $"El número de colección no puede exceder {NumeroColeccionLength} caracteres.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > NombreLength)
            {
                return $"Captura un nombre válido de hasta {NombreLength} caracteres.";
            }

            if ((request.Descripcion ?? string.Empty).Trim().Length > DescripcionCatalogoLength)
            {
                return $"La descripción no puede exceder {DescripcionCatalogoLength} caracteres.";
            }

            return string.Empty;
        }

        private static string ValidatePaqueteRequest(ProductoServicioPaqueteGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > NombreLength)
            {
                return $"Captura un nombre válido de hasta {NombreLength} caracteres.";
            }

            string tipo = (request.TipoPaquete ?? string.Empty).Trim().ToLowerInvariant();
            if (tipo != "caja" && tipo != "sobre" && tipo != "flexible")
            {
                return "Selecciona un tipo de paquete válido.";
            }

            if ((request.LargoCm.HasValue && request.LargoCm.Value < 0) ||
                (request.AnchoCm.HasValue && request.AnchoCm.Value < 0) ||
                (request.AltoCm.HasValue && request.AltoCm.Value < 0) ||
                (request.PesoEmpaqueVacioKg.HasValue && request.PesoEmpaqueVacioKg.Value < 0))
            {
                return "Las medidas y el peso del paquete no pueden ser negativos.";
            }

            return string.Empty;
        }

        private static string ValidateAtributoCatalogoRequest(ProductoServicioAtributoCatalogoGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > AtributoNombreLength)
            {
                return $"Captura un atributo válido de hasta {AtributoNombreLength} caracteres.";
            }

            return string.Empty;
        }

        private async Task<List<ProductoServicioOpcionDto>> BuscarClavesProductoSatAsync(string[] terms, int top)
        {
            string baseUrl = GetSatCatalogosApiBaseUrl();
            return await GetSatCatalogOptionsFromExternalApiByTermsAsync(
                baseUrl,
                "GetClaveProdServ4",
                terms,
                new[] { "ClaveProdServ", "c_ClaveProdServ", "claveprodserv", "clave" },
                new[] { "Descripción", "descripcion", "Nombre", "nombre", "desc" },
                top);
        }

        private async Task<List<ProductoServicioOpcionDto>> BuscarClavesUnidadSatAsync(string q, int top)
        {
            string baseUrl = GetSatCatalogosApiBaseUrl();
            List<ProductoServicioOpcionDto> items = await GetSatCatalogOptionsFromExternalApiAsync(
                baseUrl,
                "GetTodoClaveUnidad",
                null,
                new[] { "ClaveUnidad", "Nombre", "c_ClaveUnidad", "claveunidad", "clave" },
                new[] { "Valor", "Descripción", "descripcion", "desc", "Nombre" },
                Math.Max(top * 3, 120));

            if (!items.Any(item => string.Equals(item.Clave, "H87", StringComparison.OrdinalIgnoreCase)))
            {
                items.Insert(0, new ProductoServicioOpcionDto
                {
                    Clave = "H87",
                    Nombre = "H87 - Pieza"
                });
            }

            string filtro = (q ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                items = items
                    .Where(item => item.Clave.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                        || item.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return items
                .GroupBy(item => item.Clave, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Take(top)
                .ToList();
        }

        private string GetSatCatalogosApiBaseUrl()
        {
            string raw = _configuration["SatCatalogosApi:BaseUrl"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = "http://checkapp-001-site10.htempurl.com";
            }

            return raw.Trim().TrimEnd('/');
        }

        private static string[] ParseSatTerms(string q)
        {
            return (q ?? string.Empty)
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(fragment => fragment.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Select(fragment => fragment.Trim())
                .Where(fragment => !string.IsNullOrWhiteSpace(fragment))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToArray();
        }

        private static async Task<List<ProductoServicioOpcionDto>> GetSatCatalogOptionsFromExternalApiByTermsAsync(
            string baseUrl,
            string route,
            string[] terms,
            string[] keyCandidates,
            string[] descCandidates,
            int top)
        {
            List<ProductoServicioOpcionDto> merged = new List<ProductoServicioOpcionDto>();
            if (terms == null || terms.Length == 0)
            {
                return merged;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string term in terms)
            {
                List<ProductoServicioOpcionDto> partial = await GetSatCatalogOptionsFromExternalApiAsync(baseUrl, route, term, keyCandidates, descCandidates, top);
                foreach (ProductoServicioOpcionDto item in partial)
                {
                    if (string.IsNullOrWhiteSpace(item.Clave) || !seen.Add(item.Clave))
                    {
                        continue;
                    }

                    merged.Add(item);
                    if (merged.Count >= top)
                    {
                        return merged;
                    }
                }
            }

            return merged;
        }

        private static async Task<List<ProductoServicioOpcionDto>> GetSatCatalogOptionsFromExternalApiAsync(
            string baseUrl,
            string route,
            string? termino,
            string[] keyCandidates,
            string[] descCandidates,
            int top)
        {
            List<ProductoServicioOpcionDto> list = new List<ProductoServicioOpcionDto>();
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(route))
            {
                return list;
            }

            string safeRoute = route.Trim().Trim('/');
            string url = string.IsNullOrWhiteSpace(termino)
                ? $"{baseUrl}/api/Catalogos/{safeRoute}"
                : $"{baseUrl}/api/Catalogos/{safeRoute}/{Uri.EscapeDataString(termino.Trim())}";

            using HttpClient http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            using HttpResponseMessage response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return list;
                }

                string detail = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"SAT API {safeRoute} respondió {(int)response.StatusCode}: {detail}");
            }

            string json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                return list;
            }

            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JsonElement row in document.RootElement.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                string clave = GetJsonValueByCandidates(row, keyCandidates);
                if (string.IsNullOrWhiteSpace(clave) || !seen.Add(clave))
                {
                    continue;
                }

                string descripcion = GetJsonValueByCandidates(row, descCandidates);
                list.Add(new ProductoServicioOpcionDto
                {
                    Clave = clave,
                    Nombre = string.IsNullOrWhiteSpace(descripcion) ? clave : $"{clave} - {descripcion}"
                });

                if (list.Count >= top)
                {
                    break;
                }
            }

            return list;
        }

        private static string GetJsonValueByCandidates(JsonElement obj, string[] candidates)
        {
            if (obj.ValueKind != JsonValueKind.Object || candidates == null || candidates.Length == 0)
            {
                return string.Empty;
            }

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate) || !TryGetPropertyIgnoreCase(obj, candidate, out JsonElement value))
                {
                    continue;
                }

                string text = JsonElementToString(value);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }

            return string.Empty;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
        {
            if (obj.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in obj.EnumerateObject())
                {
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static string JsonElementToString(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                _ => string.Empty
            };
        }

        private async Task<UploadedImagePayload> UploadImageToFirebaseAsync(string folderName, string storedName, byte[] fileBytes, string nombreOriginal, string mimeType, long pesoBytes)
        {
            string extension = NormalizeExtension(Path.GetExtension(nombreOriginal), mimeType);
            var config = new FirebaseAuthConfig
            {
                ApiKey = _configuration.GetValue<string>("fireBdata:fireApiKey"),
                AuthDomain = _configuration.GetValue<string>("fireBdata:fireAuthDomain"),
                Providers = new FirebaseAuthProvider[] { new EmailProvider() }
            };

            var authClient = new FirebaseAuthClient(config);
            var userCredential = await authClient.SignInWithEmailAndPasswordAsync(
                _configuration.GetValue<string>("fireBdata:fireUser"),
                _configuration.GetValue<string>("fireBdata:fireClave"));
            string token = await userCredential.User.GetIdTokenAsync();

            using MemoryStream stream = new MemoryStream(fileBytes);
            var storage = new FirebaseStorage(
                _configuration.GetValue<string>("fireBdata:fireStorage"),
                new FirebaseStorageOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(token),
                    ThrowOnCancel = true
                });

            string downloadUrl = await storage.Child(folderName).Child(storedName).PutAsync(stream);
            authClient.SignOut();

            return new UploadedImagePayload
            {
                FolderName = folderName,
                NombreOriginal = NormalizeArchivoText(nombreOriginal, NombreArchivoLength, storedName),
                NombreAlmacenado = storedName,
                Extension = extension,
                MimeType = NormalizeArchivoText(mimeType, MimeTypeLength, "image/jpeg"),
                UrlFirebase = NormalizeArchivoText(downloadUrl, UrlLength, string.Empty),
                PesoBytes = pesoBytes > 0 ? pesoBytes : fileBytes.LongLength
            };
        }

        private async Task<UploadedImagePayload> MoveTemporalImageToFinalAsync(string empresaStorageKey, Guid productoId, TemporalImageTokenPayload temporal)
        {
            byte[] fileBytes = await DownloadFirebaseFileAsync(temporal.FolderName, temporal.NombreAlmacenado);
            return await UploadImageToFirebaseAsync(
                BuildFinalFolderName(empresaStorageKey, productoId),
                BuildStoredFileName(temporal.NombreOriginal, temporal.MimeType),
                fileBytes,
                temporal.NombreOriginal,
                temporal.MimeType,
                temporal.PesoBytes);
        }

        private async Task<UploadedImagePayload> MoveTemporalImageToFinalAsync(string empresaStorageKey, Guid productoId, TemporalImageTokenPayload temporal, string finalFolderName)
        {
            byte[] fileBytes = await DownloadFirebaseFileAsync(temporal.FolderName, temporal.NombreAlmacenado);
            return await UploadImageToFirebaseAsync(
                finalFolderName,
                BuildStoredFileName(temporal.NombreOriginal, temporal.MimeType),
                fileBytes,
                temporal.NombreOriginal,
                temporal.MimeType,
                temporal.PesoBytes);
        }

        private async Task<byte[]> DownloadFirebaseFileAsync(string folderName, string storedName)
        {
            var config = new FirebaseAuthConfig
            {
                ApiKey = _configuration.GetValue<string>("fireBdata:fireApiKey"),
                AuthDomain = _configuration.GetValue<string>("fireBdata:fireAuthDomain"),
                Providers = new FirebaseAuthProvider[] { new EmailProvider() }
            };

            var authClient = new FirebaseAuthClient(config);
            var userCredential = await authClient.SignInWithEmailAndPasswordAsync(
                _configuration.GetValue<string>("fireBdata:fireUser"),
                _configuration.GetValue<string>("fireBdata:fireClave"));
            string token = await userCredential.User.GetIdTokenAsync();

            var storage = new FirebaseStorage(
                _configuration.GetValue<string>("fireBdata:fireStorage"),
                new FirebaseStorageOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(token),
                    ThrowOnCancel = true
                });

            string url = await storage.Child(folderName).Child(storedName).GetDownloadUrlAsync();
            using HttpClient httpClient = new HttpClient();
            byte[] fileBytes = await httpClient.GetByteArrayAsync(url);
            authClient.SignOut();
            return fileBytes;
        }

        private async Task CleanupUploadedFirebaseFilesAsync(List<FirebaseCleanupItem> items)
        {
            List<FirebaseCleanupItem> filesToDelete = items
                .Where(item => !string.IsNullOrWhiteSpace(item.FolderName) && !string.IsNullOrWhiteSpace(item.StoredName))
                .GroupBy(item => $"{item.FolderName}/{item.StoredName}")
                .Select(group => group.First())
                .ToList();

            if (filesToDelete.Count == 0)
            {
                return;
            }

            try
            {
                var config = new FirebaseAuthConfig
                {
                    ApiKey = _configuration.GetValue<string>("fireBdata:fireApiKey"),
                    AuthDomain = _configuration.GetValue<string>("fireBdata:fireAuthDomain"),
                    Providers = new FirebaseAuthProvider[] { new EmailProvider() }
                };

                var authClient = new FirebaseAuthClient(config);
                var userCredential = await authClient.SignInWithEmailAndPasswordAsync(
                    _configuration.GetValue<string>("fireBdata:fireUser"),
                    _configuration.GetValue<string>("fireBdata:fireClave"));
                string token = await userCredential.User.GetIdTokenAsync();

                var storage = new FirebaseStorage(
                    _configuration.GetValue<string>("fireBdata:fireStorage"),
                    new FirebaseStorageOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(token),
                        ThrowOnCancel = true
                    });

                foreach (FirebaseCleanupItem item in filesToDelete)
                {
                    await storage.Child(item.FolderName).Child(item.StoredName).DeleteAsync();
                }

                authClient.SignOut();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo la limpieza de archivos en Firebase para productos y servicios.");
            }
        }

        private async Task<List<ProductoServicioAtributoSeleccionDto>> ObtenerAtributosProductoAsync(SqlConnection connection, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT
    ppa.id,
    ppa.idAtributo,
    a.Nombre,
    ppa.Orden,
    av.id AS idAtributoValor,
    av.Valor,
    pav.Orden AS OrdenValor
FROM dbo.ProductosServiciosProductoAtributos ppa
INNER JOIN dbo.ProductosServiciosAtributos a
    ON a.idEmpresa = ppa.idEmpresa AND a.id = ppa.idAtributo
LEFT JOIN dbo.ProductosServiciosProductoAtributoValores pav
    ON pav.idEmpresa = ppa.idEmpresa AND pav.idProductoAtributo = ppa.id AND pav.Activo = 1
LEFT JOIN dbo.ProductosServiciosAtributosValores av
    ON av.idEmpresa = pav.idEmpresa AND av.id = pav.idAtributoValor
WHERE ppa.idEmpresa = @IdEmpresa AND ppa.idProductoServicio = @IdProductoServicio AND ppa.Activo = 1
ORDER BY ppa.Orden, a.Nombre, pav.Orden, av.Valor", connection);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            Dictionary<Guid, ProductoServicioAtributoSeleccionDto> lookup = new Dictionary<Guid, ProductoServicioAtributoSeleccionDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Guid idProductoAtributo = ReadGuid(reader, "id");
                if (!lookup.TryGetValue(idProductoAtributo, out ProductoServicioAtributoSeleccionDto? current))
                {
                    current = new ProductoServicioAtributoSeleccionDto
                    {
                        IdProductoAtributo = idProductoAtributo,
                        IdAtributo = ReadGuid(reader, "idAtributo"),
                        Nombre = ReadString(reader, "Nombre"),
                        Orden = ReadInt(reader, "Orden")
                    };
                    lookup[idProductoAtributo] = current;
                }

                Guid idAtributoValor = ReadGuid(reader, "idAtributoValor");
                if (idAtributoValor != Guid.Empty)
                {
                    current.Valores.Add(new ProductoServicioAtributoValorSeleccionDto
                    {
                        IdAtributoValor = idAtributoValor,
                        Valor = ReadString(reader, "Valor"),
                        Orden = ReadInt(reader, "OrdenValor")
                    });
                }
            }

            return lookup.Values.OrderBy(x => x.Orden).ToList();
        }

        private async Task<List<ProductoServicioOpcionVarianteDto>> ObtenerOpcionesVarianteProductoAsync(SqlConnection connection, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT
    ov.id,
    ov.idProductoServicio,
    ov.Nombre,
    ov.Orden,
    ov.Activo,
    ovv.id AS idOpcionVarianteValor,
    ovv.Valor,
    ovv.Orden AS OrdenValor,
    ovv.Activo AS ValorActivo
FROM dbo.ProductosServiciosOpcionesVariante ov
LEFT JOIN dbo.ProductosServiciosOpcionesVarianteValores ovv
    ON ovv.idEmpresa = ov.idEmpresa AND ovv.idOpcionVariante = ov.id AND ovv.Activo = 1
WHERE ov.idEmpresa = @IdEmpresa AND ov.idProductoServicio = @IdProductoServicio AND ov.Activo = 1
ORDER BY ov.Orden, ov.Nombre, ovv.Orden, ovv.Valor", connection);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            Dictionary<Guid, ProductoServicioOpcionVarianteDto> lookup = new Dictionary<Guid, ProductoServicioOpcionVarianteDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Guid idOpcion = ReadGuid(reader, "id");
                if (!lookup.TryGetValue(idOpcion, out ProductoServicioOpcionVarianteDto? current))
                {
                    current = new ProductoServicioOpcionVarianteDto
                    {
                        Id = idOpcion,
                        IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                        Nombre = ReadString(reader, "Nombre"),
                        Orden = ReadInt(reader, "Orden"),
                        Activo = ReadBool(reader, "Activo")
                    };
                    lookup[idOpcion] = current;
                }

                Guid idValor = ReadGuid(reader, "idOpcionVarianteValor");
                if (idValor != Guid.Empty)
                {
                    current.Valores.Add(new ProductoServicioOpcionVarianteValorDto
                    {
                        Id = idValor,
                        IdOpcionVariante = idOpcion,
                        Valor = ReadString(reader, "Valor"),
                        Orden = ReadInt(reader, "OrdenValor"),
                        Activo = ReadBool(reader, "ValorActivo")
                    });
                }
            }

            return lookup.Values.OrderBy(x => x.Orden).ToList();
        }

        private async Task<List<ProductoServicioVarianteDto>> ObtenerVariantesProductoAsync(SqlConnection connection, Guid idEmpresa, Guid idProductoServicio, SqlTransaction? transaction = null)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT
    pv.id,
    pv.idProductoServicio,
    ISNULL(pv.Sku, '') AS Sku,
    pv.Nombre,
    pv.ClaveCombinacion,
    ISNULL(pv.ImagenUrl, '') AS ImagenUrl,
    ISNULL(pv.ImagenNombre, '') AS ImagenNombre,
    pv.Costo,
    pv.PrecioPublico,
    pv.PrecioComparacion,
    pv.PrecioUnitarioMonto,
    pv.PrecioUnitarioBaseCantidad,
    ISNULL(pv.PrecioUnitarioUnidad, '') AS PrecioUnitarioUnidad,
    pv.Orden,
    pv.Activo,
    vv.idOpcionVariante,
    ov.Nombre AS Opcion,
    vv.idOpcionVarianteValor,
    av.Valor,
    vv.Orden AS OrdenValor
FROM dbo.ProductosServiciosVariantes pv
LEFT JOIN dbo.ProductosServiciosVarianteValores vv
    ON vv.idEmpresa = pv.idEmpresa AND vv.idVariante = pv.id
LEFT JOIN dbo.ProductosServiciosOpcionesVariante ov
    ON ov.idEmpresa = vv.idEmpresa AND ov.id = vv.idOpcionVariante
LEFT JOIN dbo.ProductosServiciosOpcionesVarianteValores av
    ON av.idEmpresa = vv.idEmpresa AND av.id = vv.idOpcionVarianteValor
WHERE pv.idEmpresa = @IdEmpresa AND pv.idProductoServicio = @IdProductoServicio AND pv.Activo = 1
ORDER BY pv.Orden, pv.Nombre, vv.Orden", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            Dictionary<Guid, ProductoServicioVarianteDto> lookup = new Dictionary<Guid, ProductoServicioVarianteDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Guid idVariante = ReadGuid(reader, "id");
                if (!lookup.TryGetValue(idVariante, out ProductoServicioVarianteDto? current))
                {
                    current = new ProductoServicioVarianteDto
                    {
                        Id = idVariante,
                        IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                        Sku = ReadString(reader, "Sku"),
                        Nombre = ReadString(reader, "Nombre"),
                        ClaveCombinacion = ReadString(reader, "ClaveCombinacion"),
                        ImagenUrl = ReadString(reader, "ImagenUrl"),
                        ImagenNombre = ReadString(reader, "ImagenNombre"),
                        Costo = ReadNullableDecimal(reader, "Costo"),
                        PrecioPublico = ReadNullableDecimal(reader, "PrecioPublico"),
                        PrecioComparacion = ReadNullableDecimal(reader, "PrecioComparacion"),
                        PrecioUnitarioMonto = ReadNullableDecimal(reader, "PrecioUnitarioMonto"),
                        PrecioUnitarioBaseCantidad = ReadNullableDecimal(reader, "PrecioUnitarioBaseCantidad"),
                        PrecioUnitarioUnidad = ReadString(reader, "PrecioUnitarioUnidad"),
                        Orden = ReadInt(reader, "Orden"),
                        Activo = ReadBool(reader, "Activo")
                    };
                    lookup[idVariante] = current;
                }

                Guid idOpcionVariante = ReadGuid(reader, "idOpcionVariante");
                Guid idOpcionVarianteValor = ReadGuid(reader, "idOpcionVarianteValor");
                if (idOpcionVariante != Guid.Empty && idOpcionVarianteValor != Guid.Empty)
                {
                    current.Valores.Add(new ProductoServicioVarianteValorDto
                    {
                        IdOpcionVariante = idOpcionVariante,
                        Opcion = ReadString(reader, "Opcion"),
                        IdOpcionVarianteValor = idOpcionVarianteValor,
                        Valor = ReadString(reader, "Valor"),
                        Orden = ReadInt(reader, "OrdenValor")
                    });
                }
            }

            return lookup.Values.OrderBy(x => x.Orden).ToList();
        }

        private async Task<List<ProductoServicioMultimediaDto>> ObtenerMultimediaProductoAsync(SqlConnection connection, Guid idEmpresa, Guid idProductoServicio, SqlTransaction? transaction = null)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, idProductoServicio, TipoMultimedia, Foto, Video, Documento, NombreOriginal, NombreAlmacenado, Extension, MimeType, UrlFirebase, PesoBytes, Orden, Activo, FechaCreacion, FechaActualizacion
FROM dbo.ProductosServiciosMultimedia
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio AND Activo = 1
ORDER BY CASE WHEN Foto = 1 THEN 1 WHEN Video = 1 THEN 2 ELSE 3 END, Orden, FechaCreacion", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            List<ProductoServicioMultimediaDto> items = new List<ProductoServicioMultimediaDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new ProductoServicioMultimediaDto
                {
                    Id = ReadGuid(reader, "id"),
                    IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                    TipoMultimedia = ReadString(reader, "TipoMultimedia"),
                    Foto = ReadBool(reader, "Foto"),
                    Video = ReadBool(reader, "Video"),
                    Documento = ReadBool(reader, "Documento"),
                    NombreOriginal = ReadString(reader, "NombreOriginal"),
                    NombreAlmacenado = ReadString(reader, "NombreAlmacenado"),
                    Extension = ReadString(reader, "Extension"),
                    MimeType = ReadString(reader, "MimeType"),
                    UrlFirebase = ReadString(reader, "UrlFirebase"),
                    PesoBytes = ReadLong(reader, "PesoBytes"),
                    Orden = ReadInt(reader, "Orden"),
                    Activo = ReadBool(reader, "Activo"),
                    FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                    FechaActualizacion = ReadDateTime(reader, "FechaActualizacion")
                });
            }

            return items;
        }

        private async Task<PreparedMultimediaOperation> PrepareMultimediaOperationAsync(RequestContext context, Guid productoId, NormalizedProductoServicioRequest request)
        {
            PreparedMultimediaOperation operation = new PreparedMultimediaOperation();
            foreach (ProductoServicioMultimediaGuardarRequest item in request.Multimedia ?? new List<ProductoServicioMultimediaGuardarRequest>())
            {
                string tipo = NormalizeTipoMultimedia(item.TipoMultimedia);
                if (string.IsNullOrWhiteSpace(tipo))
                {
                    continue;
                }

                if (item.Id.HasValue && item.Id.Value != Guid.Empty)
                {
                    operation.ExistingItemIds.Add(item.Id.Value);
                    operation.FinalItems.Add(new ProductoServicioMultimediaDto
                    {
                        Id = item.Id.Value,
                        IdProductoServicio = productoId,
                        TipoMultimedia = tipo,
                        Foto = tipo == "foto",
                        Video = tipo == "video",
                        Documento = tipo == "documento",
                        NombreOriginal = item.NombreOriginal,
                        NombreAlmacenado = item.NombreAlmacenado,
                        Extension = item.Extension,
                        MimeType = item.MimeType,
                        UrlFirebase = item.UrlFirebase,
                        PesoBytes = item.PesoBytes,
                        Orden = item.Orden,
                        Activo = true,
                        FechaCreacion = DateTime.UtcNow,
                        FechaActualizacion = DateTime.UtcNow
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.TemporalToken))
                {
                    continue;
                }

                TemporalImageTokenPayload temporal = TryParseTemporalToken(item.TemporalToken)
                    ?? throw new InvalidOperationException("Se detectó una referencia temporal inválida.");
                if (!FolderBelongsToEmpresa(temporal.FolderName, context.EmpresaStorageKey))
                {
                    throw new InvalidOperationException("La evidencia temporal no pertenece a la empresa activa.");
                }

                UploadedImagePayload uploaded = await MoveTemporalImageToFinalAsync(
                    context.EmpresaStorageKey,
                    productoId,
                    temporal,
                    BuildFinalMultimediaFolderName(context.EmpresaStorageKey, productoId, tipo));

                operation.FinalItems.Add(new ProductoServicioMultimediaDto
                {
                    Id = Guid.NewGuid(),
                    IdProductoServicio = productoId,
                    TipoMultimedia = tipo,
                    Foto = tipo == "foto",
                    Video = tipo == "video",
                    Documento = tipo == "documento",
                    NombreOriginal = uploaded.NombreOriginal,
                    NombreAlmacenado = uploaded.NombreAlmacenado,
                    Extension = uploaded.Extension,
                    MimeType = uploaded.MimeType,
                    UrlFirebase = uploaded.UrlFirebase,
                    PesoBytes = uploaded.PesoBytes,
                    Orden = item.Orden,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow
                });

                operation.TemporalCleanups.Add(new FirebaseCleanupItem
                {
                    FolderName = temporal.FolderName,
                    StoredName = temporal.NombreAlmacenado
                });

                operation.NewFileCompensations.Add(new FirebaseCleanupItem
                {
                    FolderName = uploaded.FolderName,
                    StoredName = uploaded.NombreAlmacenado
                });
            }

            return operation;
        }

        private async Task FinalizeMultimediaOperationAfterCommitAsync(PreparedMultimediaOperation operation)
        {
            await CleanupUploadedFirebaseFilesAsync(operation.TemporalCleanups);
        }

        private async Task CompensatePreparedMultimediaAsync(PreparedMultimediaOperation operation)
        {
            await CleanupUploadedFirebaseFilesAsync(operation.NewFileCompensations);
        }

        private async Task SynchronizeProductoMultimediaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio, PreparedMultimediaOperation operation, DateTime ahora)
        {
            List<ProductoServicioMultimediaDto> actual = await ObtenerMultimediaProductoAsync(connection, idEmpresa, idProductoServicio, transaction);
            HashSet<Guid> actualIds = actual.Select(x => x.Id).ToHashSet();
            if (operation.ExistingItemIds.Any(id => !actualIds.Contains(id)))
            {
                throw new ProductoServicioValidationException("Se detectó una evidencia multimedia inválida para el producto.");
            }

            List<ProductoServicioMultimediaDto> multimediaFinal = operation.FinalItems;
            HashSet<Guid> finalIds = multimediaFinal.Select(x => x.Id).ToHashSet();
            if (finalIds.Count == 0)
            {
                using SqlCommand deactivateAll = new SqlCommand(@"
UPDATE dbo.ProductosServiciosMultimedia
SET Activo = 0, FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);
                deactivateAll.Parameters.AddWithValue("@FechaActualizacion", ahora);
                deactivateAll.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                deactivateAll.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                await deactivateAll.ExecuteNonQueryAsync();
            }
            else
            {
                using SqlCommand deactivate = new SqlCommand(@"
UPDATE dbo.ProductosServiciosMultimedia
SET Activo = 0, FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio AND id NOT IN (SELECT TRY_CONVERT(uniqueidentifier, value) FROM STRING_SPLIT(@IdsCsv, ',') WHERE TRY_CONVERT(uniqueidentifier, value) IS NOT NULL)", connection, transaction);
                deactivate.Parameters.AddWithValue("@FechaActualizacion", ahora);
                deactivate.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                deactivate.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                deactivate.Parameters.AddWithValue("@IdsCsv", string.Join(",", finalIds.Select(x => x.ToString())));
                await deactivate.ExecuteNonQueryAsync();
            }

            foreach (ProductoServicioMultimediaDto item in multimediaFinal)
            {
                bool exists = actual.Any(x => x.Id == item.Id);
                if (exists)
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServiciosMultimedia
SET Orden = @Orden, Activo = 1, FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
                    update.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    update.Parameters.AddWithValue("@Id", item.Id);
                    update.Parameters.AddWithValue("@Orden", item.Orden);
                    update.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await update.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosMultimedia
    (id, idEmpresa, identityKey, idProductoServicio, TipoMultimedia, Foto, Video, Documento, NombreOriginal, NombreAlmacenado, Extension, MimeType, UrlFirebase, PesoBytes, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdProductoServicio, @TipoMultimedia, @Foto, @Video, @Documento, @NombreOriginal, @NombreAlmacenado, @Extension, @MimeType, @UrlFirebase, @PesoBytes, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);
                    insert.Parameters.AddWithValue("@Id", item.Id);
                    insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insert.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                    insert.Parameters.AddWithValue("@TipoMultimedia", item.TipoMultimedia);
                    insert.Parameters.AddWithValue("@Foto", item.Foto);
                    insert.Parameters.AddWithValue("@Video", item.Video);
                    insert.Parameters.AddWithValue("@Documento", item.Documento);
                    insert.Parameters.AddWithValue("@NombreOriginal", item.NombreOriginal);
                    insert.Parameters.AddWithValue("@NombreAlmacenado", item.NombreAlmacenado);
                    insert.Parameters.AddWithValue("@Extension", item.Extension);
                    insert.Parameters.AddWithValue("@MimeType", item.MimeType);
                    insert.Parameters.AddWithValue("@UrlFirebase", item.UrlFirebase);
                    insert.Parameters.AddWithValue("@PesoBytes", item.PesoBytes);
                    insert.Parameters.AddWithValue("@Orden", item.Orden);
                    insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insert.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task<ProductoServicioTagDto> ResolveOrCreateTagAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string nombre, DateTime ahora)
        {
            string normalizedName = Truncate(nombre ?? string.Empty, TagLength).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new ProductoServicioValidationException("Captura un nombre de etiqueta.");
            }

            using SqlCommand find = new SqlCommand(@"
SELECT TOP (1)
    id,
    idEmpresa,
    identityKey,
    '' AS Codigo,
    Nombre,
    '' AS Descripcion,
    Activo,
    FechaCreacion,
    FechaActualizacion,
    FechaArchivado
FROM dbo.ProductosServiciosTags
WHERE idEmpresa = @IdEmpresa
  AND UPPER(LTRIM(RTRIM(Nombre))) = UPPER(@NombreNormalizado)
ORDER BY Activo DESC, FechaActualizacion DESC, FechaCreacion DESC, id DESC", connection, transaction);

            find.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            find.Parameters.AddWithValue("@NombreNormalizado", normalizedName);

            using SqlDataReader reader = await find.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                ProductoServicioTagDto existing = new ProductoServicioTagDto
                {
                    Id = ReadGuid(reader, "id"),
                    IdEmpresa = ReadGuid(reader, "idEmpresa"),
                    IdentityKey = ReadGuid(reader, "identityKey"),
                    Codigo = ReadString(reader, "Codigo"),
                    Nombre = ReadString(reader, "Nombre"),
                    Descripcion = ReadString(reader, "Descripcion"),
                    Activo = ReadBool(reader, "Activo"),
                    FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                    FechaActualizacion = ReadDateTime(reader, "FechaActualizacion"),
                    FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
                };

                reader.Close();

                if (!existing.Activo)
                {
                    await ReactivateTagAsync(connection, transaction, idEmpresa, existing.Id, ahora);
                    existing.Activo = true;
                    existing.FechaActualizacion = ahora;
                    existing.FechaArchivado = null;
                }

                return existing;
            }

            reader.Close();

            ProductoServicioTagDto created = new ProductoServicioTagDto
            {
                Id = Guid.NewGuid(),
                IdEmpresa = idEmpresa,
                IdentityKey = Guid.NewGuid(),
                Nombre = normalizedName,
                Activo = true,
                FechaCreacion = ahora,
                FechaActualizacion = ahora
            };

            using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosTags
    (id, idEmpresa, identityKey, Nombre, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Nombre, 1, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);

            insert.Parameters.AddWithValue("@Id", created.Id);
            insert.Parameters.AddWithValue("@IdEmpresa", created.IdEmpresa);
            insert.Parameters.AddWithValue("@IdentityKey", created.IdentityKey);
            insert.Parameters.AddWithValue("@Nombre", created.Nombre);
            insert.Parameters.AddWithValue("@FechaCreacion", created.FechaCreacion);
            insert.Parameters.AddWithValue("@FechaActualizacion", created.FechaActualizacion);
            await insert.ExecuteNonQueryAsync();

            return created;
        }

        private async Task ReactivateTagAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idTag, DateTime ahora)
        {
            using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServiciosTags
SET
    Activo = 1,
    FechaActualizacion = @FechaActualizacion,
    FechaArchivado = NULL
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);

            update.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            update.Parameters.AddWithValue("@Id", idTag);
            update.Parameters.AddWithValue("@FechaActualizacion", ahora);
            await update.ExecuteNonQueryAsync();
        }

        private async Task SynchronizeProductoTagsAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio, List<ProductoServicioTagGuardarRequest> tags, DateTime ahora)
        {
            List<ProductoServicioTagGuardarRequest> requestedTags = tags ?? new List<ProductoServicioTagGuardarRequest>();
            Dictionary<Guid, ProductoServicioTagDto> resolvedTags = new Dictionary<Guid, ProductoServicioTagDto>();

            foreach (ProductoServicioTagGuardarRequest requestedTag in requestedTags)
            {
                ProductoServicioTagDto resolved;
                if (requestedTag.Id.HasValue && requestedTag.Id.Value != Guid.Empty)
                {
                    using SqlCommand findById = new SqlCommand(@"
SELECT TOP (1)
    id,
    idEmpresa,
    identityKey,
    '' AS Codigo,
    Nombre,
    '' AS Descripcion,
    Activo,
    FechaCreacion,
    FechaActualizacion,
    FechaArchivado
FROM dbo.ProductosServiciosTags
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);

                    findById.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    findById.Parameters.AddWithValue("@Id", requestedTag.Id.Value);

                    using SqlDataReader reader = await findById.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        throw new ProductoServicioValidationException("Una de las etiquetas seleccionadas ya no está disponible.");
                    }

                    resolved = new ProductoServicioTagDto
                    {
                        Id = ReadGuid(reader, "id"),
                        IdEmpresa = ReadGuid(reader, "idEmpresa"),
                        IdentityKey = ReadGuid(reader, "identityKey"),
                        Codigo = ReadString(reader, "Codigo"),
                        Nombre = ReadString(reader, "Nombre"),
                        Descripcion = ReadString(reader, "Descripcion"),
                        Activo = ReadBool(reader, "Activo"),
                        FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                        FechaActualizacion = ReadDateTime(reader, "FechaActualizacion"),
                        FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
                    };

                    reader.Close();

                    if (!resolved.Activo)
                    {
                        await ReactivateTagAsync(connection, transaction, idEmpresa, resolved.Id, ahora);
                        resolved.Activo = true;
                        resolved.FechaActualizacion = ahora;
                        resolved.FechaArchivado = null;
                    }
                }
                else
                {
                    resolved = await ResolveOrCreateTagAsync(connection, transaction, idEmpresa, requestedTag.Nombre, ahora);
                }

                resolvedTags[resolved.Id] = resolved;
            }

            using SqlCommand delete = new SqlCommand(@"
DELETE FROM dbo.ProductosServiciosProductoTags
WHERE idEmpresa = @IdEmpresa
  AND idProductoServicio = @IdProductoServicio", connection, transaction);

            delete.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            delete.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await delete.ExecuteNonQueryAsync();

            foreach (Guid idTag in resolvedTags.Keys)
            {
                using SqlCommand insert = new SqlCommand(@"
IF NOT EXISTS (
    SELECT 1
    FROM dbo.ProductosServiciosProductoTags
    WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio AND idTag = @IdTag
)
BEGIN
    INSERT INTO dbo.ProductosServiciosProductoTags
        (id, idEmpresa, identityKey, idProductoServicio, idTag, FechaCreacion)
    VALUES
        (@Id, @IdEmpresa, @IdentityKey, @IdProductoServicio, @IdTag, @FechaCreacion)
END", connection, transaction);

                insert.Parameters.AddWithValue("@Id", Guid.NewGuid());
                insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                insert.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                insert.Parameters.AddWithValue("@IdTag", idTag);
                insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                await insert.ExecuteNonQueryAsync();
            }
        }

        private async Task SynchronizeProductoAtributosAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio, List<ProductoServicioAtributoGuardarRequest> atributos, DateTime ahora)
        {
            await ValidateProductoAtributoRequestIdsAsync(connection, transaction, idEmpresa, idProductoServicio, atributos);

            using SqlCommand deleteValores = new SqlCommand(@"
DELETE pav
FROM dbo.ProductosServiciosProductoAtributoValores pav
INNER JOIN dbo.ProductosServiciosProductoAtributos ppa ON ppa.idEmpresa = pav.idEmpresa AND ppa.id = pav.idProductoAtributo
WHERE pav.idEmpresa = @IdEmpresa AND ppa.idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteValores.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            deleteValores.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteValores.ExecuteNonQueryAsync();

            using SqlCommand deleteAtributos = new SqlCommand(@"
DELETE FROM dbo.ProductosServiciosProductoAtributos
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteAtributos.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            deleteAtributos.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteAtributos.ExecuteNonQueryAsync();

            // One product/attribute relation owns all its selected elements, including
            // elements submitted separately by older clients after reopening a product.
            var grupos = atributos.OrderBy(x => x.Orden).GroupBy(x => x.IdAtributo);
            foreach (var grupo in grupos)
            {
                ProductoServicioAtributoGuardarRequest atributo = grupo.First();
                Guid idProductoAtributo = Guid.NewGuid();
                using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosProductoAtributos
    (id, idEmpresa, identityKey, idProductoServicio, idAtributo, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdProductoServicio, @IdAtributo, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);
                insert.Parameters.AddWithValue("@Id", idProductoAtributo);
                insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                insert.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                insert.Parameters.AddWithValue("@IdAtributo", atributo.IdAtributo);
                insert.Parameters.AddWithValue("@Orden", atributo.Orden);
                insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                await insert.ExecuteNonQueryAsync();

                var valoresInsertados = new HashSet<Guid>();
                int ordenValor = 0;
                foreach (ProductoServicioAtributoValorGuardarRequest valor in grupo.SelectMany(x => x.Valores.OrderBy(v => v.Orden)))
                {
                    Guid idValor = await EnsureAtributoValorAsync(connection, transaction, idEmpresa, atributo.IdAtributo, valor, ahora);
                    if (!valoresInsertados.Add(idValor))
                    {
                        throw new ProductoServicioValidationException("Esa relación atributo / elemento ya fue agregada al producto.");
                    }
                    using SqlCommand insertValor = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosProductoAtributoValores
    (id, idEmpresa, identityKey, idProductoAtributo, idAtributoValor, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdProductoAtributo, @IdAtributoValor, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);
                    insertValor.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    insertValor.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    insertValor.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insertValor.Parameters.AddWithValue("@IdProductoAtributo", idProductoAtributo);
                    insertValor.Parameters.AddWithValue("@IdAtributoValor", idValor);
                    insertValor.Parameters.AddWithValue("@Orden", ++ordenValor);
                    insertValor.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insertValor.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insertValor.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task<Dictionary<string, VariantOptionReference>> SynchronizeProductoOpcionesVarianteAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio, List<ProductoServicioOpcionVarianteGuardarRequest> opciones, DateTime ahora)
        {
            await ValidateProductoOpcionVarianteRequestIdsAsync(connection, transaction, idEmpresa, idProductoServicio, opciones);

            using SqlCommand deleteVariantValues = new SqlCommand(@"
DELETE vv
FROM dbo.ProductosServiciosVarianteValores vv
INNER JOIN dbo.ProductosServiciosVariantes pv ON pv.idEmpresa = vv.idEmpresa AND pv.id = vv.idVariante
WHERE vv.idEmpresa = @IdEmpresa AND pv.idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteVariantValues.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            deleteVariantValues.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteVariantValues.ExecuteNonQueryAsync();

            using SqlCommand deleteValores = new SqlCommand(@"
DELETE ovv
FROM dbo.ProductosServiciosOpcionesVarianteValores ovv
INNER JOIN dbo.ProductosServiciosOpcionesVariante ov ON ov.idEmpresa = ovv.idEmpresa AND ov.id = ovv.idOpcionVariante
WHERE ovv.idEmpresa = @IdEmpresa AND ov.idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteValores.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            deleteValores.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteValores.ExecuteNonQueryAsync();

            using SqlCommand deleteOpciones = new SqlCommand(@"
DELETE FROM dbo.ProductosServiciosOpcionesVariante
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteOpciones.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            deleteOpciones.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteOpciones.ExecuteNonQueryAsync();

            Dictionary<string, VariantOptionReference> references = new Dictionary<string, VariantOptionReference>(StringComparer.OrdinalIgnoreCase);
            foreach (ProductoServicioOpcionVarianteGuardarRequest opcion in opciones.OrderBy(x => x.Orden))
            {
                Guid idOpcion = opcion.Id ?? Guid.NewGuid();
                using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosOpcionesVariante
    (id, idEmpresa, identityKey, idProductoServicio, Nombre, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdProductoServicio, @Nombre, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);
                insert.Parameters.AddWithValue("@Id", idOpcion);
                insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                insert.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                insert.Parameters.AddWithValue("@Nombre", opcion.Nombre.Trim());
                insert.Parameters.AddWithValue("@Orden", opcion.Orden);
                insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                await insert.ExecuteNonQueryAsync();

                VariantOptionReference current = new VariantOptionReference { Id = idOpcion, Nombre = opcion.Nombre.Trim() };
                foreach (ProductoServicioOpcionVarianteValorGuardarRequest valor in opcion.Valores.OrderBy(x => x.Orden))
                {
                    Guid idValor = valor.Id ?? Guid.NewGuid();
                    using SqlCommand insertValor = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosOpcionesVarianteValores
    (id, idEmpresa, identityKey, idOpcionVariante, Valor, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdOpcionVariante, @Valor, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);
                    insertValor.Parameters.AddWithValue("@Id", idValor);
                    insertValor.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    insertValor.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insertValor.Parameters.AddWithValue("@IdOpcionVariante", idOpcion);
                    insertValor.Parameters.AddWithValue("@Valor", valor.Valor.Trim());
                    insertValor.Parameters.AddWithValue("@Orden", valor.Orden);
                    insertValor.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insertValor.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insertValor.ExecuteNonQueryAsync();

                    current.Valores[NormalizeCatalogKey(valor.Valor)] = idValor;
                }

                references[NormalizeCatalogKey(opcion.Nombre)] = current;
            }

            return references;
        }

        private async Task<PreparedVariantSyncResult> SynchronizeProductoVariantesAsync(SqlConnection connection, SqlTransaction transaction, RequestContext context, Guid idProductoServicio, List<ProductoServicioVarianteGuardarRequest> variantes, Dictionary<string, VariantOptionReference> optionReferences, DateTime ahora)
        {
            List<ProductoServicioVarianteDto> variantesActuales = await ObtenerVariantesProductoAsync(connection, context.IdEmpresa, idProductoServicio, transaction);
            Dictionary<Guid, ProductoServicioVarianteDto> variantesActualesPorId = variantesActuales.ToDictionary(x => x.Id);
            Dictionary<string, ProductoServicioVarianteDto> variantesActualesPorClave = variantesActuales
                .Where(x => !string.IsNullOrWhiteSpace(x.ClaveCombinacion))
                .GroupBy(x => x.ClaveCombinacion, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            PreparedVariantSyncResult result = new PreparedVariantSyncResult();
            foreach (Guid requestedVariantId in variantes.Where(x => x.Id.HasValue && x.Id.Value != Guid.Empty).Select(x => x.Id!.Value))
            {
                if (!variantesActualesPorId.ContainsKey(requestedVariantId))
                {
                    throw new ProductoServicioValidationException("Se detectó una variante inválida para el producto.");
                }
            }

            using SqlCommand deleteValores = new SqlCommand(@"
DELETE vv
FROM dbo.ProductosServiciosVarianteValores vv
INNER JOIN dbo.ProductosServiciosVariantes pv ON pv.idEmpresa = vv.idEmpresa AND pv.id = vv.idVariante
WHERE vv.idEmpresa = @IdEmpresa AND pv.idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteValores.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
            deleteValores.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteValores.ExecuteNonQueryAsync();

            using SqlCommand deleteVariantes = new SqlCommand(@"
DELETE FROM dbo.ProductosServiciosVariantes
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteVariantes.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
            deleteVariantes.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteVariantes.ExecuteNonQueryAsync();

            HashSet<Guid> finalVariantIds = new HashSet<Guid>();
            foreach (ProductoServicioVarianteGuardarRequest variante in variantes.OrderBy(x => x.Orden))
            {
                ProductoServicioVarianteDto? existente = null;
                if (variante.Id.HasValue && variante.Id.Value != Guid.Empty)
                {
                    variantesActualesPorId.TryGetValue(variante.Id.Value, out existente);
                }

                if (existente == null && !string.IsNullOrWhiteSpace(variante.ClaveCombinacion))
                {
                    variantesActualesPorClave.TryGetValue(variante.ClaveCombinacion.Trim(), out existente);
                }

                Guid idVariante = variante.Id.HasValue && variante.Id.Value != Guid.Empty
                    ? variante.Id.Value
                    : existente?.Id ?? Guid.NewGuid();
                finalVariantIds.Add(idVariante);

                PreparedImageOperation preparedImage = await PrepareVariantImageOperationAsync(context, idProductoServicio, idVariante, variante);
                ResolvedImageMutation imageMutation = ResolveImageMutation(existente?.ImagenUrl ?? string.Empty, existente?.ImagenNombre ?? string.Empty, preparedImage);

                if (preparedImage.TemporalCleanup != null)
                {
                    result.TemporalCleanups.Add(preparedImage.TemporalCleanup);
                }

                if (preparedImage.NewImageCleanup != null)
                {
                    result.NewFileCompensations.Add(preparedImage.NewImageCleanup);
                }

                if (imageMutation.PreviousImageCleanup != null)
                {
                    result.FinalCleanups.Add(imageMutation.PreviousImageCleanup);
                }

                using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosVariantes
    (id, idEmpresa, identityKey, idProductoServicio, Sku, Nombre, ClaveCombinacion, ImagenUrl, ImagenNombre, Costo, PrecioPublico, PrecioComparacion, PrecioUnitarioMonto, PrecioUnitarioBaseCantidad, PrecioUnitarioUnidad, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdProductoServicio, @Sku, @Nombre, @ClaveCombinacion, @ImagenUrl, @ImagenNombre, @Costo, @PrecioPublico, @PrecioComparacion, @PrecioUnitarioMonto, @PrecioUnitarioBaseCantidad, @PrecioUnitarioUnidad, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);
                insert.Parameters.AddWithValue("@Id", idVariante);
                insert.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                insert.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                insert.Parameters.AddWithValue("@Sku", string.IsNullOrWhiteSpace(variante.Sku) ? DBNull.Value : variante.Sku.Trim());
                insert.Parameters.AddWithValue("@Nombre", variante.Nombre.Trim());
                insert.Parameters.AddWithValue("@ClaveCombinacion", variante.ClaveCombinacion.Trim());
                insert.Parameters.AddWithValue("@ImagenUrl", string.IsNullOrWhiteSpace(imageMutation.ImagenUrl) ? DBNull.Value : imageMutation.ImagenUrl);
                insert.Parameters.AddWithValue("@ImagenNombre", string.IsNullOrWhiteSpace(imageMutation.ImagenNombre) ? DBNull.Value : imageMutation.ImagenNombre);
                insert.Parameters.AddWithValue("@Costo", variante.Costo.HasValue ? variante.Costo.Value : DBNull.Value);
                insert.Parameters.AddWithValue("@PrecioPublico", variante.PrecioPublico.HasValue ? variante.PrecioPublico.Value : DBNull.Value);
                insert.Parameters.AddWithValue("@PrecioComparacion", variante.PrecioComparacion.HasValue ? variante.PrecioComparacion.Value : DBNull.Value);
                insert.Parameters.AddWithValue("@PrecioUnitarioMonto", variante.PrecioUnitarioMonto.HasValue ? variante.PrecioUnitarioMonto.Value : DBNull.Value);
                insert.Parameters.AddWithValue("@PrecioUnitarioBaseCantidad", variante.PrecioUnitarioBaseCantidad.HasValue ? variante.PrecioUnitarioBaseCantidad.Value : DBNull.Value);
                insert.Parameters.AddWithValue("@PrecioUnitarioUnidad", string.IsNullOrWhiteSpace(variante.PrecioUnitarioUnidad) ? DBNull.Value : variante.PrecioUnitarioUnidad.Trim());
                insert.Parameters.AddWithValue("@Orden", variante.Orden);
                insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                await insert.ExecuteNonQueryAsync();

                foreach (ProductoServicioVarianteValorGuardarRequest valor in variante.Valores.OrderBy(x => x.Orden))
                {
                    VariantOptionReference option = ResolveVariantOptionReference(optionReferences, valor.IdOpcionVariante ?? Guid.Empty, valor.Opcion);
                    Guid idValor = ResolveVariantOptionValueId(option, valor.IdOpcionVarianteValor, valor.Valor);

                    using SqlCommand insertValor = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosVarianteValores
    (id, idEmpresa, identityKey, idVariante, idOpcionVariante, idOpcionVarianteValor, Orden, FechaCreacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdVariante, @IdOpcionVariante, @IdOpcionVarianteValor, @Orden, @FechaCreacion)", connection, transaction);
                    insertValor.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    insertValor.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    insertValor.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insertValor.Parameters.AddWithValue("@IdVariante", idVariante);
                    insertValor.Parameters.AddWithValue("@IdOpcionVariante", option.Id);
                    insertValor.Parameters.AddWithValue("@IdOpcionVarianteValor", idValor);
                    insertValor.Parameters.AddWithValue("@Orden", valor.Orden);
                    insertValor.Parameters.AddWithValue("@FechaCreacion", ahora);
                    await insertValor.ExecuteNonQueryAsync();
                }
            }

            foreach (ProductoServicioVarianteDto varianteEliminada in variantesActuales.Where(x => !finalVariantIds.Contains(x.Id)))
            {
                FirebaseCleanupItem? cleanup = TryBuildCleanupItemFromUrl(varianteEliminada.ImagenUrl);
                if (cleanup != null)
                {
                    result.FinalCleanups.Add(cleanup);
                }
            }

            return result;
        }

        private async Task<PreparedImageOperation> PrepareVariantImageOperationAsync(RequestContext context, Guid productoId, Guid varianteId, ProductoServicioVarianteGuardarRequest request)
        {
            if (request.EliminarImagen)
            {
                return PreparedImageOperation.ForRemove();
            }

            if (request.Imagen == null || string.IsNullOrWhiteSpace(request.Imagen.TemporalToken))
            {
                return PreparedImageOperation.None();
            }

            TemporalImageTokenPayload temporal = TryParseTemporalToken(request.Imagen.TemporalToken)
                ?? throw new InvalidOperationException("La referencia temporal de la imagen de la variante es inválida o expiró.");

            if (!FolderBelongsToEmpresa(temporal.FolderName, context.EmpresaStorageKey))
            {
                throw new InvalidOperationException("La imagen temporal de la variante no pertenece a la empresa activa.");
            }

            UploadedImagePayload uploaded = await MoveTemporalImageToFinalAsync(
                context.EmpresaStorageKey,
                productoId,
                temporal,
                BuildFinalVariantImageFolderName(context.EmpresaStorageKey, productoId, varianteId));

            return PreparedImageOperation.ForNewImage(uploaded, new FirebaseCleanupItem
            {
                FolderName = temporal.FolderName,
                StoredName = temporal.NombreAlmacenado
            });
        }

        private async Task FinalizeVariantSyncAfterCommitAsync(PreparedVariantSyncResult result)
        {
            await CleanupUploadedFirebaseFilesAsync(result.TemporalCleanups.Concat(result.FinalCleanups).ToList());
        }

        private async Task CompensatePreparedVariantSyncAsync(PreparedVariantSyncResult result)
        {
            try
            {
                await CleanupUploadedFirebaseFilesAsync(result.NewFileCompensations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo la compensacion de imagen final para variantes de productos y servicios.");
            }
        }

        private async Task<Guid> EnsureAtributoValorAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idAtributo, ProductoServicioAtributoValorGuardarRequest valor, DateTime ahora)
        {
            if (valor.IdAtributoValor.HasValue && valor.IdAtributoValor.Value != Guid.Empty)
            {
                using SqlCommand validateExisting = new SqlCommand(@"
SELECT TOP 1 id
FROM dbo.ProductosServiciosAtributosValores
WHERE idEmpresa = @IdEmpresa AND idAtributo = @IdAtributo AND id = @IdAtributoValor", connection, transaction);
                validateExisting.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                validateExisting.Parameters.AddWithValue("@IdAtributo", idAtributo);
                validateExisting.Parameters.AddWithValue("@IdAtributoValor", valor.IdAtributoValor.Value);
                object? validExisting = await validateExisting.ExecuteScalarAsync();
                if (validExisting != null && validExisting != DBNull.Value)
                {
                    return valor.IdAtributoValor.Value;
                }

                throw new ProductoServicioValidationException("Se detectó un valor de atributo inválido para la variante.");
            }

            using SqlCommand find = new SqlCommand(@"
SELECT TOP 1 id
FROM dbo.ProductosServiciosAtributosValores
WHERE idEmpresa = @IdEmpresa AND idAtributo = @IdAtributo AND Valor = @Valor", connection, transaction);
            find.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            find.Parameters.AddWithValue("@IdAtributo", idAtributo);
            find.Parameters.AddWithValue("@Valor", valor.Valor.Trim());
            object? existing = await find.ExecuteScalarAsync();
            if (existing != null && existing != DBNull.Value)
            {
                return (Guid)existing;
            }

            Guid id = Guid.NewGuid();
            using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosAtributosValores
    (id, idEmpresa, identityKey, idAtributo, Valor, Orden, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdAtributo, @Valor, @Orden, 1, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);
            insert.Parameters.AddWithValue("@Id", id);
            insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
            insert.Parameters.AddWithValue("@IdAtributo", idAtributo);
            insert.Parameters.AddWithValue("@Valor", valor.Valor.Trim());
            insert.Parameters.AddWithValue("@Orden", valor.Orden);
            insert.Parameters.AddWithValue("@FechaCreacion", ahora);
            insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
            await insert.ExecuteNonQueryAsync();
            return id;
        }

        private async Task ValidateProductoAtributoRequestIdsAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio, List<ProductoServicioAtributoGuardarRequest> atributos)
        {
            foreach (ProductoServicioAtributoGuardarRequest atributo in atributos.Where(x => x.IdProductoAtributo.HasValue && x.IdProductoAtributo.Value != Guid.Empty))
            {
                using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosProductoAtributos
WHERE idEmpresa = @IdEmpresa
  AND idProductoServicio = @IdProductoServicio
  AND idAtributo = @IdAtributo
  AND id = @IdProductoAtributo", connection, transaction);
                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                command.Parameters.AddWithValue("@IdAtributo", atributo.IdAtributo);
                command.Parameters.AddWithValue("@IdProductoAtributo", atributo.IdProductoAtributo.GetValueOrDefault());
                if (Convert.ToInt32(await command.ExecuteScalarAsync()) == 0)
                {
                    throw new ProductoServicioValidationException("Se detectó una relación de atributo inválida para el producto.");
                }
            }
        }

        private async Task ValidateProductoOpcionVarianteRequestIdsAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio, List<ProductoServicioOpcionVarianteGuardarRequest> opciones)
        {
            Dictionary<Guid, HashSet<Guid>> valoresPorOpcion = new Dictionary<Guid, HashSet<Guid>>();
            using (SqlCommand command = new SqlCommand(@"
SELECT ov.id AS IdOpcion, ovv.id AS IdValor
FROM dbo.ProductosServiciosOpcionesVariante ov
LEFT JOIN dbo.ProductosServiciosOpcionesVarianteValores ovv
    ON ovv.idEmpresa = ov.idEmpresa AND ovv.idOpcionVariante = ov.id
WHERE ov.idEmpresa = @IdEmpresa
  AND ov.idProductoServicio = @IdProductoServicio", connection, transaction))
            {
                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Guid idOpcion = ReadGuid(reader, "IdOpcion");
                    if (!valoresPorOpcion.TryGetValue(idOpcion, out HashSet<Guid>? valores))
                    {
                        valores = new HashSet<Guid>();
                        valoresPorOpcion[idOpcion] = valores;
                    }

                    Guid idValor = ReadGuid(reader, "IdValor");
                    if (idValor != Guid.Empty)
                    {
                        valores.Add(idValor);
                    }
                }
            }

            foreach (ProductoServicioOpcionVarianteGuardarRequest opcion in opciones)
            {
                HashSet<Guid>? valoresActuales = null;
                if (opcion.Id.HasValue && opcion.Id.Value != Guid.Empty)
                {
                    if (!valoresPorOpcion.TryGetValue(opcion.Id.Value, out valoresActuales))
                    {
                        throw new ProductoServicioValidationException("Se detectó una opción de variante inválida para el producto.");
                    }
                }

                foreach (ProductoServicioOpcionVarianteValorGuardarRequest valor in opcion.Valores.Where(x => x.Id.HasValue && x.Id.Value != Guid.Empty))
                {
                    if (valoresActuales == null || !valoresActuales.Contains(valor.Id.GetValueOrDefault()))
                    {
                        throw new ProductoServicioValidationException("Se detectó un valor de opción inválido para el producto.");
                    }
                }
            }
        }

        private static VariantOptionReference ResolveVariantOptionReference(Dictionary<string, VariantOptionReference> optionReferences, Guid idOpcionVariante, string opcion)
        {
            if (idOpcionVariante != Guid.Empty)
            {
                VariantOptionReference? matchById = optionReferences.Values.FirstOrDefault(item => item.Id == idOpcionVariante);
                if (matchById != null)
                {
                    return matchById;
                }
            }

            string key = NormalizeCatalogKey(opcion);
            if (!string.IsNullOrWhiteSpace(key) && optionReferences.TryGetValue(key, out VariantOptionReference? current))
            {
                return current;
            }

            throw new ProductoServicioValidationException("Se detectó una opción de variante inválida para una combinación.");
        }

        private static Guid ResolveVariantOptionValueId(VariantOptionReference option, Guid? idOpcionVarianteValor, string valor)
        {
            if (idOpcionVarianteValor.HasValue && idOpcionVarianteValor.Value != Guid.Empty)
            {
                if (option.Valores.Values.Contains(idOpcionVarianteValor.Value))
                {
                    return idOpcionVarianteValor.Value;
                }

                throw new ProductoServicioValidationException("Se detectó un valor de opción inválido para una variante.");
            }

            string key = NormalizeCatalogKey(valor);
            if (!string.IsNullOrWhiteSpace(key) && option.Valores.TryGetValue(key, out Guid idValor))
            {
                return idValor;
            }

            throw new ProductoServicioValidationException("Se detectó un valor de opción inválido para una variante.");
        }

        private static string NormalizeCatalogKey(string value)
        {
            return string.Concat((value ?? string.Empty)
                .Trim()
                .Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark))
                .Trim()
                .ToLowerInvariant();
        }

        private static string BuildTemporalFolderName(string empresaStorageKey)
        {
            return $"{empresaStorageKey}/ProductosServicios/Temporal/Imagen";
        }

        private static string BuildTemporalMultimediaFolderName(string empresaStorageKey, string operacionCarga, string tipoMultimedia)
        {
            string operacion = string.IsNullOrWhiteSpace(operacionCarga) ? Guid.NewGuid().ToString("N") : operacionCarga.Trim();
            return $"{empresaStorageKey}/ProductosServicios/Temporal/{operacion}/{tipoMultimedia}";
        }

        private static string BuildFinalFolderName(string empresaStorageKey, Guid productoId)
        {
            return $"{empresaStorageKey}/ProductosServicios/{productoId:N}/Imagen";
        }

        private static string BuildFinalVariantImageFolderName(string empresaStorageKey, Guid productoId, Guid varianteId)
        {
            return $"{empresaStorageKey}/ProductosServicios/{productoId:N}/Variantes/{varianteId:N}/Imagen";
        }

        private static string BuildFinalMultimediaFolderName(string empresaStorageKey, Guid productoId, string tipoMultimedia)
        {
            string folder = tipoMultimedia switch
            {
                "foto" => "Fotos",
                "video" => "Video",
                _ => "Documentos"
            };

            return $"{empresaStorageKey}/ProductosServicios/{productoId:N}/{folder}";
        }

        private static string BuildStoredFileName(string fileName, string mimeType)
        {
            string extension = NormalizeExtension(Path.GetExtension(fileName), mimeType);
            return $"{Guid.NewGuid():N}{extension}";
        }

        private static string NormalizeExtension(string? extension, string mimeType)
        {
            string normalized = (extension ?? string.Empty).Trim().ToLowerInvariant();
            if (ExtensionesImagenPermitidas.Contains(normalized))
            {
                return normalized;
            }

            if (normalized is ".pdf" or ".doc" or ".docx" or ".mp4" or ".mov" or ".webm")
            {
                return normalized;
            }

            return (mimeType ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "application/pdf" => ".pdf",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                "video/mp4" => ".mp4",
                "video/quicktime" => ".mov",
                "video/webm" => ".webm",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
        }

        private static string NormalizeTipoMultimedia(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return TiposMultimediaPermitidos.Contains(normalized) ? normalized : string.Empty;
        }

        private static string NormalizeDescripcionCatalogo(string? value)
        {
            return SanitizeRichTextHtml(value);
        }

        internal static string SanitizeRichTextHtml(string? value)
        {
            string html = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            html = html.Replace("\0", string.Empty);
            html = Regex.Replace(html, "<!--[\\s\\S]*?-->", string.Empty, RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "<(script|style|iframe|object|embed|form|input|button|textarea|select)[\\s\\S]*?</\\1>", string.Empty, RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "<[^>]+>", match => SanitizeAllowedHtmlTag(match.Value), RegexOptions.IgnoreCase);
            return html.Trim();
        }

        private static string SanitizeAllowedHtmlTag(string rawTag)
        {
            Match nameMatch = Regex.Match(rawTag, @"^<\s*/?\s*([a-z0-9]+)", RegexOptions.IgnoreCase);
            if (!nameMatch.Success)
            {
                return string.Empty;
            }

            string tagName = nameMatch.Groups[1].Value.ToLowerInvariant();
            HashSet<string> allowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "h2", "h3", "blockquote", "code", "pre", "a"
            };

            if (!allowedTags.Contains(tagName))
            {
                return string.Empty;
            }

            bool isClosing = rawTag.Contains("</", StringComparison.Ordinal);
            bool selfClosing = rawTag.EndsWith("/>", StringComparison.Ordinal);
            if (isClosing)
            {
                return $"</{tagName}>";
            }

            if (!string.Equals(tagName, "a", StringComparison.OrdinalIgnoreCase))
            {
                return selfClosing ? $"<{tagName} />" : $"<{tagName}>";
            }

            string href = ExtractAttributeValue(rawTag, "href");
            if (!IsSafeHref(href))
            {
                href = string.Empty;
            }

            string title = WebUtility.HtmlEncode(ExtractAttributeValue(rawTag, "title"));
            bool openBlank = string.Equals(ExtractAttributeValue(rawTag, "target"), "_blank", StringComparison.OrdinalIgnoreCase);
            StringBuilder builder = new StringBuilder("<a");

            if (!string.IsNullOrWhiteSpace(href))
            {
                builder.Append(" href=\"").Append(WebUtility.HtmlEncode(href)).Append('"');
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                builder.Append(" title=\"").Append(title).Append('"');
            }

            if (openBlank)
            {
                builder.Append(" target=\"_blank\" rel=\"noopener noreferrer\"");
            }

            builder.Append('>');
            return builder.ToString();
        }

        private static string ExtractAttributeValue(string tag, string attributeName)
        {
            Match match = Regex.Match(tag, attributeName + "\\s*=\\s*(['\"])(.*?)\\1", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[2].Value.Trim() : string.Empty;
        }

        private static bool IsSafeHref(string? href)
        {
            string normalized = (href ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            if (normalized.StartsWith("#", StringComparison.Ordinal))
            {
                return true;
            }

            if (!Uri.TryCreate(normalized, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                return false;
            }

            if (!uri.IsAbsoluteUri)
            {
                return normalized.StartsWith("/", StringComparison.Ordinal);
            }

            return uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps
                || uri.Scheme == Uri.UriSchemeMailto
                || string.Equals(uri.Scheme, "tel", StringComparison.OrdinalIgnoreCase);
        }

        private static string HtmlToPlainText(string? value)
        {
            string html = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            string withBreaks = Regex.Replace(html, @"<(br|/p|/li|/h2|/h3)\s*/?>", "\n", RegexOptions.IgnoreCase);
            string withoutTags = Regex.Replace(withBreaks, "<[^>]+>", string.Empty, RegexOptions.IgnoreCase);
            string decoded = WebUtility.HtmlDecode(withoutTags);
            return Regex.Replace(decoded, @"\n{3,}", "\n\n").Trim();
        }

        private static void AppendRichTextToPdf(TextDescriptor text, string? value)
        {
            string html = (value ?? string.Empty).Trim();
            bool bold = false;
            bool italic = false;
            bool hasContent = false;

            foreach (Match token in Regex.Matches(html, @"<[^>]+>|[^<]+"))
            {
                string part = token.Value;
                if (!part.StartsWith("<", StringComparison.Ordinal))
                {
                    string plainText = WebUtility.HtmlDecode(part);
                    if (!string.IsNullOrEmpty(plainText))
                    {
                        TextSpanDescriptor span = text.Span(plainText);
                        if (bold)
                        {
                            span.Bold();
                        }

                        if (italic)
                        {
                            span.Italic();
                        }

                        hasContent = true;
                    }

                    continue;
                }

                string tag = Regex.Replace(part, @"[<>\s/]", string.Empty).ToLowerInvariant();
                bool closing = part.StartsWith("</", StringComparison.Ordinal);
                switch (tag)
                {
                    case "strong":
                    case "b":
                        bold = !closing;
                        break;
                    case "em":
                    case "i":
                        italic = !closing;
                        break;
                    case "br":
                        text.Line("");
                        hasContent = false;
                        break;
                    case "p":
                    case "h2":
                    case "h3":
                    case "ul":
                    case "ol":
                        if (closing && hasContent)
                        {
                            text.Line("");
                            hasContent = false;
                        }
                        break;
                    case "li":
                        if (!closing)
                        {
                            if (hasContent)
                            {
                                text.Line("");
                            }

                            text.Span("• ");
                            hasContent = true;
                        }
                        else if (hasContent)
                        {
                            text.Line("");
                            hasContent = false;
                        }
                        break;
                }
            }
        }

        private static string ValidateTemporalMultimediaUpload(string tipoMultimedia, IFormFile archivo)
        {
            if (archivo.Length <= 0)
            {
                return "Selecciona un archivo válido para cargar.";
            }

            string extension = NormalizeExtension(Path.GetExtension(archivo.FileName), archivo.ContentType ?? string.Empty);
            string mimeType = (archivo.ContentType ?? string.Empty).Trim().ToLowerInvariant();
            return tipoMultimedia switch
            {
                "foto" when archivo.Length > ImagenMaxBytes => "La foto excede el tamaño máximo permitido de 10 MB.",
                "foto" when !(new[] { ".jpg", ".jpeg", ".png", ".webp", ".heic" }.Contains(extension)) => "Selecciona una foto válida.",
                "foto" when !string.IsNullOrWhiteSpace(mimeType) && !mimeType.StartsWith("image/", StringComparison.Ordinal) => "Selecciona una foto válida.",
                "video" when archivo.Length > VideoMaxBytes => "El video excede el tamaño máximo permitido de 200 MB.",
                "video" when !(new[] { ".mp4", ".mov", ".webm" }.Contains(extension)) => "Selecciona un video válido.",
                "video" when !string.IsNullOrWhiteSpace(mimeType) && !mimeType.StartsWith("video/", StringComparison.Ordinal) => "Selecciona un video válido.",
                "documento" when archivo.Length > DocumentoMaxBytes => "El documento excede el tamaño máximo permitido de 25 MB.",
                "documento" when !(new[] { ".pdf", ".doc", ".docx" }.Contains(extension)) => "Selecciona un documento PDF o Word válido.",
                _ => string.Empty
            };
        }

        private static string NormalizeArchivoText(string value, int maxLength, string fallback)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = fallback;
            }

            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }

        private static string CreateTemporalToken(TemporalImageTokenPayload payload)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        }

        private static TemporalImageTokenPayload? TryParseTemporalToken(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return null;
                }

                byte[] bytes = Convert.FromBase64String(token);
                TemporalImageTokenPayload? payload = JsonSerializer.Deserialize<TemporalImageTokenPayload>(Encoding.UTF8.GetString(bytes));
                if (payload == null || payload.ExpiraUtc < DateTime.UtcNow)
                {
                    return null;
                }

                return payload;
            }
            catch
            {
                return null;
            }
        }

        private static FirebaseCleanupItem? TryBuildCleanupItemFromUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || !Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uri))
            {
                return null;
            }

            string path = Uri.UnescapeDataString(uri.AbsolutePath);
            int bucketMarker = path.IndexOf("/o/", StringComparison.OrdinalIgnoreCase);
            if (bucketMarker < 0)
            {
                return null;
            }

            string filePath = path[(bucketMarker + 3)..];
            int slashIndex = filePath.LastIndexOf('/');
            if (slashIndex <= 0 || slashIndex >= filePath.Length - 1)
            {
                return null;
            }

            return new FirebaseCleanupItem
            {
                FolderName = filePath[..slashIndex],
                StoredName = filePath[(slashIndex + 1)..]
            };
        }

        private static bool FolderBelongsToEmpresa(string folderName, string empresaStorageKey)
        {
            return !string.IsNullOrWhiteSpace(folderName) &&
                   folderName.StartsWith($"{empresaStorageKey}/", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<byte[]> ReadFileBytesAsync(IFormFile archivo)
        {
            using MemoryStream stream = new MemoryStream();
            await archivo.CopyToAsync(stream);
            return stream.ToArray();
        }

        private Task<bool> TryResolveRequestContextAsync(
            Guid? clientEmpresaId,
            string? clientEmpresaKey,
            out RequestContext context,
            out IActionResult? error,
            ProductosServiciosPermissionRequirement requirement)
        {
            context = null!;
            error = null;

            Guid? effectiveEmpresaId = TryResolveEmpresaId(out string? proxyEmpresaKey);
            if (!effectiveEmpresaId.HasValue || effectiveEmpresaId.Value == Guid.Empty)
            {
                error = Unauthorized(new ProductoServicioOperacionResponse { Mensaje = "No fue posible resolver la empresa activa." });
                return Task.FromResult(false);
            }

            if (clientEmpresaId.HasValue && clientEmpresaId.Value != Guid.Empty && clientEmpresaId.Value != effectiveEmpresaId.Value)
            {
                error = StatusCode(403, new ProductoServicioOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
                return Task.FromResult(false);
            }

            string empresaStorageKey = TryResolveEmpresaStorageKey(effectiveEmpresaId.Value, proxyEmpresaKey);
            if (!string.IsNullOrWhiteSpace(clientEmpresaKey) &&
                !string.Equals(clientEmpresaKey.Trim(), empresaStorageKey, StringComparison.OrdinalIgnoreCase))
            {
                error = StatusCode(403, new ProductoServicioOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
                return Task.FromResult(false);
            }

            // Unbound query/form fields must not contradict the authenticated context.
            foreach (var item in Request.Query)
            {
                if (!TenantInputMatches(item.Key, item.Value, effectiveEmpresaId.Value, empresaStorageKey))
                { error = StatusCode(403, new { code = "TENANT_INPUT_MISMATCH", message = "No fue posible autorizar la solicitud." }); return Task.FromResult(false); }
            }
            if (Request.HasFormContentType)
            {
                foreach (var item in Request.Form)
                    if (!TenantInputMatches(item.Key, item.Value, effectiveEmpresaId.Value, empresaStorageKey))
                    { error = StatusCode(403, new { code = "TENANT_INPUT_MISMATCH", message = "No fue posible autorizar la solicitud." }); return Task.FromResult(false); }
            }

            TenantDatabaseDescriptor tenantDatabase;
            try
            {
                tenantDatabase = _tenantDatabaseResolver.ResolveAsync(
                    new TenantDatabaseContext
                    {
                        EmpresaKey = empresaStorageKey,
                        IdEmpresa = effectiveEmpresaId.Value
                    },
                    HttpContext.RequestAborted).GetAwaiter().GetResult();
            }
            catch (TenantDatabaseResolutionException ex)
            {
                _logger.LogWarning("Resolución tenant ProductosServicios falló. Categoria={TenantResolutionCode} EmpresaKey={EmpresaKey} IdEmpresa={IdEmpresa}",
                    ex.Code,
                    SanitizeEmpresaKey(empresaStorageKey),
                    effectiveEmpresaId.Value);
                error = ToTenantResolutionError(ex.Code);
                return Task.FromResult(false);
            }

            SignedProxyContext? signedContext = HttpContext.Items.TryGetValue(ProxyContextItemKey, out var identity)
                ? identity as SignedProxyContext
                : null;
            ProductosServiciosAuthorizationDecision authorization = _authorizationService.AuthorizeAsync(
                new ProductosServiciosAuthorizationRequest
                {
                    IdEmpresa = effectiveEmpresaId.Value,
                    UserId = signedContext?.UserId ?? string.Empty,
                    TenantDatabase = tenantDatabase,
                    Requirement = requirement,
                    PermissionCode = ResolvePermissionCodeForCurrentAction()
                },
                HttpContext.RequestAborted).GetAwaiter().GetResult();
            if (!authorization.IsAllowed(requirement))
            {
                _logger.LogWarning(
                    "AuthZ ProductosServicios bloqueó operación. ReferenceId={ReferenceId} PermissionCode={PermissionCode} Requirement={Requirement} ReasonCode={ReasonCode}",
                    authorization.ReferenceId,
                    authorization.PermissionCode,
                    requirement.ToString(),
                    authorization.ReasonCode);
                error = StatusCode(403, new
                {
                    code = requirement == ProductosServiciosPermissionRequirement.Write ? "PRODUCTOS_SERVICIOS_WRITE_FORBIDDEN" : "PRODUCTOS_SERVICIOS_ACCESS_FORBIDDEN",
                    message = "No tienes permiso para realizar esta operación.",
                    referenceId = authorization.ReferenceId
                });
                return Task.FromResult(false);
            }

            CompatibilityDecision compatibility = _compatibilityGate.EvaluateAsync(tenantDatabase, DatabaseScopes.ProductosServicios, HttpContext.RequestAborted).GetAwaiter().GetResult();
            if (!compatibility.IsAllowed)
            {
                _logger.LogWarning("Gate ProductosServicios bloqueó operación. ReferenceId={ReferenceId} Identity={DatabaseIdentity} Scope={Scope} ReasonCode={ReasonCode} CurrentVersion={CurrentVersion} SchemaResult={SchemaResult}",
                    compatibility.ReferenceId,
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(compatibility.SanitizedIdentity))),
                    compatibility.Scope,
                    compatibility.ReasonCode,
                    compatibility.CurrentVersion,
                    compatibility.SchemaResult?.ToString() ?? "N/A");
                error = ToCompatibilityError(compatibility);
                return Task.FromResult(false);
            }

            // T22: prepare company scope after authenticated context/resolution/T20, without seeds or DDL.
            // Functional AuthZ requires the existing PS policy to be identified (T23 PO decision).
            CompanyBootstrapResult company = _companyBootstrapper.BootstrapAsync(
                tenantDatabase, DatabaseScopes.ProductosServicios, HttpContext.RequestAborted).GetAwaiter().GetResult();
            if (company.Status != "NO_CHANGES")
            {
                error = StatusCode(503, new { code = company.ReasonCode,
                    message = "No fue posible preparar la operación de la empresa. Intenta nuevamente más tarde.",
                    referenceId = company.ReferenceId });
                return Task.FromResult(false);
            }

            context = new RequestContext
            {
                IdEmpresa = effectiveEmpresaId.Value,
                EmpresaStorageKey = empresaStorageKey,
                TenantDatabase = tenantDatabase
            };
            return Task.FromResult(true);
        }

        private string ResolvePermissionCodeForCurrentAction()
        {
            string actionName = ControllerContext?.ActionDescriptor?.ActionName
                ?? RouteData?.Values["action"]?.ToString()
                ?? string.Empty;
            if (CategoriaActions.Contains(actionName))
            {
                return CategoriasPermissionCode;
            }

            if (MarcaActions.Contains(actionName))
            {
                return MarcasPermissionCode;
            }

            if (UnidadMedidaActions.Contains(actionName))
            {
                return UnidadesMedidaPermissionCode;
            }

            return AbcPermissionCode;
        }

        private static bool TenantInputMatches(string key, Microsoft.Extensions.Primitives.StringValues values, Guid id, string empresa)
        {
            if (new[] { "cadena", "connectionString", "server", "database", "password" }.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
            if (new[] { "idEmpresa", "empresaId", "tenantId" }.Contains(key, StringComparer.OrdinalIgnoreCase))
                return values.All(v => Guid.TryParse(v, out Guid parsed) && parsed == id);
            if (new[] { "empresa", "empresaKey" }.Contains(key, StringComparer.OrdinalIgnoreCase))
                return values.All(v => string.Equals(v?.Trim(), empresa, StringComparison.OrdinalIgnoreCase));
            // Scope and DatabaseIdentity are never consumed from request fields.
            return true;
        }

        private Guid? TryResolveEmpresaId(out string? proxyEmpresaKey)
        {
            proxyEmpresaKey = null;
            if (!ProductosServiciosIdentityValidation.TryValidate(User, Request.Headers,
                _configuration["fireBdata:fireClave"] ?? string.Empty, DateTimeOffset.UtcNow,
                out TenantDatabaseContext verified, out string userId)) return null;
            var identity = new SignedProxyContext
            {
                IdEmpresa = verified.IdEmpresa, EmpresaStorageKey = verified.EmpresaKey,
                UserId = userId,
                UsuarioId = Guid.TryParse(userId, out Guid actor) && actor != Guid.Empty ? actor : null
            };
            HttpContext.Items[ProxyContextItemKey] = identity;
            proxyEmpresaKey = identity.EmpresaStorageKey;
            return identity.IdEmpresa;
        }

        private string TryResolveEmpresaStorageKey(Guid empresaId, string? proxyEmpresaKey = null)
            => proxyEmpresaKey ?? string.Empty;

        private Guid? TryResolveUsuarioId()
            => HttpContext.Items.TryGetValue(ProxyContextItemKey, out var identity)
                ? (identity as SignedProxyContext)?.UsuarioId : null;

        private SqlConnection CreateConnection(RequestContext context)
        {
            return _tenantSqlConnectionFactory.CreateConnection(context.TenantDatabase);
        }


        private IActionResult ToCompatibilityError(CompatibilityDecision decision)
        {
            string message = decision.ReasonCode switch
            {
                "SCHEMA_PREPARING" => "Productos y Servicios se está preparando. Intente nuevamente.",
                "MIGRATION_IN_PROGRESS" => "Productos y Servicios se está actualizando. Intente nuevamente.",
                "SCHEMA_EMPTY" => "Productos y Servicios aún no está preparado para esta empresa.",
                "SCHEMA_PARTIAL" => "No es posible acceder temporalmente a Productos y Servicios.",
                "SCHEMA_OUTDATED" => "Productos y Servicios requiere actualización antes de continuar.",
                "SCHEMA_FUTURE" => "La versión de Productos y Servicios no es compatible con esta aplicación.",
                "SCHEMA_UNKNOWN" => "No es posible confirmar la compatibilidad de Productos y Servicios.",
                "SCHEMA_UNAVAILABLE" => "Productos y Servicios no está disponible temporalmente.",
                "SCHEMA_DRIFT" => "No es posible acceder temporalmente a Productos y Servicios.",
                "SCHEMA_DRIFT_CRITICAL" => "No es posible acceder temporalmente a Productos y Servicios.",
                "VERSION_EVIDENCE_MISSING" => "No es posible confirmar la versión de Productos y Servicios.",
                "VERSION_INCOMPATIBLE" => "La versión de Productos y Servicios no es compatible con esta aplicación.",
                "MANIFEST_HASH_MISMATCH" => "No es posible confirmar la compatibilidad de Productos y Servicios.",
                "VALIDATION_INCONCLUSIVE" => "No es posible validar Productos y Servicios en este momento.",
                "TENANT_CONTEXT_INVALID" => "No fue posible resolver la empresa activa.",
                "DATABASE_IDENTITY_INVALID" => "No fue posible validar la base de datos de la empresa.",
                _ => "No es posible acceder temporalmente a Productos y Servicios."
            };

            return StatusCode(503, new
            {
                code = decision.ReasonCode,
                message,
                referenceId = decision.ReferenceId
            });
        }

        private IActionResult HandleException(Exception ex, string operation, string safeMessage)
        {
            _logger.LogError("Error en ProductosServicios. ReferenceId={ReferenceId} Operation={Operation} ReasonCode={ReasonCode}", Guid.NewGuid().ToString("N"), operation, "OPERATION_FAILED");
            if (IsTenantDatabaseUnavailable(ex))
            {
                return StatusCode(503, new ProductoServicioOperacionResponse { Mensaje = "La base de datos de la empresa no está disponible en este momento." });
            }

            return StatusCode(500, new ProductoServicioOperacionResponse { Mensaje = safeMessage });
        }

        private IActionResult ToTenantResolutionError(TenantDatabaseResolutionCode code)
        {
            string message = "No fue posible autorizar la empresa activa.";
            int statusCode = code switch
            {
                TenantDatabaseResolutionCode.TenantContextMissing => StatusCodes.Status401Unauthorized,
                TenantDatabaseResolutionCode.TenantDatabaseResolutionFailed => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status403Forbidden
            };

            return StatusCode(statusCode, new ProductoServicioOperacionResponse { Mensaje = message });
        }

        private static bool IsTenantDatabaseUnavailable(Exception ex)
        {
            return ex is SqlException sqlException &&
                   sqlException.Errors.Cast<SqlError>().Any(error =>
                       error.Number == -2 ||
                       error.Number == 53 ||
                       error.Number == 233 ||
                       error.Number == 4060 ||
                       error.Number == 18456);
        }

        private static string SanitizeEmpresaKey(string value)
        {
            string trimmed = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (trimmed.Length <= 24)
            {
                return trimmed;
            }

            return trimmed.Substring(0, 24);
        }

        private static void AppendBusquedaCatalogo(StringBuilder query, SqlCommand command, string busqueda)
        {
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR ISNULL(Descripcion, '') LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }
        }

        private static void AppendGuidFilter(StringBuilder query, SqlCommand command, string columnName, string parameterName, Guid? value)
        {
            if (value.HasValue && value.Value != Guid.Empty)
            {
                query.Append($" AND {columnName} = {parameterName}");
                command.Parameters.AddWithValue(parameterName, value.Value);
            }
        }

        private static void AppendTinyIntFilter(StringBuilder query, SqlCommand command, string columnName, string parameterName, byte? value)
        {
            if (value.HasValue)
            {
                query.Append($" AND {columnName} = {parameterName}");
                command.Parameters.AddWithValue(parameterName, value.Value);
            }
        }

        private static void AppendBitFilter(StringBuilder query, SqlCommand command, string columnName, string parameterName, bool? value)
        {
            if (value.HasValue)
            {
                query.Append($" AND {columnName} = {parameterName}");
                command.Parameters.AddWithValue(parameterName, value.Value);
            }
        }

        private static void AppendEstatusFilter(StringBuilder query, string columnName, string estatus)
        {
            switch ((estatus ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "activos":
                    query.Append($" AND {columnName} = 1");
                    break;
                case "inactivos":
                    query.Append($" AND {columnName} = 0");
                    break;
            }
        }

        private static bool CategoriaAplicaATipo(byte aplicaA, byte tipo)
        {
            return aplicaA == AplicaATodos ||
                   (aplicaA == AplicaAProductos && tipo == TipoProducto) ||
                   (aplicaA == AplicaAServicios && tipo == TipoServicio);
        }

        private static bool HasContradictoryInventoryValues(decimal? existenciaInicial, decimal? existenciaMinima)
        {
            return HasMeaningfulInventoryValue(existenciaInicial) || HasMeaningfulInventoryValue(existenciaMinima);
        }

        private static bool HasMeaningfulInventoryValue(decimal? value)
        {
            return value.HasValue && value.Value != 0m;
        }

        private static decimal? NormalizeInventoryInput(decimal? value)
        {
            return value.HasValue && value.Value == 0m ? null : value;
        }

        private static decimal CalcularExistenciaPosterior(decimal existenciaActual, decimal cantidad, byte tipoMovimiento)
        {
            return tipoMovimiento switch
            {
                MovimientoEntrada => existenciaActual + cantidad,
                MovimientoAjustePositivo => existenciaActual + cantidad,
                MovimientoSalida => existenciaActual - cantidad,
                MovimientoAjusteNegativo => existenciaActual - cantidad,
                MovimientoExistenciaInicial => cantidad,
                _ => existenciaActual
            };
        }

        private static string GetMovimientoNombre(byte tipoMovimiento)
        {
            return tipoMovimiento switch
            {
                MovimientoExistenciaInicial => "Existencia inicial",
                MovimientoEntrada => "Entrada",
                MovimientoSalida => "Salida",
                MovimientoAjustePositivo => "Ajuste positivo",
                MovimientoAjusteNegativo => "Ajuste negativo",
                _ => "Movimiento"
            };
        }

        private static string GetAplicaANombre(byte aplicaA)
        {
            return aplicaA switch
            {
                AplicaAProductos => "Productos",
                AplicaAServicios => "Servicios",
                _ => "Todos"
            };
        }

        private static string Truncate(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }

        private static ProductoServicioListadoDto MapProductoServicioListado(SqlDataReader reader)
        {
            return new ProductoServicioListadoDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                Tipo = ReadByte(reader, "Tipo"),
                TipoNombre = ReadString(reader, "TipoNombre"),
                Codigo = ReadString(reader, "Codigo"),
                Tag = ReadString(reader, "Tag"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                IdCategoria = ReadGuid(reader, "idCategoria"),
                Categoria = ReadString(reader, "Categoria"),
                CategoriaAplicaA = ReadByte(reader, "CategoriaAplicaA"),
                IdMarca = ReadNullableGuid(reader, "idMarca"),
                Marca = ReadString(reader, "Marca"),
                IdUnidadMedida = ReadGuid(reader, "idUnidadMedida"),
                UnidadMedida = ReadString(reader, "UnidadMedida"),
                UnidadAbreviatura = ReadString(reader, "UnidadAbreviatura"),
                UnidadPermiteDecimales = ReadBool(reader, "UnidadPermiteDecimales"),
                IdColeccion = ReadNullableGuid(reader, "idColeccion"),
                ColeccionNumero = ReadString(reader, "ColeccionNumero"),
                ColeccionNombre = ReadString(reader, "ColeccionNombre"),
                IdPaquete = ReadNullableGuid(reader, "idPaquete"),
                PaqueteNombre = ReadString(reader, "PaqueteNombre"),
                Costo = ReadNullableDecimal(reader, "Costo"),
                PrecioPublico = ReadDecimal(reader, "PrecioPublico"),
                PrecioComparacion = ReadNullableDecimal(reader, "PrecioComparacion"),
                PrecioUnitarioMonto = ReadNullableDecimal(reader, "PrecioUnitarioMonto"),
                PrecioUnitarioCantidadTotal = ReadNullableDecimal(reader, "PrecioUnitarioCantidadTotal"),
                PrecioUnitarioUnidadTotal = ReadString(reader, "PrecioUnitarioUnidadTotal"),
                PrecioUnitarioBaseCantidad = ReadNullableDecimal(reader, "PrecioUnitarioBaseCantidad"),
                PrecioUnitarioUnidad = ReadString(reader, "PrecioUnitarioUnidad"),
                PrecioUnitarioUnidadBase = ReadString(reader, "PrecioUnitarioUnidadBase"),
                ObjetoImpuesto = ReadString(reader, "ObjetoImpuesto"),
                PorcentajeIVA = ReadDecimal(reader, "PorcentajeIVA"),
                ClaveProductoSat = ReadString(reader, "ClaveProductoSat"),
                ClaveUnidadSat = ReadString(reader, "ClaveUnidadSat"),
                EsProductoFisico = ReadBool(reader, "EsProductoFisico"),
                PesoKg = ReadNullableDecimal(reader, "PesoKg"),
                LargoCm = ReadNullableDecimal(reader, "LargoCm"),
                AnchoCm = ReadNullableDecimal(reader, "AnchoCm"),
                AltoCm = ReadNullableDecimal(reader, "AltoCm"),
                UsaNumeroSerie = ReadBool(reader, "UsaNumeroSerie"),
                CausaInventario = ReadBool(reader, "CausaInventario"),
                PermiteVentaSinExistencia = ReadBool(reader, "PermiteVentaSinExistencia"),
                ExistenciaActual = ReadNullableDecimal(reader, "ExistenciaActual"),
                ExistenciaMinima = ReadNullableDecimal(reader, "ExistenciaMinima"),
                CostoPromedio = ReadNullableDecimal(reader, "CostoPromedio"),
                ImagenUrl = ReadString(reader, "ImagenUrl"),
                ImagenNombre = ReadString(reader, "ImagenNombre"),
                CantidadFotos = ReadInt(reader, "CantidadFotos"),
                CantidadVideos = ReadInt(reader, "CantidadVideos"),
                CantidadDocumentos = ReadInt(reader, "CantidadDocumentos"),
                CantidadVariantes = ReadInt(reader, "CantidadVariantes"),
                Activo = ReadBool(reader, "Activo"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadNullableDateTime(reader, "FechaActualizacion"),
                FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
            };
        }

        private static ProductoServicioCategoriaDto MapCategoria(SqlDataReader reader)
        {
            byte aplicaA = ReadByte(reader, "AplicaA");
            return new ProductoServicioCategoriaDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                Codigo = ReadString(reader, "Codigo"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                AplicaA = aplicaA,
                AplicaANombre = GetAplicaANombre(aplicaA),
                Activo = ReadBool(reader, "Activo"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadDateTime(reader, "FechaActualizacion"),
                FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
            };
        }

        private static ProductoServicioMarcaDto MapMarca(SqlDataReader reader)
        {
            return new ProductoServicioMarcaDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                Codigo = ReadString(reader, "Codigo"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                Activo = ReadBool(reader, "Activo"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadDateTime(reader, "FechaActualizacion"),
                FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
            };
        }

        private static ProductoServicioUnidadMedidaDto MapUnidad(SqlDataReader reader)
        {
            return new ProductoServicioUnidadMedidaDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                Codigo = ReadString(reader, "Codigo"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                Abreviatura = ReadString(reader, "Abreviatura"),
                PermiteDecimales = ReadBool(reader, "PermiteDecimales"),
                TipoUnidad = HasColumn(reader, "TipoUnidad") ? ReadString(reader, "TipoUnidad") : "OTHER",
                TipoUnidadNombre = GetTipoUnidadNombre(HasColumn(reader, "TipoUnidad") ? ReadString(reader, "TipoUnidad") : "OTHER"),
                EsSistema = HasColumn(reader, "EsSistema") && ReadBool(reader, "EsSistema"),
                EsPersonalizada = !HasColumn(reader, "EsPersonalizada") || ReadBool(reader, "EsPersonalizada"),
                FactorConversion = HasColumn(reader, "FactorConversion") ? ReadNullableDecimal(reader, "FactorConversion") : null,
                Convertible = HasColumn(reader, "Convertible") && ReadBool(reader, "Convertible"),
                ClaveSistema = HasColumn(reader, "ClaveSistema") ? ReadString(reader, "ClaveSistema") : string.Empty,
                Activo = ReadBool(reader, "Activo"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadDateTime(reader, "FechaActualizacion"),
                FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
            };
        }

        private static ProductoServicioCatalogoComboDto MapCatalogoCombo(SqlDataReader reader)
        {
            return new ProductoServicioCatalogoComboDto
            {
                Id = ReadGuid(reader, "id"),
                Codigo = ReadString(reader, "Codigo"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                Activo = ReadBool(reader, "Activo"),
                AplicaA = ReadNullableByte(reader, "AplicaA"),
                Abreviatura = ReadString(reader, "Abreviatura"),
                PermiteDecimales = ReadNullableBool(reader, "PermiteDecimales"),
                Numero = HasColumn(reader, "Numero") ? ReadString(reader, "Numero") : string.Empty,
                TipoPaquete = HasColumn(reader, "TipoPaquete") ? ReadString(reader, "TipoPaquete") : string.Empty,
                LargoCm = HasColumn(reader, "LargoCm") ? ReadNullableDecimal(reader, "LargoCm") : null,
                AnchoCm = HasColumn(reader, "AnchoCm") ? ReadNullableDecimal(reader, "AnchoCm") : null,
                AltoCm = HasColumn(reader, "AltoCm") ? ReadNullableDecimal(reader, "AltoCm") : null,
                PesoEmpaqueVacioKg = HasColumn(reader, "PesoEmpaqueVacioKg") ? ReadNullableDecimal(reader, "PesoEmpaqueVacioKg") : null,
                EsPredeterminado = HasColumn(reader, "EsPredeterminado") ? ReadNullableBool(reader, "EsPredeterminado") : null
                ,TipoUnidad = HasColumn(reader, "TipoUnidad") ? ReadString(reader, "TipoUnidad") : string.Empty
                ,EsSistema = HasColumn(reader, "EsSistema") ? ReadNullableBool(reader, "EsSistema") : null
                ,EsPersonalizada = HasColumn(reader, "EsPersonalizada") ? ReadNullableBool(reader, "EsPersonalizada") : null
                ,FactorConversion = HasColumn(reader, "FactorConversion") ? ReadNullableDecimal(reader, "FactorConversion") : null
                ,Convertible = HasColumn(reader, "Convertible") ? ReadNullableBool(reader, "Convertible") : null
            };
        }

        private static ProductoServicioMovimientoDto MapMovimiento(SqlDataReader reader)
        {
            byte tipoMovimiento = ReadByte(reader, "TipoMovimiento");
            return new ProductoServicioMovimientoDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                TipoMovimiento = tipoMovimiento,
                TipoMovimientoNombre = GetMovimientoNombre(tipoMovimiento),
                Cantidad = ReadDecimal(reader, "Cantidad"),
                ExistenciaAnterior = ReadDecimal(reader, "ExistenciaAnterior"),
                ExistenciaPosterior = ReadDecimal(reader, "ExistenciaPosterior"),
                CostoUnitario = ReadNullableDecimal(reader, "CostoUnitario"),
                Referencia = ReadString(reader, "Referencia"),
                Observaciones = ReadString(reader, "Observaciones"),
                IdUsuario = ReadNullableGuid(reader, "idUsuario"),
                FechaMovimiento = ReadDateTime(reader, "FechaMovimiento")
            };
        }

        private async Task<byte[]> BuildFichaTecnicaPdfDocumentAsync(ProductoServicioFichaTecnicaDto ficha)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            byte[]? logo = LoadSharedCheckAppLogo();
            byte[]? imagenPrincipal = await LoadRemoteImageBytesAsync(ficha.ImagenUrl);
            Dictionary<Guid, byte[]?> imagenesVariantes = new Dictionary<Guid, byte[]?>();
            foreach (ProductoServicioVarianteDto variante in ficha.Variantes)
            {
                imagenesVariantes[variante.Id] = await LoadRemoteImageBytesAsync(variante.ImagenUrl);
            }

            bool hasPhysicalSection = ficha.Tipo == TipoProducto && (
                ficha.EsProductoFisico ||
                ficha.PesoKg.HasValue ||
                ficha.IdPaquete.HasValue ||
                ficha.PaqueteLargoCm.HasValue ||
                ficha.PaqueteAnchoCm.HasValue ||
                ficha.PaqueteAltoCm.HasValue ||
                ficha.PaquetePesoEmpaqueVacioKg.HasValue);
            bool hasInventorySection = ficha.Tipo == TipoProducto && ficha.CausaInventario;
            bool hasAttributesSection = ficha.Tipo == TipoProducto && ficha.Atributos.Any();
            bool hasVariantsSection = ficha.Tipo == TipoProducto && ficha.Variantes.Any();
            bool hasMultimediaSection = ficha.Multimedia.Any();

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(style => style.FontFamily(Fonts.Calibri).FontSize(9).FontColor(FichaInk));

                    page.Header().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(brand =>
                        {
                            if (logo != null) brand.Item().Width(112).Height(25).Image(logo).FitArea();
                            brand.Item().PaddingTop(3).Text("Tu operación, más simple.").FontSize(8).FontColor(FichaMuted);
                        });
                        row.RelativeItem().AlignRight().Column(title =>
                        {
                            title.Item().AlignRight().Text("FICHA TÉCNICA").Bold().FontSize(13);
                            title.Item().AlignRight().Text(ficha.Tipo == TipoServicio ? "Servicio" : "Producto").FontSize(8).FontColor(FichaMuted);
                        });
                    });

                    page.Content().Column(content =>
                    {
                        if (ficha.Tipo == TipoServicio)
                        {
                            content.Spacing(15);
                            content.Item().Element(container => ComposeFichaServiceHero(container, ficha, imagenPrincipal));
                            content.Item().Element(container => ComposeFichaServiceDescription(container, ficha));
                            content.Item().Element(container => ComposeFichaServiceMetadata(container, ficha));
                            content.Item().Element(container => ComposeFichaCommercialSection(container, ficha));
                            content.Item().Element(container => ComposeFichaFiscalSection(container, ficha));
                            if (hasMultimediaSection)
                                content.Item().Element(container => ComposeFichaMultimediaSection(container, ficha.Multimedia));
                            return;
                        }

                        content.Spacing(10);
                        content.Item().Element(container => ComposeFichaGeneralSection(container, ficha, imagenPrincipal));
                        content.Item().Element(container => ComposeFichaCommercialSection(container, ficha));
                        if (hasInventorySection || hasAttributesSection)
                            content.Item().Row(row =>
                            {
                                row.Spacing(10);
                                if (hasInventorySection) row.RelativeItem().Element(box => ComposeFichaInventorySection(box, ficha));
                                if (hasAttributesSection) row.RelativeItem().Element(box => ComposeFichaAttributesSection(box, ficha.Atributos));
                            });
                        if (ficha.PresentacionesVenta.Count > 0 || hasVariantsSection)
                            content.Item().Row(row =>
                            {
                                row.Spacing(10);
                                if (ficha.PresentacionesVenta.Count > 0) row.RelativeItem().Element(box => ComposeFichaPresentationsSection(box, ficha));
                                if (hasVariantsSection) row.RelativeItem().Element(box => ComposeFichaVariantsSection(box, ficha.Variantes, imagenesVariantes));
                            });
                        content.Item().Element(container => ComposeFichaCombinedTechnical(container, ficha, hasPhysicalSection));
                        if (hasMultimediaSection)
                            content.Item().Element(container => ComposeFichaMultimediaSection(container, ficha.Multimedia));
                    });

                    page.Footer().PaddingTop(6).Row(footer =>
                    {
                        footer.RelativeItem().Text(ficha.Tipo == TipoServicio ? "CheckApp · Ficha técnica de servicio" : "CheckApp · Ficha técnica de producto").FontSize(7).FontColor(FichaMuted);
                        footer.ConstantItem(42).Text(text =>
                        {
                            text.DefaultTextStyle(style => style.FontSize(7).FontColor(FichaMuted));
                            text.CurrentPageNumber(); text.Span(" / "); text.TotalPages();
                        });
                        footer.ConstantItem(60).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)).FontSize(7).FontColor(FichaMuted);
                    });
                });
            }).GeneratePdf();
        }

        // PDF palette mirrors checkapp-theme.css; branding remains in the shared logo.
        private const string FichaInk = "#20304A";
        private const string FichaMuted = "#6B7280";
        private const string FichaSurface = "#F3F6F8";
        private const string FichaRule = "#D7E0EA";
        // Dominant orange sampled from the existing shared CheckApp logo.
        private const string FichaAccent = "#FF9231";

        private static void ComposeFichaServiceHero(IContainer container, ProductoServicioFichaTecnicaDto ficha, byte[]? image)
        {
            container.EnsureSpace(165).Row(row =>
            {
                row.RelativeItem(4.8f).PaddingRight(16).Height(155).Element(visual =>
                {
                    if (image != null)
                        visual.AlignMiddle().Image(image).FitArea();
                    else
                    {
                        string initials = string.Concat((ficha.Nombre ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Take(2).Select(word => word.Substring(0, 1))).ToUpperInvariant();
                        visual.Background(FichaSurface).Padding(16).AlignCenter().AlignMiddle().Column(fallback =>
                        {
                            fallback.Spacing(6);
                            fallback.Item().AlignCenter().Text(initials.Length > 0 ? initials : "S").SemiBold().FontSize(36).FontColor(FichaMuted);
                            fallback.Item().AlignCenter().Text(string.IsNullOrWhiteSpace(ficha.Categoria) ? "Servicio" : ficha.Categoria).FontSize(9).FontColor(FichaMuted);
                        });
                    }
                });
                row.RelativeItem(5.2f).AlignMiddle().Column(identity =>
                {
                    identity.Spacing(9);
                    if (!string.IsNullOrWhiteSpace(ficha.Categoria))
                        identity.Item().Text(ficha.Categoria.ToUpperInvariant()).FontSize(8).FontColor(FichaAccent);
                    identity.Item().Text(FichaTextOrDash(ficha.Nombre)).Bold().FontSize(23).LineHeight(1.05f);
                    identity.Item().Text($"Servicio {FichaTextOrDash(ficha.Codigo)}").FontSize(10).FontColor(FichaMuted);
                    identity.Item().Row(badges =>
                    {
                        badges.AutoItem().Background(FichaSurface).PaddingVertical(4).PaddingHorizontal(8)
                            .Text("SERVICIO").SemiBold().FontSize(7.5f);
                        badges.AutoItem().PaddingLeft(6).Border(0.5f).BorderColor(FichaRule).PaddingVertical(4).PaddingHorizontal(8)
                            .Text(text => { text.Span("● ").FontColor(ficha.Activo ? "#16A34A" : FichaMuted); text.Span(FichaTextOrDash(ficha.EstatusNombre).ToUpperInvariant()).SemiBold().FontSize(7.5f); });
                    });

                });
            });
        }

        private static void ComposeFichaServiceDescription(IContainer container, ProductoServicioFichaTecnicaDto ficha)
        {
            FichaCompactSection(container, "Descripción del servicio").PaddingTop(3).Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(9).LineHeight(1.2f));
                if (string.IsNullOrWhiteSpace(ficha.Descripcion)) text.Span("—");
                else AppendRichTextToPdf(text, ficha.Descripcion);
            });
        }

        private static void ComposeFichaServiceMetadata(IContainer container, ProductoServicioFichaTecnicaDto ficha)
        {
            var fields = new List<(string Label, string Value)>
            {
                ("Código", FichaTextOrDash(ficha.Codigo)),
                ("Tipo", FichaTextOrDash(ficha.TipoNombre)),
                ("Estatus", FichaTextOrDash(ficha.EstatusNombre)),
                ("Categoría", FichaTextOrDash(ficha.Categoria)),
                ("Unidad base", BuildUnidadLabel(ficha.UnidadMedida, ficha.UnidadAbreviatura)),
                ("Etiquetas", string.Join(" · ", ficha.Tags.Select(tag => FichaTextOrDash(tag.Nombre))))
            };
            if (!string.IsNullOrWhiteSpace(ficha.Marca)) fields.Add(("Marca", ficha.Marca));
            string collection = BuildColeccionLabel(ficha.ColeccionNumero, ficha.ColeccionNombre);
            if (!string.IsNullOrWhiteSpace(collection)) fields.Add(("Colección", collection));
            FichaCompactSection(container, "Información general").Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); });
                foreach (var field in fields)
                    table.Cell().PaddingVertical(5).PaddingRight(12).Column(value =>
                    {
                        value.Item().Text(field.Label).FontSize(8).FontColor(FichaMuted);
                        value.Item().Text(FichaTextOrDash(field.Value)).SemiBold().FontSize(10);
                    });
            });
        }

        private static void ComposeFichaGeneralSection(IContainer container, ProductoServicioFichaTecnicaDto ficha, byte[]? imagenPrincipal)
        {
            container.EnsureSpace(190).Row(row =>
            {
                row.RelativeItem(3.4f).PaddingRight(14).Height(190).Background(FichaSurface).CornerRadius(5).Padding(9).Element(image =>
                {
                    if (imagenPrincipal != null) image.Image(imagenPrincipal).FitArea();
                    else image.AlignCenter().AlignMiddle().Text("Sin imagen").FontColor(FichaMuted);
                });
                row.RelativeItem(6.6f).Column(details =>
                {
                    details.Spacing(5);
                    if (!string.IsNullOrWhiteSpace(ficha.Categoria)) details.Item().Text(ficha.Categoria.ToUpperInvariant()).FontSize(8).FontColor(FichaAccent);
                    details.Item().Text(FichaTextOrDash(ficha.Nombre)).Bold().FontSize(22).LineHeight(1.05f);
                    if (!string.IsNullOrWhiteSpace(ficha.Descripcion))
                        details.Item().Text(text => AppendRichTextToPdf(text, ficha.Descripcion));
                    var metadata = new List<(string Label, string Value)>
                    {
                        ("Código", FichaTextOrDash(ficha.Codigo)),
                        ("Tipo", FichaTextOrDash(ficha.TipoNombre)),
                        ("Estatus", FichaTextOrDash(ficha.EstatusNombre))
                    };
                    if (!string.IsNullOrWhiteSpace(ficha.Marca)) metadata.Add(("Marca", ficha.Marca));
                    string collection = BuildColeccionLabel(ficha.ColeccionNumero, ficha.ColeccionNombre);
                    if (!string.IsNullOrWhiteSpace(collection)) metadata.Add(("Colección", collection));
                    details.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); });
                        foreach (var field in metadata)
                            table.Cell().Padding(2).Background(FichaSurface).Border(0.4f).BorderColor(FichaRule).CornerRadius(4).Padding(5)
                                .Element(box => ComposeFichaGeneralField(box, field.Label, field.Value));
                    });
                    if (ficha.Tags.Any())
                    {
                        details.Item().Text("Etiquetas").FontSize(7).FontColor(FichaMuted);
                        details.Item().Inlined(chips =>
                        {
                            chips.Spacing(3);
                            foreach (var tag in ficha.Tags)
                                chips.Item().Background("#EEF2FF").CornerRadius(7).PaddingVertical(3).PaddingHorizontal(5)
                                    .Text(FichaTextOrDash(tag.Nombre)).FontSize(6.5f).FontColor("#1D4ED8");
                        });
                    }
                });
            });
        }

        private static void ComposeFichaGeneralField(IContainer container, string label, string value)
        {
            container.PaddingVertical(2).PaddingRight(8).Column(field =>
            {
                field.Item().Text(label).FontSize(7.5f).FontColor(FichaMuted);
                field.Item().Text(value).FontSize(9);
            });
        }

        // Keep the section title with its first content, while allowing long tables to flow.
        private static IContainer FichaCompactSection(IContainer container, string title)
        {
            IContainer body = null!;
            container.EnsureSpace(50).Border(0.5f).BorderColor(FichaRule).CornerRadius(5).Column(column =>
            {
                column.Item().Background(FichaSurface).PaddingVertical(6).PaddingHorizontal(8).Row(header =>
                {
                    header.ConstantItem(16).Height(16).Element(icon => ComposeFichaSectionIcon(icon, title));
                    header.RelativeItem().PaddingLeft(6).AlignMiddle().Text(title).SemiBold().FontSize(10);
                    if (title == "Precios y Costos" || title == "Precio y rentabilidad")
                        header.ConstantItem(65).AlignRight().AlignMiddle().Text("Valores en MXN").FontSize(6.5f).FontColor(FichaMuted);
                });
                body = column.Item().Padding(7);
            });
            return body;
        }

        private static void ComposeFichaSectionIcon(IContainer container, string title)
        {
            string path = title.Contains("Precio") ? "M8 2v12M11 4H6a2 2 0 0 0 0 4h4a2 2 0 0 1 0 4H4"
                : title == "Atributos" ? "M2 3h6l6 6-5 5-7-7zM5 5h.1"
                : title == "Variantes" ? "M8 2v12M2 8h12M4 4l8 8M4 12l8-8"
                : "M3 4h10v10H3zM5 2h6v4H5zM6 8h4M6 11h4";
            container.Svg($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 18 18'><rect width='18' height='18' rx='4' fill='{FichaAccent}'/><path d='{path}' transform='translate(1 1)' fill='none' stroke='white' stroke-width='1.3' stroke-linecap='round' stroke-linejoin='round'/></svg>");
        }

        private static void ComposeFichaMetricGrid(IContainer container, IReadOnlyCollection<(string Label, string Value)> rows, int columns)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(cols => { for (int i = 0; i < columns; i++) cols.RelativeColumn(); });
                foreach (var item in rows)
                    table.Cell().Element(box => ComposeFichaGeneralField(box, item.Label, FichaTextOrDash(item.Value)));
            });
        }

        private static void ComposeFichaCommercialSection(IContainer container, ProductoServicioFichaTecnicaDto ficha)
        {
            List<(string Label, string Value)> rows = new List<(string Label, string Value)>
            {
                ("Unidad Base", BuildUnidadLabel(ficha.UnidadMedida, ficha.UnidadAbreviatura)),
                ("Precio público", FichaFormatCurrency(ficha.PrecioPublico))
            };

            if (ficha.Costo.HasValue)
            {
                rows.Insert(1, ("Costo", FichaFormatCurrency(ficha.Costo.Value)));
            }

            if (ficha.PrecioComparacion.HasValue && ficha.PrecioComparacion.Value > 0)
            {
                rows.Add(("Precio de comparación", FichaFormatCurrency(ficha.PrecioComparacion.Value)));
            }

            if (ficha.Costo.HasValue)
            {
                decimal ganancia = ficha.PrecioPublico - ficha.Costo.Value;
                rows.Add(("Ganancia", FichaFormatCurrency(ganancia)));
                if (ficha.PrecioPublico > 0) rows.Add(("Margen", (ganancia / ficha.PrecioPublico * 100).ToString("0.00", CultureInfo.InvariantCulture) + "%"));
            }

            var order = new[] { "Precio público", "Costo", "Ganancia", "Margen", "Precio de comparación", "Unidad Base" };
            if (ficha.Tipo == TipoServicio) rows.RemoveAll(item => item.Label == "Unidad Base");
            rows = rows.OrderBy(item => Array.IndexOf(order, item.Label)).ToList();
            FichaCompactSection(container, ficha.Tipo == TipoServicio ? "Precio y rentabilidad" : "Precios y Costos").Row(cards =>
            {
                cards.Spacing(5);
                foreach (var metric in rows)
                {
                    bool main = metric.Label == "Precio público";
                    cards.RelativeItem(main ? 1.65f : 1).Background(main ? "#FFF4EA" : FichaSurface)
                        .Border(0.5f).BorderColor(main ? FichaAccent : FichaRule).CornerRadius(4).Padding(7).Column(field =>
                        {
                            field.Spacing(3);
                            field.Item().Text(metric.Label).FontSize(7).FontColor(main ? FichaAccent : FichaMuted);
                            field.Item().Text(metric.Value).SemiBold().FontSize(main ? 21 : 10);
                        });
                }
            });
        }

        private static void ComposeFichaPresentationsSection(IContainer container, ProductoServicioFichaTecnicaDto ficha)
        {
            FichaCompactSection(container, "Presentaciones de venta").Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.RelativeColumn(1.3f); cols.RelativeColumn(); cols.RelativeColumn(); });
                table.Header(header =>
                {
                    header.Cell().Element(x => FichaTableHeaderCell(x, "Venta"));
                    header.Cell().Element(x => FichaTableHeaderCell(x, "Equivale en inventario"));
                    header.Cell().Element(x => FichaTableHeaderCell(x, "Precio", true));
                });
                foreach (var item in ficha.PresentacionesVenta)
                {
                    string venta = item.CantidadVenta.ToString("0.####", CultureInfo.InvariantCulture) + " " + item.UnidadVenta + (EsPresentacionBase(item, ficha.IdUnidadMedida) ? " Base" : "");
                    table.Cell().Element(x => FichaTableBodyCell(x, venta));
                    table.Cell().Element(x => FichaTableBodyCell(x, item.EquivalenciaBase.ToString("0.####", CultureInfo.InvariantCulture) + " " + ficha.UnidadAbreviatura));
                    table.Cell().Element(x => FichaTableBodyCell(x, FichaFormatCurrency(item.Precio), true, true));
                }
            });
        }

        private static void ComposeFichaFiscalSection(IContainer container, ProductoServicioFichaTecnicaDto ficha)
        {
            var rows = GetFichaFiscalRows(ficha);
            if (rows.Count > 0) ComposeFichaMetricGrid(FichaCompactSection(container, "Información fiscal"), rows, 3);
        }

        private static List<(string Label, string Value)> GetFichaFiscalRows(ProductoServicioFichaTecnicaDto ficha)
        {
            List<(string Label, string Value)> rows = new List<(string Label, string Value)>();

            if (!string.IsNullOrWhiteSpace(ficha.ClaveProductoSat))
            {
                rows.Add(("Clave producto/servicio SAT", BuildSatLabel(ficha.ClaveProductoSat, ficha.ClaveProductoSatDescripcion)));
            }

            if (!string.IsNullOrWhiteSpace(ficha.ClaveUnidadSat))
            {
                rows.Add(("Clave unidad SAT", BuildSatLabel(ficha.ClaveUnidadSat, ficha.ClaveUnidadSatDescripcion)));
            }

            if (!string.IsNullOrWhiteSpace(ficha.ObjetoImpuesto))
            {
                // Representación del catálogo vigente; no depende del porcentaje de IVA.
                string objetoImpuesto = ficha.ObjetoImpuesto.Trim() switch
                {
                    "01" => "No",
                    "02" or "03" or "04" => "Sí",
                    _ => ""
                };
                if (objetoImpuesto.Length > 0) rows.Add(("Objeto de impuesto", objetoImpuesto));
            }

            if (ficha.PorcentajeIVA > 0)
            {
                rows.Add(("IVA", ficha.PorcentajeIVA.ToString("0.##", CultureInfo.InvariantCulture) + "%"));
            }

            if (!rows.Any())
            {
                return rows;
            }

            return rows;
        }

        private static void ComposeFichaPhysicalSection(IContainer container, ProductoServicioFichaTecnicaDto ficha)
        {
            var rows = GetFichaPhysicalRows(ficha);
            if (rows.Count > 0) ComposeFichaMetricGrid(FichaCompactSection(container, "Información física y logística"), rows, 3);
        }

        private static List<(string Label, string Value)> GetFichaPhysicalRows(ProductoServicioFichaTecnicaDto ficha)
        {
            List<(string Label, string Value)> rows = new List<(string Label, string Value)>();

            if (ficha.EsProductoFisico)
            {
                rows.Add(("Producto físico", "Sí"));
            }

            if (!string.IsNullOrWhiteSpace(ficha.PaqueteNombre))
            {
                rows.Add(("Paquete", ficha.PaqueteNombre.Trim()));
            }

            string tipoPaquete = BuildTipoPaqueteLabel(ficha.TipoPaquete);
            if (!string.IsNullOrWhiteSpace(tipoPaquete))
            {
                rows.Add(("Tipo de paquete", tipoPaquete));
            }

            rows.Add(("Peso del producto", FormatLogisticsWeightOrUnavailable(ficha.PesoKg)));
            rows.Add(("Peso vacío del empaque", FormatLogisticsWeightOrUnavailable(ficha.PaquetePesoEmpaqueVacioKg)));
            rows.Add(("Peso físico total", FormatLogisticsWeightOrUnavailable(ficha.PesoFisicoTotalKg)));
            rows.Add(("Dimensiones del paquete", BuildDimensionsLabel(ficha.PaqueteLargoCm, ficha.PaqueteAnchoCm, ficha.PaqueteAltoCm, "No disponible")));
            rows.Add(("Peso volumétrico", FormatLogisticsWeightOrUnavailable(ficha.PesoVolumetricoKg)));
            rows.Add(("Peso facturable", FormatLogisticsWeightOrUnavailable(ficha.PesoFacturableKg)));

            if (ficha.UsaNumeroSerie)
            {
                rows.Add(("Usa número de serie", "Sí"));
            }

            if (!rows.Any())
            {
                return rows;
            }

            return rows;
        }

        private static void ComposeFichaCombinedTechnical(IContainer container, ProductoServicioFichaTecnicaDto ficha, bool physical)
        {
            var fiscal = GetFichaFiscalRows(ficha);
            var logistics = physical ? GetFichaPhysicalRows(ficha) : new List<(string Label, string Value)>();
            if (fiscal.Count == 0 && logistics.Count == 0) return;
            FichaCompactSection(container, "Información fiscal, física y logística").Row(row =>
            {
                row.Spacing(10);
                if (fiscal.Count > 0) row.RelativeItem(1.2f).Element(box => ComposeFichaMetricGrid(box, fiscal, 1));
                if (logistics.Count > 0) row.RelativeItem(3).Element(box => ComposeFichaMetricGrid(box, logistics, 3));
            });
        }

        private static void ComposeFichaInventorySection(IContainer container, ProductoServicioFichaTecnicaDto ficha)
        {
            List<(string Label, string Value)> rows = new List<(string Label, string Value)>();

            if (ficha.ExistenciaActual.HasValue)
            {
                rows.Add(("Existencia actual", ficha.ExistenciaActual.Value.ToString("0.####", CultureInfo.InvariantCulture)));
            }

            if (ficha.ExistenciaMinima.HasValue)
            {
                rows.Add(("Existencia mínima", ficha.ExistenciaMinima.Value.ToString("0.####", CultureInfo.InvariantCulture)));
            }

            rows.Add(("Permite venta sin existencia", ficha.PermiteVentaSinExistencia ? "Sí" : "No"));

            FichaCompactSection(container, "Inventario").Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); });
                foreach (var item in rows)
                    table.Cell().Padding(2).Border(0.4f).BorderColor(FichaRule).CornerRadius(4).Padding(5).Column(metric =>
                    {
                        metric.Item().Text(item.Label == "Permite venta sin existencia" ? "Venta sin existencia" : item.Label).FontSize(7).FontColor(FichaMuted);
                        metric.Item().Text(item.Value).SemiBold().FontSize(item.Label == "Existencia actual" ? 16 : 10);
                    });
            });
        }

        private static void ComposeFichaAttributesSection(IContainer container, IReadOnlyCollection<ProductoServicioAtributoSeleccionDto> atributos)
        {
            var rows = atributos.Select(atributo => (
                Label: FichaTextOrDash(atributo.Nombre),
                Value: FichaTextOrDash(string.Join(", ", atributo.Valores.OrderBy(v => v.Orden).Select(v => v.Valor).Where(v => !string.IsNullOrWhiteSpace(v)))))).ToList();
            FichaCompactSection(container, "Atributos").Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(1.7f); });
                foreach (var item in rows)
                {
                    table.Cell().Element(box => FichaTableBodyCell(box, item.Label));
                    table.Cell().Element(box => FichaTableBodyCell(box, item.Value));
                }
            });
        }

        private static void ComposeFichaVariantsSection(IContainer container, IReadOnlyCollection<ProductoServicioVarianteDto> variantes, IReadOnlyDictionary<Guid, byte[]?> imagenesVariantes)
        {
            FichaCompactSection(container.EnsureSpace(90), "Variantes").Column(column =>
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f);
                            columns.ConstantColumn(36);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(x => FichaTableHeaderCell(x, "Variante"));
                            header.Cell().Element(x => FichaTableHeaderCell(x, "Imagen"));
                            header.Cell().Element(x => FichaTableHeaderCell(x, "Costo", true));
                            header.Cell().Element(x => FichaTableHeaderCell(x, "Precio", true));
                        });

                        foreach (ProductoServicioVarianteDto variante in variantes.OrderBy(v => v.Orden))
                        {
                            string descripcion = BuildVariantLabel(variante);
                            table.Cell().Element(x => FichaTableBodyCell(x, descripcion));
                            table.Cell().Element(cell =>
                            {
                                IContainer imageCell = cell
                                    .BorderBottom(1)
                                    .BorderColor(FichaRule)
                                    .Padding(3)
                                    .AlignCenter()
                                    .AlignMiddle();

                                if (imagenesVariantes.TryGetValue(variante.Id, out byte[]? imageBytes) && imageBytes != null)
                                {
                                    imageCell.Height(32).Image(imageBytes).FitArea();
                                }
                                else
                                {
                                    imageCell.Width(22).Height(26).Background(FichaSurface).CornerRadius(3).AlignCenter().AlignMiddle().Text("▧").FontSize(12).FontColor(FichaMuted);
                                }
                            });
                            table.Cell().Element(x => FichaTableBodyCell(x, variante.Costo.HasValue ? FichaFormatCurrency(variante.Costo.Value) : "—", true));
                            table.Cell().Element(x => FichaTableBodyCell(x, variante.PrecioPublico.HasValue ? FichaFormatCurrency(variante.PrecioPublico.Value) : "—", true, true));
                        }
                    });
                });
        }

        private static void ComposeFichaMultimediaSection(IContainer container, IReadOnlyCollection<ProductoServicioMultimediaDto> multimedia)
        {
            string fotos = string.Join(", ", multimedia.Where(item => item.Foto).Select(item => item.NombreOriginal).Where(name => !string.IsNullOrWhiteSpace(name)));
            string videos = string.Join(", ", multimedia.Where(item => item.Video).Select(item => item.NombreOriginal).Where(name => !string.IsNullOrWhiteSpace(name)));
            string documentos = string.Join(", ", multimedia.Where(item => item.Documento).Select(item => item.NombreOriginal).Where(name => !string.IsNullOrWhiteSpace(name)));

            List<(string Label, string Value)> rows = new List<(string Label, string Value)>();
            if (!string.IsNullOrWhiteSpace(fotos))
            {
                rows.Add(("Fotografías", fotos));
            }

            if (!string.IsNullOrWhiteSpace(videos))
            {
                rows.Add(("Video", videos));
            }

            if (!string.IsNullOrWhiteSpace(documentos))
            {
                rows.Add(("Documentos", documentos));
            }

            if (!rows.Any())
            {
                return;
            }

            ComposeFichaInfoTable(container, "Evidencia y multimedia", rows);
        }

        private static void ComposeFichaInfoTable(IContainer container, string title, IReadOnlyCollection<(string Label, string Value)> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }

            container
                .Border(0.5f)
                .BorderColor(FichaRule)
                .CornerRadius(4)
                .Padding(7)
                .Column(column =>
                {
                    column.Spacing(4);
                    column.Item().Text(title).SemiBold().FontSize(10).FontColor("#0F172A");
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(2.4f);
                        });

                        foreach ((string label, string value) in rows)
                        {
                            table.Cell().Element(x => FichaTableLabelCell(x, label));
                            table.Cell().Element(x => FichaTableBodyCell(x, value));
                        }
                    });
                });
        }

        private static void FichaTableHeaderCell(IContainer container, string text, bool alignRight = false)
        {
            IContainer cell = container.Background(FichaSurface).PaddingVertical(5).PaddingHorizontal(5);
            cell = alignRight ? cell.AlignRight() : cell.AlignLeft();
            cell.Text(text).SemiBold().FontSize(7).FontColor(FichaInk);
        }

        private static void FichaTableLabelCell(IContainer container, string text)
        {
            container.BorderBottom(0.5f).BorderColor(FichaRule).PaddingVertical(3).PaddingHorizontal(5)
                .Text(text).FontSize(8).FontColor(FichaMuted);
        }

        private static void FichaTableBodyCell(IContainer container, string text, bool alignRight = false, bool emphasized = false)
        {
            IContainer body = container.BorderBottom(0.5f).BorderColor(FichaRule).PaddingVertical(3).PaddingHorizontal(5).AlignMiddle();
            body = alignRight ? body.AlignRight() : body.AlignLeft();
            var value = body.Text(FichaTextOrDash(text)).FontSize(8).FontColor(FichaInk);
            if (emphasized) value.SemiBold();
        }

        private static string FichaTextOrDash(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        }

        private static string FichaFormatCurrency(decimal value)
        {
            return value.ToString("C2", CultureInfo.GetCultureInfo("es-MX"));
        }

        private static string BuildUnidadLabel(string unidad, string abreviatura)
        {
            string normalizedUnit = (unidad ?? string.Empty).Trim();
            string normalizedAbbreviation = (abreviatura ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedUnit))
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(normalizedAbbreviation)
                ? normalizedUnit
                : $"{normalizedUnit} ({normalizedAbbreviation})";
        }

        private static string BuildColeccionLabel(string numero, string nombre)
        {
            string normalizedNumero = (numero ?? string.Empty).Trim();
            string normalizedNombre = (nombre ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedNumero))
            {
                return normalizedNombre;
            }

            if (string.IsNullOrWhiteSpace(normalizedNombre))
            {
                return normalizedNumero;
            }

            return $"{normalizedNumero} · {normalizedNombre}";
        }

        private static string BuildSatLabel(string clave, string descripcion)
        {
            string normalizedKey = (clave ?? string.Empty).Trim();
            string normalizedDescription = (descripcion ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedDescription))
            {
                return normalizedKey;
            }

            return $"{normalizedKey} - {normalizedDescription}";
        }

        private static string BuildTipoPaqueteLabel(string tipo)
        {
            return (tipo ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "caja" => "Caja",
                "sobre" => "Sobre",
                "flexible" => "Paquete flexible",
                _ => string.Empty
            };
        }

        private static string BuildDimensionsLabel(decimal? largo, decimal? ancho, decimal? alto, string fallback = "")
        {
            List<string> items = new List<string>();
            if (largo.HasValue)
            {
                items.Add($"L {largo.Value.ToString("0.####", CultureInfo.InvariantCulture)} cm");
            }

            if (ancho.HasValue)
            {
                items.Add($"A {ancho.Value.ToString("0.####", CultureInfo.InvariantCulture)} cm");
            }

            if (alto.HasValue)
            {
                items.Add($"H {alto.Value.ToString("0.####", CultureInfo.InvariantCulture)} cm");
            }

            return items.Any() ? string.Join(" · ", items) : fallback;
        }

        private static string FormatLogisticsWeightOrUnavailable(decimal? value)
        {
            return value.HasValue
                ? $"{value.Value.ToString("0.00", CultureInfo.InvariantCulture)} kg"
                : "No disponible";
        }

        private static string BuildVariantLabel(ProductoServicioVarianteDto variante)
        {
            string nombre = (variante.Nombre ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                return nombre;
            }

            string composed = string.Join(" / ", variante.Valores.OrderBy(value => value.Orden).Select(value => value.Valor).Where(value => !string.IsNullOrWhiteSpace(value)));
            if (!string.IsNullOrWhiteSpace(composed))
            {
                return composed;
            }

            return FichaTextOrDash(variante.Sku);
        }

        private static string BuildFichaTecnicaFileName(string codigo, string nombre)
        {
            string raw = $"FichaTecnica_{codigo}_{nombre}".Trim('_').Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = "FichaTecnica";
            }

            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                raw = raw.Replace(invalid, '_');
            }

            while (raw.Contains("__", StringComparison.Ordinal))
            {
                raw = raw.Replace("__", "_", StringComparison.Ordinal);
            }

            return $"{raw.Trim('_')}.pdf";
        }

        private static byte[]? LoadSharedCheckAppLogo()
        {
            DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                string candidate = Path.Combine(current.FullName, "inspector", "checklist", "wwwroot", "assets", "media", "logos", "checkapp2.png");
                if (System.IO.File.Exists(candidate))
                {
                    return System.IO.File.ReadAllBytes(candidate);
                }

                current = current.Parent;
            }

            return null;
        }

        private static async Task<byte[]?> LoadRemoteImageBytesAsync(string url)
        {
            string normalized = (url ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            try
            {
                using HttpClient client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(20)
                };
                return await client.GetByteArrayAsync(normalized);
            }
            catch
            {
                return null;
            }
        }

        private static Guid ReadGuid(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) ? reader.GetGuid(ordinal) : Guid.Empty;
        }

        private static Guid? ReadNullableGuid(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
        }

        private static byte ReadByte(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) ? reader.GetByte(ordinal) : (byte)0;
        }

        private static byte? ReadNullableByte(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetByte(ordinal);
        }

        private static bool ReadBool(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
        }

        private static bool? ReadNullableBool(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
        }

        private static string ReadString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int index = 0; index < reader.FieldCount; index++)
            {
                if (string.Equals(reader.GetName(index), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static decimal ReadDecimal(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
        }

        private static decimal? ReadNullableDecimal(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
        }

        private static int ReadInt(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static long ReadLong(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0L : Convert.ToInt64(reader.GetValue(ordinal));
        }

        private static DateTime ReadDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }

        private sealed class RequestContext
        {
            public Guid IdEmpresa { get; init; }
            public string EmpresaStorageKey { get; init; } = string.Empty;
            public TenantDatabaseDescriptor TenantDatabase { get; init; } = null!;
        }

        private sealed class ProductoServicioValidationException : Exception
        {
            public ProductoServicioValidationException(string message) : base(message)
            {
            }
        }

        private sealed class SignedProxyContext
        {
            public string UserId { get; init; } = string.Empty;
            public Guid IdEmpresa { get; init; }
            public string EmpresaStorageKey { get; init; } = string.Empty;
            public Guid? UsuarioId { get; init; }
        }

        private sealed class NormalizedProductoServicioRequest
        {
            public Guid? Id { get; set; }
            public byte Tipo { get; set; }
            public string Codigo { get; set; } = string.Empty;
            public string Tag { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public Guid IdCategoria { get; set; }
            public Guid? IdMarca { get; set; }
            public Guid IdUnidadMedida { get; set; }
            public Guid? IdColeccion { get; set; }
            public Guid? IdPaquete { get; set; }
            public decimal? Costo { get; set; }
            public decimal PrecioPublico { get; set; }
            public decimal? PrecioComparacion { get; set; }
            public decimal? PrecioUnitarioMonto { get; set; }
            public decimal? PrecioUnitarioCantidadTotal { get; set; }
            public string PrecioUnitarioUnidadTotal { get; set; } = string.Empty;
            public decimal? PrecioUnitarioBaseCantidad { get; set; }
            public string PrecioUnitarioUnidad { get; set; } = string.Empty;
            public string PrecioUnitarioUnidadBase { get; set; } = string.Empty;
            public string ObjetoImpuesto { get; set; } = string.Empty;
            public decimal PorcentajeIVA { get; set; }
            public string ClaveProductoSat { get; set; } = string.Empty;
            public string ClaveUnidadSat { get; set; } = string.Empty;
            public bool EsProductoFisico { get; set; }
            public decimal? PesoKg { get; set; }
            public decimal? LargoCm { get; set; }
            public decimal? AnchoCm { get; set; }
            public decimal? AltoCm { get; set; }
            public bool UsaNumeroSerie { get; set; }
            public bool CausaInventario { get; set; }
            public bool PermiteVentaSinExistencia { get; set; }
            public decimal? ExistenciaInicial { get; set; }
            public decimal? ExistenciaMinima { get; set; }
            public bool Activo { get; set; }
            public ProductoServicioImagenGuardarRequest? ImagenPrincipal { get; set; }
            public bool EliminarImagenPrincipal { get; set; }
            public List<ProductoServicioTagGuardarRequest> Tags { get; set; } = new List<ProductoServicioTagGuardarRequest>();
            public List<ProductoServicioAtributoGuardarRequest> Atributos { get; set; } = new List<ProductoServicioAtributoGuardarRequest>();
            public List<ProductoServicioOpcionVarianteGuardarRequest> OpcionesVariante { get; set; } = new List<ProductoServicioOpcionVarianteGuardarRequest>();
            public List<ProductoServicioVarianteGuardarRequest> Variantes { get; set; } = new List<ProductoServicioVarianteGuardarRequest>();
            public List<ProductoServicioMultimediaGuardarRequest> Multimedia { get; set; } = new List<ProductoServicioMultimediaGuardarRequest>();
            public List<ProductoServicioPresentacionVentaGuardarRequest> PresentacionesVenta { get; set; } = new List<ProductoServicioPresentacionVentaGuardarRequest>();
        }

        private sealed class ProductoServicioSnapshot
        {
            public Guid Id { get; set; }
            public Guid IdEmpresa { get; set; }
            public byte Tipo { get; set; }
            public string Codigo { get; set; } = string.Empty;
            public Guid IdCategoria { get; set; }
            public Guid? IdMarca { get; set; }
            public Guid IdUnidadMedida { get; set; }
            public decimal PrecioPublico { get; set; }
            public Guid? IdColeccion { get; set; }
            public Guid? IdPaquete { get; set; }
            public bool EsProductoFisico { get; set; }
            public bool CausaInventario { get; set; }
            public bool PermiteVentaSinExistencia { get; set; }
            public bool Activo { get; set; }
            public string ImagenUrl { get; set; } = string.Empty;
            public string ImagenNombre { get; set; } = string.Empty;
            public Guid? IdExistencia { get; set; }
        }

        private sealed class TemporalImageTokenPayload
        {
            public string NombreOriginal { get; set; } = string.Empty;
            public string NombreAlmacenado { get; set; } = string.Empty;
            public string Extension { get; set; } = string.Empty;
            public string MimeType { get; set; } = string.Empty;
            public string UrlFirebase { get; set; } = string.Empty;
            public string FolderName { get; set; } = string.Empty;
            public long PesoBytes { get; set; }
            public string TipoMultimedia { get; set; } = string.Empty;
            public DateTime ExpiraUtc { get; set; }
        }

        private sealed class UploadedImagePayload
        {
            public string FolderName { get; set; } = string.Empty;
            public string NombreOriginal { get; set; } = string.Empty;
            public string NombreAlmacenado { get; set; } = string.Empty;
            public string Extension { get; set; } = string.Empty;
            public string MimeType { get; set; } = string.Empty;
            public string UrlFirebase { get; set; } = string.Empty;
            public long PesoBytes { get; set; }
        }

        private sealed class FirebaseCleanupItem
        {
            public string FolderName { get; set; } = string.Empty;
            public string StoredName { get; set; } = string.Empty;
        }

        private sealed class VariantOptionReference
        {
            public Guid Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public Dictionary<string, Guid> Valores { get; set; } = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class UnitPriceUnitMeta
        {
            public UnitPriceUnitMeta(string family, decimal factor)
            {
                Family = family;
                Factor = factor;
            }

            public string Family { get; }
            public decimal Factor { get; }
        }

        private enum ImageOperationMode
        {
            None,
            NewImage,
            Remove
        }

        private sealed class PreparedImageOperation
        {
            public ImageOperationMode Mode { get; set; }
            public UploadedImagePayload? UploadedImage { get; set; }
            public FirebaseCleanupItem? TemporalCleanup { get; set; }
            public FirebaseCleanupItem? NewImageCleanup { get; set; }

            public static PreparedImageOperation None() => new PreparedImageOperation { Mode = ImageOperationMode.None };

            public static PreparedImageOperation ForRemove() => new PreparedImageOperation { Mode = ImageOperationMode.Remove };

            public static PreparedImageOperation ForNewImage(UploadedImagePayload uploadedImage, FirebaseCleanupItem temporalCleanup)
            {
                return new PreparedImageOperation
                {
                    Mode = ImageOperationMode.NewImage,
                    UploadedImage = uploadedImage,
                    TemporalCleanup = temporalCleanup,
                    NewImageCleanup = new FirebaseCleanupItem
                    {
                        FolderName = uploadedImage.FolderName,
                        StoredName = uploadedImage.NombreAlmacenado
                    }
                };
            }
        }

        private sealed class ResolvedImageMutation
        {
            public string ImagenUrl { get; set; } = string.Empty;
            public string ImagenNombre { get; set; } = string.Empty;
            public FirebaseCleanupItem? PreviousImageCleanup { get; set; }
        }

        private sealed class PreparedMultimediaOperation
        {
            public List<ProductoServicioMultimediaDto> FinalItems { get; set; } = new List<ProductoServicioMultimediaDto>();
            public HashSet<Guid> ExistingItemIds { get; set; } = new HashSet<Guid>();
            public List<FirebaseCleanupItem> TemporalCleanups { get; set; } = new List<FirebaseCleanupItem>();
            public List<FirebaseCleanupItem> NewFileCompensations { get; set; } = new List<FirebaseCleanupItem>();
        }

        private sealed class PreparedVariantSyncResult
        {
            public List<FirebaseCleanupItem> TemporalCleanups { get; set; } = new List<FirebaseCleanupItem>();
            public List<FirebaseCleanupItem> FinalCleanups { get; set; } = new List<FirebaseCleanupItem>();
            public List<FirebaseCleanupItem> NewFileCompensations { get; set; } = new List<FirebaseCleanupItem>();
        }
    }
}

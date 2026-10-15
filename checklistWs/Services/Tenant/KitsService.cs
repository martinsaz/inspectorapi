using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using checklistWs.Models.Kits;

namespace checklistWs.Services.Tenant;

public sealed class KitsDomainException : Exception
{
    public KitsDomainException(string code, string message, object? details = null) : base(message)
    {
        Code = code;
        Details = details;
    }

    public string Code { get; }
    public object? Details { get; }
}

public interface IKitsService
{
    Task<IReadOnlyList<KitListItemResponse>> ListAsync(KitActor actor, KitListQuery query, CancellationToken cancellationToken = default);
    Task<KitDetailResponse> GetAsync(KitActor actor, Guid idKit, CancellationToken cancellationToken = default);
    Task<KitDetailResponse?> FindByOperationKeyAsync(KitActor actor, string operationKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KitOptionResponse>> OptionsAsync(KitActor actor, string search, byte? kitType, CancellationToken cancellationToken = default);
    Task<KitPriceResolutionResponse> ResolvePriceAsync(KitActor actor, KitPriceResolutionRequest request, CancellationToken cancellationToken = default);
    Task<KitOperationResponse> CreateAsync(KitActor actor, KitCreateRequest request, CancellationToken cancellationToken = default);
    Task<KitOperationResponse> UpdateAsync(KitActor actor, Guid idKit, KitUpdateRequest request, CancellationToken cancellationToken = default);
    Task<KitOperationResponse> ArchiveAsync(KitActor actor, Guid idKit, CancellationToken cancellationToken = default);
    Task<KitOperationResponse> ReactivateAsync(KitActor actor, Guid idKit, CancellationToken cancellationToken = default);
    Task<KitOperationResponse> AddComponentAsync(KitActor actor, Guid idKit, KitComponentCreateRequest request, CancellationToken cancellationToken = default);
    Task<KitOperationResponse> UpdateComponentAsync(KitActor actor, Guid idKit, Guid idComponent, KitComponentUpdateRequest request, CancellationToken cancellationToken = default);
    Task<KitOperationResponse> ArchiveComponentAsync(KitActor actor, Guid idKit, Guid idComponent, CancellationToken cancellationToken = default);
    Task<KitOperationResponse> ReactivateComponentAsync(KitActor actor, Guid idKit, Guid idComponent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KitHistoryResponse>> HistoryAsync(KitActor actor, Guid idKit, CancellationToken cancellationToken = default);
}

public static class KitsPolicy
{
    public static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    public static decimal Amount(decimal quantity, decimal unitPrice) => Money(quantity * unitPrice);

    public static string NormalizeName(string? value)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (normalized.Length is < 1 or > 150) throw Error("NOMBRE_INVALIDO", "El nombre del Kit es obligatorio y admite hasta 150 caracteres.");
        return normalized;
    }

    public static void ValidateKitType(byte type)
    {
        if (type is not (KitsConstants.TipoProducto or KitsConstants.TipoServicio or KitsConstants.TipoMixto))
            throw Error("TIPO_KIT_INVALIDO", "El tipo de Kit no es válido.");
    }

    public static void ValidatePrice(decimal price, string code = "PRECIO_KIT_INVALIDO")
    {
        if (price < 0m || decimal.Round(price, 2, MidpointRounding.AwayFromZero) != price)
            throw Error(code, "El precio debe ser mayor o igual a cero y tener como máximo dos decimales.");
    }

    public static void ValidateQuantity(decimal quantity, bool permitsDecimals)
    {
        if (quantity <= 0m || DecimalScale(quantity) > 4)
            throw Error("CANTIDAD_INVALIDA", "La cantidad debe ser positiva y tener como máximo cuatro decimales.");
        if (!permitsDecimals && decimal.Truncate(quantity) != quantity)
            throw Error("CANTIDAD_DECIMAL_NO_PERMITIDA", "La unidad del producto requiere una cantidad entera positiva.");
    }

    public static void ValidateTypeCompatibility(byte kitType, KitSource source)
    {
        ValidateKitType(kitType);
        bool isService = source.TipoProductoServicio == KitsConstants.TipoServicio;
        bool allowed = kitType == KitsConstants.TipoMixto ||
            (kitType == KitsConstants.TipoProducto && !isService) ||
            (kitType == KitsConstants.TipoServicio && isService);
        if (!allowed)
            throw Error("COMPONENTE_INCOMPATIBLE_TIPO", "El componente no es compatible con el tipo del Kit.", new { source.Codigo, source.Nombre, source.IdentidadVisible, Motivo = isService ? "SERVICIO_EN_KIT_PRODUCTO" : "PRODUCTO_EN_KIT_SERVICIO" });
    }

    public static void ValidateIdentityShape(KitIdentity identity)
    {
        if (identity.IdProductoServicio == Guid.Empty) throw Error("IDENTIDAD_INVALIDA", "La identidad del componente no es válida.");
        bool valid = identity.TipoIdentidad switch
        {
            KitsConstants.IdentidadProducto or KitsConstants.IdentidadServicio => !identity.IdVariante.HasValue && !identity.IdPresentacionVenta.HasValue,
            KitsConstants.IdentidadVariante => identity.IdVariante.HasValue && identity.IdVariante.Value != Guid.Empty && !identity.IdPresentacionVenta.HasValue,
            KitsConstants.IdentidadPresentacionVenta => !identity.IdVariante.HasValue && identity.IdPresentacionVenta.HasValue && identity.IdPresentacionVenta.Value != Guid.Empty,
            _ => false
        };
        if (!valid) throw Error("IDENTIDAD_INVALIDA", "La combinación de producto, variante o presentación no es válida.");
    }

    private static int DecimalScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0x7F;
    private static KitsDomainException Error(string code, string message, object? details = null) => new(code, message, details);
}

public sealed class KitsService : IKitsService
{
    private readonly ITenantSqlConnectionFactory _connections;
    private readonly TenantDatabaseDescriptor _descriptor;
    private readonly IListaPreciosService _prices;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public KitsService(ITenantSqlConnectionFactory connections, TenantDatabaseDescriptor descriptor, IListaPreciosService prices)
    {
        _connections = connections;
        _descriptor = descriptor;
        _prices = prices;
    }

    public async Task<IReadOnlyList<KitListItemResponse>> ListAsync(KitActor actor, KitListQuery query, CancellationToken cancellationToken = default)
    {
        query ??= new KitListQuery();
        if (query.Tipo.HasValue) KitsPolicy.ValidateKitType(query.Tipo.Value);
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        StringBuilder sql = new("SELECT id FROM dbo.Kits WHERE idEmpresa=@Empresa");
        if (query.Activo.HasValue) sql.Append(" AND Activo=@Activo");
        if (query.Tipo.HasValue) sql.Append(" AND Tipo=@Tipo");
        if (!string.IsNullOrWhiteSpace(query.Busqueda)) sql.Append(" AND Nombre LIKE @Search ESCAPE N'~'");
        sql.Append(" ORDER BY Nombre,id");
        using SqlCommand command = new(sql.ToString(), connection);
        command.Parameters.Add("@Empresa", SqlDbType.UniqueIdentifier).Value = actor.IdEmpresa;
        if (query.Activo.HasValue) command.Parameters.Add("@Activo", SqlDbType.Bit).Value = query.Activo.Value;
        if (query.Tipo.HasValue) command.Parameters.Add("@Tipo", SqlDbType.TinyInt).Value = query.Tipo.Value;
        if (!string.IsNullOrWhiteSpace(query.Busqueda)) command.Parameters.Add("@Search", SqlDbType.NVarChar, 452).Value = $"%{EscapeLike(query.Busqueda.Trim())}%";
        List<Guid> ids = new();
        using (SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken)) ids.Add(reader.GetGuid(0));
        List<KitListItemResponse> result = new(ids.Count);
        foreach (Guid id in ids) result.Add(await GetAsync(actor, id, cancellationToken));
        return result;
    }

    private static string EscapeLike(string value)
        => value.Replace("~", "~~", StringComparison.Ordinal)
            .Replace("%", "~%", StringComparison.Ordinal)
            .Replace("_", "~_", StringComparison.Ordinal)
            .Replace("[", "~[", StringComparison.Ordinal);

    public async Task<KitDetailResponse> GetAsync(KitActor actor, Guid idKit, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        return await LoadDetailAsync(connection, null, actor.IdEmpresa, idKit, false, cancellationToken)
            ?? throw new KitsDomainException("KIT_NO_ENCONTRADO", "El Kit no existe.");
    }

    public async Task<KitDetailResponse?> FindByOperationKeyAsync(KitActor actor, string operationKey, CancellationToken cancellationToken = default)
    {
        Guid identityKey = OperationIdentity(actor.IdEmpresa, RequireOperationKey(operationKey));
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        using SqlCommand command = new("SELECT id FROM dbo.Kits WHERE idEmpresa=@Empresa AND identityKey=@Key", connection);
        Add(command, "@Empresa", actor.IdEmpresa);
        Add(command, "@Key", identityKey);
        object? id = await command.ExecuteScalarAsync(cancellationToken);
        return id is Guid value ? await LoadDetailAsync(connection, null, actor.IdEmpresa, value, false, cancellationToken) : null;
    }

    public async Task<IReadOnlyList<KitOptionResponse>> OptionsAsync(KitActor actor, string search, byte? kitType, CancellationToken cancellationToken = default)
    {
        if (kitType.HasValue) KitsPolicy.ValidateKitType(kitType.Value);
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        const string sql = @"
WITH Opciones AS (
 SELECT CAST(CASE WHEN ps.Tipo=2 THEN 2 ELSE 1 END AS tinyint) TipoIdentidad,ps.id IdProductoServicio,CAST(NULL AS uniqueidentifier) IdVariante,CAST(NULL AS uniqueidentifier) IdPresentacionVenta,
        ISNULL(ps.Codigo,N'') Codigo,ps.Nombre,CASE WHEN ps.Tipo=2 THEN N'Servicio' ELSE N'Producto base' END IdentidadVisible,
        CAST(CASE WHEN ps.Tipo=2 THEN 1 ELSE ISNULL(um.PermiteDecimales,0) END AS bit) UnidadPermiteDecimales,ps.Activo FuenteActiva,ps.Tipo
 FROM dbo.ProductosServicios ps LEFT JOIN dbo.ProductosServiciosUnidadesMedida um ON um.idEmpresa=ps.idEmpresa AND um.id=ps.idUnidadMedida
 WHERE ps.idEmpresa=@Empresa
 UNION ALL
 SELECT 3,ps.id,v.id,NULL,ISNULL(NULLIF(v.Sku,N''),ISNULL(ps.Codigo,N'')),CONCAT(ps.Nombre,N' · ',v.Nombre),CONCAT(N'Variante · ',v.Nombre),
        CAST(ISNULL(um.PermiteDecimales,0) AS bit),CAST(CASE WHEN ps.Activo=1 AND v.Activo=1 THEN 1 ELSE 0 END AS bit),ps.Tipo
 FROM dbo.ProductosServiciosVariantes v INNER JOIN dbo.ProductosServicios ps ON ps.idEmpresa=v.idEmpresa AND ps.id=v.idProductoServicio
 LEFT JOIN dbo.ProductosServiciosUnidadesMedida um ON um.idEmpresa=ps.idEmpresa AND um.id=ps.idUnidadMedida WHERE v.idEmpresa=@Empresa AND ps.Tipo=1
 UNION ALL
 SELECT 4,ps.id,NULL,pv.id,ISNULL(ps.Codigo,N''),CONCAT(ps.Nombre,N' · ',pv.Nombre),CONCAT(N'Presentación venta · ',pv.Nombre),
        CAST(ISNULL(um.PermiteDecimales,0) AS bit),CAST(CASE WHEN ps.Activo=1 AND pv.Activo=1 THEN 1 ELSE 0 END AS bit),ps.Tipo
 FROM dbo.ProductosServiciosPresentacionesVenta pv INNER JOIN dbo.ProductosServicios ps ON ps.idEmpresa=pv.idEmpresa AND ps.id=pv.idProductoServicio
 LEFT JOIN dbo.ProductosServiciosUnidadesMedida um ON um.idEmpresa=ps.idEmpresa AND um.id=ps.idUnidadMedida WHERE pv.idEmpresa=@Empresa AND ps.Tipo=1
)
SELECT TipoIdentidad,IdProductoServicio,IdVariante,IdPresentacionVenta,Codigo,Nombre,IdentidadVisible,UnidadPermiteDecimales,FuenteActiva
FROM Opciones WHERE FuenteActiva=1 AND (@TipoKit IS NULL OR @TipoKit=3 OR (@TipoKit=1 AND Tipo=1) OR (@TipoKit=2 AND Tipo=2))
AND (@Search=N'' OR Codigo LIKE @Pattern OR Nombre LIKE @Pattern) ORDER BY Nombre,TipoIdentidad;";
        using SqlCommand command = new(sql, connection);
        command.Parameters.Add("@Empresa", SqlDbType.UniqueIdentifier).Value = actor.IdEmpresa;
        command.Parameters.Add("@TipoKit", SqlDbType.TinyInt).Value = kitType.HasValue ? kitType.Value : DBNull.Value;
        string normalized = (search ?? string.Empty).Trim();
        command.Parameters.Add("@Search", SqlDbType.NVarChar, 150).Value = normalized;
        command.Parameters.Add("@Pattern", SqlDbType.NVarChar, 152).Value = $"%{normalized}%";
        List<KitOptionResponse> result = new();
        using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(new KitOptionResponse
        {
            TipoIdentidad = Convert.ToByte(reader["TipoIdentidad"]), IdProductoServicio = (Guid)reader["IdProductoServicio"],
            IdVariante = NullableGuid(reader["IdVariante"]), IdPresentacionVenta = NullableGuid(reader["IdPresentacionVenta"]),
            Codigo = Convert.ToString(reader["Codigo"]) ?? string.Empty, Nombre = Convert.ToString(reader["Nombre"]) ?? string.Empty,
            IdentidadVisible = Convert.ToString(reader["IdentidadVisible"]) ?? string.Empty,
            UnidadPermiteDecimales = Convert.ToBoolean(reader["UnidadPermiteDecimales"]), FuenteActiva = Convert.ToBoolean(reader["FuenteActiva"])
        });
        return result;
    }

    public async Task<KitPriceResolutionResponse> ResolvePriceAsync(KitActor actor, KitPriceResolutionRequest request, CancellationToken cancellationToken = default)
    {
        KitIdentity identity = Identity(request.TipoIdentidad, request.IdProductoServicio, request.IdVariante, request.IdPresentacionVenta);
        await using SqlConnection connection = await OpenAsync(cancellationToken);
        KitSource source = await LoadSourceAsync(connection, null, actor.IdEmpresa, identity, true, cancellationToken);
        ListaPreciosResolutionResult resolved = await _prices.ResolverPrecioAsync(actor.IdEmpresa, new ListaPreciosResolverRequest
        {
            Nivel = 1, TipoIdentidad = identity.TipoIdentidad, IdProductoServicio = identity.IdProductoServicio,
            IdVariante = identity.IdVariante, IdPresentacionVenta = identity.IdPresentacionVenta, RequiereActivo = true
        }, cancellationToken);
        if (!resolved.Resuelto || !resolved.PrecioFinal.HasValue)
            return new KitPriceResolutionResponse { Resuelto = false, Code = resolved.CodigoResolucion, Message = resolved.Mensaje, Origen = resolved.Origen, Nivel = 1, CorrelationId = resolved.CorrelationId };
        _ = source;
        return new KitPriceResolutionResponse { Resuelto = true, Code = resolved.CodigoResolucion, Message = resolved.Mensaje, PrecioPropuesto = KitsPolicy.Money(resolved.PrecioFinal.Value), Origen = resolved.Origen, Nivel = 1, CorrelationId = resolved.CorrelationId };
    }

    public async Task<KitOperationResponse> CreateAsync(KitActor actor, KitCreateRequest request, CancellationToken cancellationToken = default)
    {
        for (int attempt=0;;attempt++)
        {
            try{return await CreateOnceAsync(actor,request,cancellationToken);}
            catch(SqlException ex) when(ex.Number==1205&&attempt<2){await Task.Delay(25*(attempt+1),cancellationToken);}
        }
    }

    private async Task<KitOperationResponse> CreateOnceAsync(KitActor actor, KitCreateRequest request, CancellationToken cancellationToken)
    {
        if (request == null) throw new KitsDomainException("REQUEST_INVALIDO", "La solicitud es inválida.");
        string name = KitsPolicy.NormalizeName(request.Nombre); KitsPolicy.ValidateKitType(request.Tipo); KitsPolicy.ValidatePrice(request.PrecioKit);
        ValidateImage(request.ImagenUrl, request.ImagenNombre);
        request.Componentes ??= new List<KitComponentCreateRequest>();
        if (request.Activo && request.Componentes.Count == 0) throw new KitsDomainException("KIT_SIN_COMPONENTES", "Un Kit activo debe tener al menos un componente activo.");
        Guid identityKey = OperationIdentity(actor.IdEmpresa, RequireOperationKey(request.OperationKey));
        List<PreparedComponent> prepared = new();
        foreach (KitComponentCreateRequest component in request.Componentes)
            prepared.Add(await PrepareCreateComponentAsync(actor, request.Tipo, component, cancellationToken));
        EnsureNoDuplicates(prepared.Select(x => x.Identity));

        await using SqlConnection connection = await OpenAsync(cancellationToken);
        using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            KitDetailResponse? replay = await LoadByIdentityKeyAsync(connection, transaction, actor.IdEmpresa, identityKey, cancellationToken);
            if (replay != null)
            {
                EnsureCreateReplayMatches(replay, name, request, prepared);
                transaction.Commit();
                return Success(replay.Id, identityKey, true, "El alta ya había sido procesada.");
            }
            await EnsureNameAvailableAsync(connection, transaction, actor.IdEmpresa, name, null, cancellationToken);
            foreach (PreparedComponent item in prepared)
                _ = await LoadSourceAsync(connection, transaction, actor.IdEmpresa, item.Identity, true, cancellationToken);
            Guid idKit = request.PreparedId.HasValue && request.PreparedId.Value != Guid.Empty ? request.PreparedId.Value : Guid.NewGuid(); Guid correlation = identityKey;
            using (SqlCommand insert = new(@"INSERT dbo.Kits(id,idEmpresa,identityKey,Nombre,Tipo,PrecioKit,ImagenUrl,ImagenNombre,Activo,FechaCreacion,FechaActualizacion,idUsuarioCreacion,idUsuarioActualizacion)
VALUES(@Id,@Empresa,@Identity,@Nombre,@Tipo,@Precio,@ImagenUrl,@ImagenNombre,@Activo,SYSUTCDATETIME(),SYSUTCDATETIME(),@Usuario,@Usuario)", connection, transaction))
            {
                Add(insert, "@Id", idKit); Add(insert, "@Empresa", actor.IdEmpresa); Add(insert, "@Identity", identityKey); Add(insert, "@Nombre", name);
                Add(insert, "@Tipo", request.Tipo); Add(insert, "@Precio", KitsPolicy.Money(request.PrecioKit)); AddNullable(insert, "@ImagenUrl", Clean(request.ImagenUrl)); AddNullable(insert, "@ImagenNombre", Clean(request.ImagenNombre)); Add(insert, "@Activo", request.Activo); AddNullable(insert, "@Usuario", actor.IdUsuario);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
            await InsertHistoryAsync(connection, transaction, actor, idKit, null, 1, "ALTA", null, KitSnapshot(name, request.Tipo, request.PrecioKit, request.ImagenUrl, request.ImagenNombre, request.Activo), correlation, cancellationToken);
            foreach (PreparedComponent item in prepared)
            {
                Guid componentId = Guid.NewGuid(); Guid componentIdentity = OperationIdentity(actor.IdEmpresa, RequireOperationKey(item.Request.OperationKey));
                await InsertComponentAsync(connection, transaction, actor, idKit, componentId, componentIdentity, item, cancellationToken);
                await InsertHistoryAsync(connection, transaction, actor, idKit, componentId, 2, "AGREGAR_COMPONENTE", null, ComponentSnapshot(item, true), correlation, cancellationToken);
            }
            transaction.Commit();
            return Success(idKit, correlation, false, "Kit creado.");
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { SafeRollback(transaction); throw Duplicate(ex); }
        catch { SafeRollback(transaction); throw; }
    }

    public async Task<KitOperationResponse> UpdateAsync(KitActor actor, Guid idKit, KitUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new KitsDomainException("REQUEST_INVALIDO", "La solicitud es inválida.");
        string name = KitsPolicy.NormalizeName(request.Nombre); KitsPolicy.ValidateKitType(request.Tipo); KitsPolicy.ValidatePrice(request.PrecioKit); ValidateImage(request.ImagenUrl, request.ImagenNombre);
        request.Componentes ??= new List<KitComponentEditRequest>();
        if (request.Activo && request.Componentes.Count == 0) throw new KitsDomainException("KIT_SIN_COMPONENTES", "Un Kit activo debe tener al menos un componente activo.");
        if (request.Componentes.Any(x => x == null)) throw new KitsDomainException("COMPONENTE_INVALIDO", "La solicitud contiene un componente inválido.");
        List<(KitComponentEditRequest Edit, PreparedComponent Prepared)> prepared = new();
        foreach (KitComponentEditRequest component in request.Componentes)
        {
            KitsPolicy.ValidatePrice(component.PrecioUnitario, "PRECIO_COMPONENTE_INVALIDO");
            KitComponentCreateRequest create = new()
            {
                OperationKey = string.IsNullOrWhiteSpace(component.OperationKey) ? $"edit-{component.Id:D}" : component.OperationKey,
                TipoIdentidad = component.TipoIdentidad,
                IdProductoServicio = component.IdProductoServicio,
                IdVariante = component.IdVariante,
                IdPresentacionVenta = component.IdPresentacionVenta,
                Cantidad = component.Cantidad,
                PrecioUnitario = component.PrecioUnitario
            };
            prepared.Add((component, await PrepareCreateComponentAsync(actor, request.Tipo, create, cancellationToken)));
        }
        EnsureNoDuplicates(prepared.Select(x => x.Prepared.Identity));
        Guid[] repeatedIds = request.Componentes.Where(x => x.Id.HasValue).GroupBy(x => x.Id!.Value).Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
        if (repeatedIds.Length > 0) throw new KitsDomainException("COMPONENTE_DUPLICADO", "La solicitud repite componentes existentes.");
        await using SqlConnection connection = await OpenAsync(cancellationToken); using SqlTransaction tx = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            KitDetailResponse before = await RequireKitAsync(connection, tx, actor.IdEmpresa, idKit, false, cancellationToken);
            if (request.Activo) await EnsureNameAvailableAsync(connection, tx, actor.IdEmpresa, name, idKit, cancellationToken);
            Dictionary<Guid, KitComponentResponse> current = before.Componentes.ToDictionary(x => x.Id);
            foreach ((KitComponentEditRequest edit, PreparedComponent item) in prepared)
            {
                _ = await LoadSourceAsync(connection, tx, actor.IdEmpresa, item.Identity, true, cancellationToken);
                if (edit.Id.HasValue && (!current.TryGetValue(edit.Id.Value, out KitComponentResponse? existing) || !existing.Activo))
                    throw new KitsDomainException("COMPONENTE_NO_ENCONTRADO", "El componente no existe o ya no está activo.");
            }

            Guid correlation = Guid.NewGuid();
            HashSet<Guid> retained = prepared.Where(x => x.Edit.Id.HasValue).Select(x => x.Edit.Id!.Value).ToHashSet();
            foreach (KitComponentResponse removed in before.Componentes.Where(x => x.Activo && !retained.Contains(x.Id)))
            {
                using SqlCommand archive = new(@"UPDATE dbo.KitsComponentes SET Activo=0,FechaArchivado=SYSUTCDATETIME(),idUsuarioArchivado=@Usuario,FechaActualizacion=SYSUTCDATETIME(),idUsuarioActualizacion=@Usuario WHERE idEmpresa=@Empresa AND idKit=@Kit AND id=@Id", connection, tx);
                AddNullable(archive,"@Usuario",actor.IdUsuario);Add(archive,"@Empresa",actor.IdEmpresa);Add(archive,"@Kit",idKit);Add(archive,"@Id",removed.Id);await archive.ExecuteNonQueryAsync(cancellationToken);
                await InsertHistoryAsync(connection,tx,actor,idKit,removed.Id,2,"QUITAR_COMPONENTE",ComponentSnapshot(removed),new { Activo=false },correlation,cancellationToken);
            }

            foreach ((KitComponentEditRequest edit, PreparedComponent item) in prepared)
            {
                if (edit.Id.HasValue)
                {
                    KitComponentResponse old = current[edit.Id.Value];
                    await EnsureComponentAvailableAsync(connection,tx,actor.IdEmpresa,idKit,item.Identity,edit.Id.Value,cancellationToken);
                    using SqlCommand componentUpdate = new(@"UPDATE dbo.KitsComponentes SET TipoIdentidad=@Tipo,idProductoServicio=@Producto,idVariante=@Variante,idPresentacionVenta=@Presentacion,Cantidad=@Cantidad,PrecioUnitario=@Precio,FechaActualizacion=SYSUTCDATETIME(),idUsuarioActualizacion=@Usuario WHERE idEmpresa=@Empresa AND idKit=@Kit AND id=@Id",connection,tx);
                    BindIdentity(componentUpdate,item.Identity);Add(componentUpdate,"@Cantidad",item.Request.Cantidad);Add(componentUpdate,"@Precio",item.Price);AddNullable(componentUpdate,"@Usuario",actor.IdUsuario);Add(componentUpdate,"@Empresa",actor.IdEmpresa);Add(componentUpdate,"@Kit",idKit);Add(componentUpdate,"@Id",edit.Id.Value);await componentUpdate.ExecuteNonQueryAsync(cancellationToken);
                    bool changed = old.TipoIdentidad != item.Identity.TipoIdentidad || old.IdProductoServicio != item.Identity.IdProductoServicio || old.IdVariante != item.Identity.IdVariante || old.IdPresentacionVenta != item.Identity.IdPresentacionVenta || old.Cantidad != item.Request.Cantidad || old.PrecioUnitario != item.Price;
                    if (changed) await InsertHistoryAsync(connection,tx,actor,idKit,edit.Id.Value,2,old.PrecioUnitario != item.Price ? "CAMBIO_PRECIO_COMPONENTE" : "EDITAR_COMPONENTE",ComponentSnapshot(old),ComponentSnapshot(item,true),correlation,cancellationToken);
                }
                else
                {
                    await EnsureComponentAvailableAsync(connection,tx,actor.IdEmpresa,idKit,item.Identity,null,cancellationToken);
                    Guid componentId=Guid.NewGuid();Guid componentIdentity=OperationIdentity(actor.IdEmpresa,RequireOperationKey(item.Request.OperationKey));
                    await InsertComponentAsync(connection,tx,actor,idKit,componentId,componentIdentity,item,cancellationToken);
                    await InsertHistoryAsync(connection,tx,actor,idKit,componentId,2,"AGREGAR_COMPONENTE",null,ComponentSnapshot(item,true),correlation,cancellationToken);
                }
            }

            using SqlCommand update = new(@"UPDATE dbo.Kits SET Nombre=@Nombre,Tipo=@Tipo,PrecioKit=@Precio,ImagenUrl=@ImagenUrl,ImagenNombre=@ImagenNombre,Activo=@Activo,FechaArchivado=CASE WHEN @Activo=1 THEN NULL ELSE COALESCE(FechaArchivado,SYSUTCDATETIME()) END,idUsuarioArchivado=CASE WHEN @Activo=1 THEN NULL ELSE COALESCE(idUsuarioArchivado,@Usuario) END,FechaActualizacion=SYSUTCDATETIME(),idUsuarioActualizacion=@Usuario WHERE idEmpresa=@Empresa AND id=@Id", connection, tx);
            Add(update, "@Nombre", name); Add(update, "@Tipo", request.Tipo); Add(update, "@Precio", KitsPolicy.Money(request.PrecioKit)); AddNullable(update, "@ImagenUrl", Clean(request.ImagenUrl)); AddNullable(update, "@ImagenNombre", Clean(request.ImagenNombre)); Add(update,"@Activo",request.Activo); AddNullable(update, "@Usuario", actor.IdUsuario); Add(update, "@Empresa", actor.IdEmpresa); Add(update, "@Id", idKit);
            await update.ExecuteNonQueryAsync(cancellationToken);
            string operation = before.Activo != request.Activo ? request.Activo ? "REACTIVAR" : "BAJA" : before.Tipo != request.Tipo ? "CAMBIO_TIPO" : before.PrecioKit != request.PrecioKit ? "CAMBIO_PRECIO_KIT" : "EDICION";
            await InsertHistoryAsync(connection, tx, actor, idKit, null, 1, operation, KitSnapshot(before.Nombre, before.Tipo, before.PrecioKit, before.ImagenUrl, before.ImagenNombre, before.Activo), KitSnapshot(name, request.Tipo, request.PrecioKit, request.ImagenUrl, request.ImagenNombre, request.Activo), correlation, cancellationToken);
            tx.Commit(); return Success(idKit, correlation, false, "Kit actualizado.");
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { SafeRollback(tx); throw Duplicate(ex); } catch { SafeRollback(tx); throw; }
    }

    public Task<KitOperationResponse> ArchiveAsync(KitActor actor, Guid idKit, CancellationToken cancellationToken = default) => SetKitStateAsync(actor, idKit, false, cancellationToken);
    public Task<KitOperationResponse> ReactivateAsync(KitActor actor, Guid idKit, CancellationToken cancellationToken = default) => SetKitStateAsync(actor, idKit, true, cancellationToken);

    public async Task<KitOperationResponse> AddComponentAsync(KitActor actor, Guid idKit, KitComponentCreateRequest request, CancellationToken cancellationToken = default)
    {
        for(int attempt=0;;attempt++)
        {
            try{return await AddComponentOnceAsync(actor,idKit,request,cancellationToken);}
            catch(SqlException ex) when(ex.Number==1205&&attempt<2){await Task.Delay(25*(attempt+1),cancellationToken);}
        }
    }

    private async Task<KitOperationResponse> AddComponentOnceAsync(KitActor actor, Guid idKit, KitComponentCreateRequest request, CancellationToken cancellationToken)
    {
        Guid identityKey = OperationIdentity(actor.IdEmpresa, RequireOperationKey(request.OperationKey));
        await using SqlConnection connection = await OpenAsync(cancellationToken); using SqlTransaction tx = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            KitDetailResponse kit = await RequireKitAsync(connection, tx, actor.IdEmpresa, idKit, true, cancellationToken);
            KitComponentResponse? replay = await LoadComponentByIdentityKeyAsync(connection, tx, actor.IdEmpresa, idKit, identityKey, cancellationToken);
            if (replay != null) { tx.Commit(); return Success(replay.Id, identityKey, true, "El componente ya había sido agregado."); }
            PreparedComponent prepared = await PrepareCreateComponentAsync(actor, kit.Tipo, request, cancellationToken);
            _ = await LoadSourceAsync(connection, tx, actor.IdEmpresa, prepared.Identity, true, cancellationToken);
            await EnsureComponentAvailableAsync(connection, tx, actor.IdEmpresa, idKit, prepared.Identity, null, cancellationToken);
            Guid id = Guid.NewGuid(); await InsertComponentAsync(connection, tx, actor, idKit, id, identityKey, prepared, cancellationToken);
            await InsertHistoryAsync(connection, tx, actor, idKit, id, 2, "AGREGAR_COMPONENTE", null, ComponentSnapshot(prepared, true), identityKey, cancellationToken);
            tx.Commit(); return Success(id, identityKey, false, "Componente agregado.");
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { SafeRollback(tx); throw Duplicate(ex); } catch { SafeRollback(tx); throw; }
    }

    public async Task<KitOperationResponse> UpdateComponentAsync(KitActor actor, Guid idKit, Guid idComponent, KitComponentUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new KitsDomainException("REQUEST_INVALIDO", "La solicitud es inválida.");
        KitsPolicy.ValidatePrice(request.PrecioUnitario, "PRECIO_COMPONENTE_INVALIDO");
        KitIdentity identity = Identity(request.TipoIdentidad, request.IdProductoServicio, request.IdVariante, request.IdPresentacionVenta);
        await using SqlConnection connection = await OpenAsync(cancellationToken); using SqlTransaction tx = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            KitDetailResponse kit = await RequireKitAsync(connection, tx, actor.IdEmpresa, idKit, true, cancellationToken);
            KitComponentResponse before = kit.Componentes.SingleOrDefault(x => x.Id == idComponent) ?? throw new KitsDomainException("COMPONENTE_NO_ENCONTRADO", "El componente no existe.");
            if (!before.Activo) throw new KitsDomainException("COMPONENTE_INACTIVO", "El componente archivado no puede editarse.");
            KitSource source = await LoadSourceAsync(connection, tx, actor.IdEmpresa, identity, true, cancellationToken); KitsPolicy.ValidateTypeCompatibility(kit.Tipo, source); KitsPolicy.ValidateQuantity(request.Cantidad, source.TipoProductoServicio == KitsConstants.TipoServicio || source.UnidadPermiteDecimales);
            await EnsureComponentAvailableAsync(connection, tx, actor.IdEmpresa, idKit, identity, idComponent, cancellationToken);
            using SqlCommand update = new(@"UPDATE dbo.KitsComponentes SET TipoIdentidad=@Tipo,idProductoServicio=@Producto,idVariante=@Variante,idPresentacionVenta=@Presentacion,Cantidad=@Cantidad,PrecioUnitario=@Precio,FechaActualizacion=SYSUTCDATETIME(),idUsuarioActualizacion=@Usuario WHERE idEmpresa=@Empresa AND idKit=@Kit AND id=@Id", connection, tx);
            BindIdentity(update, identity); Add(update, "@Cantidad", request.Cantidad); Add(update, "@Precio", KitsPolicy.Money(request.PrecioUnitario)); AddNullable(update, "@Usuario", actor.IdUsuario); Add(update, "@Empresa", actor.IdEmpresa); Add(update, "@Kit", idKit); Add(update, "@Id", idComponent); await update.ExecuteNonQueryAsync(cancellationToken);
            Guid correlation = Guid.NewGuid(); string op = before.PrecioUnitario != request.PrecioUnitario ? "CAMBIO_PRECIO_COMPONENTE" : "EDITAR_COMPONENTE";
            await InsertHistoryAsync(connection, tx, actor, idKit, idComponent, 2, op, ComponentSnapshot(before), ComponentSnapshot(identity, request.Cantidad, request.PrecioUnitario, source, true), correlation, cancellationToken);
            tx.Commit(); return Success(idComponent, correlation, false, "Componente actualizado.");
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { SafeRollback(tx); throw Duplicate(ex); } catch { SafeRollback(tx); throw; }
    }

    public Task<KitOperationResponse> ArchiveComponentAsync(KitActor actor, Guid idKit, Guid idComponent, CancellationToken cancellationToken = default) => SetComponentStateAsync(actor, idKit, idComponent, false, cancellationToken);
    public Task<KitOperationResponse> ReactivateComponentAsync(KitActor actor, Guid idKit, Guid idComponent, CancellationToken cancellationToken = default) => SetComponentStateAsync(actor, idKit, idComponent, true, cancellationToken);

    public async Task<IReadOnlyList<KitHistoryResponse>> HistoryAsync(KitActor actor, Guid idKit, CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken); _ = await LoadDetailAsync(connection, null, actor.IdEmpresa, idKit, false, cancellationToken) ?? throw new KitsDomainException("KIT_NO_ENCONTRADO", "El Kit no existe.");
        using SqlCommand command = new("SELECT id,idKit,idKitComponente,TipoEntidad,Operacion,DatosAntes,DatosDespues,idUsuario,CorrelationId,FechaUtc FROM dbo.KitsHistorial WHERE idEmpresa=@Empresa AND idKit=@Kit ORDER BY FechaUtc DESC,id DESC", connection);
        Add(command, "@Empresa", actor.IdEmpresa); Add(command, "@Kit", idKit); List<KitHistoryResponse> result = new(); using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(new KitHistoryResponse { Id=(Guid)reader["id"],IdKit=(Guid)reader["idKit"],IdKitComponente=NullableGuid(reader["idKitComponente"]),TipoEntidad=Convert.ToByte(reader["TipoEntidad"]),Operacion=Convert.ToString(reader["Operacion"])??string.Empty,DatosAntes=NullableString(reader["DatosAntes"]),DatosDespues=NullableString(reader["DatosDespues"]),IdUsuario=NullableGuid(reader["idUsuario"]),CorrelationId=(Guid)reader["CorrelationId"],FechaUtc=Convert.ToDateTime(reader["FechaUtc"])});
        return result;
    }

    private async Task<KitOperationResponse> SetKitStateAsync(KitActor actor, Guid idKit, bool active, CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await OpenAsync(cancellationToken); using SqlTransaction tx = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            KitDetailResponse kit = await RequireKitAsync(connection, tx, actor.IdEmpresa, idKit, false, cancellationToken);
            if (kit.Activo == active) { tx.Commit(); return Success(idKit, Guid.Empty, true, active ? "El Kit ya estaba activo." : "El Kit ya estaba archivado."); }
            if (active)
            {
                await EnsureNameAvailableAsync(connection, tx, actor.IdEmpresa, kit.Nombre, idKit, cancellationToken);
                KitComponentResponse[] components = kit.Componentes.Where(x => x.Activo).ToArray();
                if (components.Length == 0) throw new KitsDomainException("KIT_SIN_COMPONENTES", "El Kit no puede reactivarse sin componentes activos.");
                foreach (KitComponentResponse item in components)
                {
                    KitIdentity identity = Identity(item.TipoIdentidad,item.IdProductoServicio,item.IdVariante,item.IdPresentacionVenta);
                    KitSource source = await LoadSourceAsync(connection,tx,actor.IdEmpresa,identity,true,cancellationToken); KitsPolicy.ValidateTypeCompatibility(kit.Tipo,source);
                }
            }
            using SqlCommand command = new(active ? @"UPDATE dbo.Kits SET Activo=1,FechaArchivado=NULL,idUsuarioArchivado=NULL,FechaActualizacion=SYSUTCDATETIME(),idUsuarioActualizacion=@Usuario WHERE idEmpresa=@Empresa AND id=@Id" : @"UPDATE dbo.Kits SET Activo=0,FechaArchivado=SYSUTCDATETIME(),idUsuarioArchivado=@Usuario,FechaActualizacion=SYSUTCDATETIME(),idUsuarioActualizacion=@Usuario WHERE idEmpresa=@Empresa AND id=@Id",connection,tx);
            AddNullable(command,"@Usuario",actor.IdUsuario);Add(command,"@Empresa",actor.IdEmpresa);Add(command,"@Id",idKit);await command.ExecuteNonQueryAsync(cancellationToken);
            Guid correlation=Guid.NewGuid();await InsertHistoryAsync(connection,tx,actor,idKit,null,1,active?"REACTIVAR":"BAJA",new { kit.Activo },new { Activo=active },correlation,cancellationToken);
            tx.Commit();return Success(idKit,correlation,false,active?"Kit reactivado.":"Kit archivado.");
        }
        catch(SqlException ex) when(ex.Number is 2601 or 2627){SafeRollback(tx);throw Duplicate(ex);}catch{SafeRollback(tx);throw;}
    }

    private async Task<KitOperationResponse> SetComponentStateAsync(KitActor actor,Guid idKit,Guid idComponent,bool active,CancellationToken cancellationToken)
    {
        await using SqlConnection connection=await OpenAsync(cancellationToken);using SqlTransaction tx=connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            KitDetailResponse kit=await RequireKitAsync(connection,tx,actor.IdEmpresa,idKit,false,cancellationToken);KitComponentResponse component=kit.Componentes.SingleOrDefault(x=>x.Id==idComponent)??throw new KitsDomainException("COMPONENTE_NO_ENCONTRADO","El componente no existe.");
            if(component.Activo==active){tx.Commit();return Success(idComponent,Guid.Empty,true,active?"El componente ya estaba activo.":"El componente ya estaba archivado.");}
            KitIdentity identity=Identity(component.TipoIdentidad,component.IdProductoServicio,component.IdVariante,component.IdPresentacionVenta);
            if(active){KitSource source=await LoadSourceAsync(connection,tx,actor.IdEmpresa,identity,true,cancellationToken);KitsPolicy.ValidateTypeCompatibility(kit.Tipo,source);await EnsureComponentAvailableAsync(connection,tx,actor.IdEmpresa,idKit,identity,idComponent,cancellationToken);}
            else if(kit.Activo&&kit.Componentes.Count(x=>x.Activo)<=1)throw new KitsDomainException("KIT_SIN_COMPONENTES","Un Kit activo debe conservar al menos un componente activo.");
            using SqlCommand command=new(active?@"UPDATE dbo.KitsComponentes SET Activo=1,FechaArchivado=NULL,idUsuarioArchivado=NULL,FechaActualizacion=SYSUTCDATETIME(),idUsuarioActualizacion=@Usuario WHERE idEmpresa=@Empresa AND idKit=@Kit AND id=@Id":@"UPDATE dbo.KitsComponentes SET Activo=0,FechaArchivado=SYSUTCDATETIME(),idUsuarioArchivado=@Usuario,FechaActualizacion=SYSUTCDATETIME(),idUsuarioActualizacion=@Usuario WHERE idEmpresa=@Empresa AND idKit=@Kit AND id=@Id",connection,tx);
            AddNullable(command,"@Usuario",actor.IdUsuario);Add(command,"@Empresa",actor.IdEmpresa);Add(command,"@Kit",idKit);Add(command,"@Id",idComponent);await command.ExecuteNonQueryAsync(cancellationToken);
            Guid correlation=Guid.NewGuid();await InsertHistoryAsync(connection,tx,actor,idKit,idComponent,2,active?"REACTIVAR_COMPONENTE":"BAJA_COMPONENTE",ComponentSnapshot(component),new{Activo=active},correlation,cancellationToken);
            tx.Commit();return Success(idComponent,correlation,false,active?"Componente reactivado.":"Componente archivado.");
        }
        catch(SqlException ex)when(ex.Number is 2601 or 2627){SafeRollback(tx);throw Duplicate(ex);}catch{SafeRollback(tx);throw;}
    }

    private async Task<PreparedComponent> PrepareCreateComponentAsync(KitActor actor,byte kitType,KitComponentCreateRequest request,CancellationToken cancellationToken)
    {
        if(request==null)throw new KitsDomainException("COMPONENTE_INVALIDO","El componente es inválido.");
        _=RequireOperationKey(request.OperationKey);KitIdentity identity=Identity(request.TipoIdentidad,request.IdProductoServicio,request.IdVariante,request.IdPresentacionVenta);
        await using SqlConnection connection=await OpenAsync(cancellationToken);KitSource source=await LoadSourceAsync(connection,null,actor.IdEmpresa,identity,true,cancellationToken);KitsPolicy.ValidateTypeCompatibility(kitType,source);KitsPolicy.ValidateQuantity(request.Cantidad,source.TipoProductoServicio==KitsConstants.TipoServicio||source.UnidadPermiteDecimales);
        KitPriceResolutionResponse resolution=await ResolvePriceAsync(actor,new KitPriceResolutionRequest{TipoIdentidad=identity.TipoIdentidad,IdProductoServicio=identity.IdProductoServicio,IdVariante=identity.IdVariante,IdPresentacionVenta=identity.IdPresentacionVenta},cancellationToken);
        if(!resolution.Resuelto||!resolution.PrecioPropuesto.HasValue)throw new KitsDomainException("PRECIO_NO_RESOLUBLE","No existe un precio resoluble para el componente.",new{resolution.Code,resolution.Message});
        decimal price=resolution.PrecioPropuesto.Value;
        if(request.PrecioUnitario.HasValue){KitsPolicy.ValidatePrice(request.PrecioUnitario.Value,"PRECIO_COMPONENTE_INVALIDO");price=KitsPolicy.Money(request.PrecioUnitario.Value);}
        return new PreparedComponent(request,identity,source,price);
    }

    private async Task<KitSource> LoadSourceAsync(SqlConnection connection,SqlTransaction? tx,Guid company,KitIdentity identity,bool requireActive,CancellationToken ct)
    {
        KitsPolicy.ValidateIdentityShape(identity);string sql=identity.TipoIdentidad switch
        {
            KitsConstants.IdentidadProducto or KitsConstants.IdentidadServicio=>@"SELECT ps.Tipo,ISNULL(ps.Codigo,N'') Codigo,ps.Nombre,CASE WHEN ps.Tipo=2 THEN N'Servicio' ELSE N'Producto base' END Visible,CAST(CASE WHEN ps.Tipo=2 THEN 1 ELSE ISNULL(um.PermiteDecimales,0) END AS bit) Decimales,ps.Activo ProductoActivo,ps.Activo IdentidadActiva FROM dbo.ProductosServicios ps LEFT JOIN dbo.ProductosServiciosUnidadesMedida um ON um.idEmpresa=ps.idEmpresa AND um.id=ps.idUnidadMedida WHERE ps.idEmpresa=@Empresa AND ps.id=@Producto",
            KitsConstants.IdentidadVariante=>@"SELECT ps.Tipo,ISNULL(NULLIF(v.Sku,N''),ISNULL(ps.Codigo,N'')) Codigo,CONCAT(ps.Nombre,N' · ',v.Nombre) Nombre,CONCAT(N'Variante · ',v.Nombre) Visible,CAST(ISNULL(um.PermiteDecimales,0) AS bit) Decimales,ps.Activo ProductoActivo,v.Activo IdentidadActiva FROM dbo.ProductosServiciosVariantes v INNER JOIN dbo.ProductosServicios ps ON ps.idEmpresa=v.idEmpresa AND ps.id=v.idProductoServicio LEFT JOIN dbo.ProductosServiciosUnidadesMedida um ON um.idEmpresa=ps.idEmpresa AND um.id=ps.idUnidadMedida WHERE v.idEmpresa=@Empresa AND v.id=@Variante AND ps.id=@Producto",
            KitsConstants.IdentidadPresentacionVenta=>@"SELECT ps.Tipo,ISNULL(ps.Codigo,N'') Codigo,CONCAT(ps.Nombre,N' · ',pv.Nombre) Nombre,CONCAT(N'Presentación venta · ',pv.Nombre) Visible,CAST(ISNULL(um.PermiteDecimales,0) AS bit) Decimales,ps.Activo ProductoActivo,pv.Activo IdentidadActiva FROM dbo.ProductosServiciosPresentacionesVenta pv INNER JOIN dbo.ProductosServicios ps ON ps.idEmpresa=pv.idEmpresa AND ps.id=pv.idProductoServicio LEFT JOIN dbo.ProductosServiciosUnidadesMedida um ON um.idEmpresa=ps.idEmpresa AND um.id=ps.idUnidadMedida WHERE pv.idEmpresa=@Empresa AND pv.id=@Presentacion AND ps.id=@Producto",
            _=>throw new KitsDomainException("IDENTIDAD_INVALIDA","La identidad del componente no es válida.")
        };
        using SqlCommand command=new(sql,connection,tx);Add(command,"@Empresa",company);Add(command,"@Producto",identity.IdProductoServicio);if(identity.IdVariante.HasValue)Add(command,"@Variante",identity.IdVariante.Value);if(identity.IdPresentacionVenta.HasValue)Add(command,"@Presentacion",identity.IdPresentacionVenta.Value);
        using SqlDataReader reader=await command.ExecuteReaderAsync(ct);if(!await reader.ReadAsync(ct))throw new KitsDomainException("FUENTE_NO_ENCONTRADA","La fuente no existe, no pertenece al tenant o no corresponde al producto indicado.");
        byte actualType=Convert.ToByte(reader["Tipo"]);if((identity.TipoIdentidad==KitsConstants.IdentidadServicio)!=(actualType==KitsConstants.TipoServicio)||identity.TipoIdentidad is KitsConstants.IdentidadVariante or KitsConstants.IdentidadPresentacionVenta&&actualType!=KitsConstants.TipoProducto)throw new KitsDomainException("TIPO_IDENTIDAD_INVALIDO","El tipo declarado no corresponde a la fuente real.");
        KitSource source=new(identity,actualType,Convert.ToString(reader["Codigo"])??string.Empty,Convert.ToString(reader["Nombre"])??string.Empty,Convert.ToString(reader["Visible"])??string.Empty,Convert.ToBoolean(reader["Decimales"]),Convert.ToBoolean(reader["ProductoActivo"]),Convert.ToBoolean(reader["IdentidadActiva"]));
        if(requireActive&&!source.FuenteActiva)throw new KitsDomainException("FUENTE_INACTIVA","No se puede agregar o reactivar una fuente inactiva.",new{source.Codigo,source.Nombre,source.IdentidadVisible,source.MotivoInactivo});return source;
    }

    private async Task<KitDetailResponse?> LoadDetailAsync(SqlConnection connection,SqlTransaction? tx,Guid company,Guid idKit,bool lockRow,CancellationToken ct)
    {
        string lockHint=lockRow?" WITH (UPDLOCK,HOLDLOCK)":string.Empty;using SqlCommand head=new($"SELECT id,identityKey,Nombre,Tipo,PrecioKit,ImagenUrl,ImagenNombre,Activo,FechaCreacion,FechaActualizacion,FechaArchivado FROM dbo.Kits{lockHint} WHERE idEmpresa=@Empresa AND id=@Id",connection,tx);Add(head,"@Empresa",company);Add(head,"@Id",idKit);
        KitDetailResponse detail;using(SqlDataReader reader=await head.ExecuteReaderAsync(ct)){if(!await reader.ReadAsync(ct))return null;detail=new KitDetailResponse{Id=(Guid)reader["id"],IdentityKey=(Guid)reader["identityKey"],Nombre=Convert.ToString(reader["Nombre"])??string.Empty,Tipo=Convert.ToByte(reader["Tipo"]),PrecioKit=KitsPolicy.Money(Convert.ToDecimal(reader["PrecioKit"])),ImagenUrl=NullableString(reader["ImagenUrl"]),ImagenNombre=NullableString(reader["ImagenNombre"]),Activo=Convert.ToBoolean(reader["Activo"]),FechaCreacion=Convert.ToDateTime(reader["FechaCreacion"]),FechaActualizacion=Convert.ToDateTime(reader["FechaActualizacion"]),FechaArchivado=NullableDate(reader["FechaArchivado"]) };}
        using SqlCommand comps=new(@"SELECT c.id,c.identityKey,c.TipoIdentidad,c.idProductoServicio,c.idVariante,c.idPresentacionVenta,c.Cantidad,c.PrecioUnitario,c.Activo,c.FechaArchivado,
ps.Tipo,ISNULL(ps.Codigo,N'') BaseCodigo,ps.Nombre BaseNombre,ps.Activo ProductoActivo,CAST(CASE WHEN ps.Tipo=2 THEN 1 ELSE ISNULL(um.PermiteDecimales,0) END AS bit) Decimales,
v.Sku VarianteCodigo,v.Nombre VarianteNombre,v.Activo VarianteActiva,pv.Nombre PresentacionNombre,pv.Activo PresentacionActiva
FROM dbo.KitsComponentes c INNER JOIN dbo.ProductosServicios ps ON ps.idEmpresa=c.idEmpresa AND ps.id=c.idProductoServicio
LEFT JOIN dbo.ProductosServiciosUnidadesMedida um ON um.idEmpresa=ps.idEmpresa AND um.id=ps.idUnidadMedida
LEFT JOIN dbo.ProductosServiciosVariantes v ON v.idEmpresa=c.idEmpresa AND v.id=c.idVariante AND v.idProductoServicio=c.idProductoServicio
LEFT JOIN dbo.ProductosServiciosPresentacionesVenta pv ON pv.idEmpresa=c.idEmpresa AND pv.id=c.idPresentacionVenta AND pv.idProductoServicio=c.idProductoServicio
WHERE c.idEmpresa=@Empresa AND c.idKit=@Kit ORDER BY c.FechaCreacion,c.id",connection,tx);Add(comps,"@Empresa",company);Add(comps,"@Kit",idKit);List<KitComponentResponse> items=new();using(SqlDataReader r=await comps.ExecuteReaderAsync(ct)){while(await r.ReadAsync(ct)){byte ti=Convert.ToByte(r["TipoIdentidad"]);bool childActive=ti switch{KitsConstants.IdentidadVariante=>r["VarianteActiva"]!=DBNull.Value&&Convert.ToBoolean(r["VarianteActiva"]),KitsConstants.IdentidadPresentacionVenta=>r["PresentacionActiva"]!=DBNull.Value&&Convert.ToBoolean(r["PresentacionActiva"]),_=>Convert.ToBoolean(r["ProductoActivo"])};bool productActive=Convert.ToBoolean(r["ProductoActivo"]);string baseName=Convert.ToString(r["BaseNombre"])??string.Empty;string? childName=ti==3?NullableString(r["VarianteNombre"]):ti==4?NullableString(r["PresentacionNombre"]):null;string visible=ti switch{1=>"Producto base",2=>"Servicio",3=>$"Variante · {childName}",4=>$"Presentación venta · {childName}",_=>string.Empty};decimal quantity=Convert.ToDecimal(r["Cantidad"]),price=KitsPolicy.Money(Convert.ToDecimal(r["PrecioUnitario"]));items.Add(new KitComponentResponse{Id=(Guid)r["id"],IdentityKey=(Guid)r["identityKey"],TipoIdentidad=ti,TipoIdentidadNombre=TypeIdentityName(ti),IdProductoServicio=(Guid)r["idProductoServicio"],IdVariante=NullableGuid(r["idVariante"]),IdPresentacionVenta=NullableGuid(r["idPresentacionVenta"]),Codigo=ti==3?NullableString(r["VarianteCodigo"])??Convert.ToString(r["BaseCodigo"])??string.Empty:Convert.ToString(r["BaseCodigo"])??string.Empty,Nombre=childName==null?baseName:$"{baseName} · {childName}",IdentidadVisible=visible,UnidadPermiteDecimales=Convert.ToBoolean(r["Decimales"]),Cantidad=quantity,PrecioUnitario=price,Importe=KitsPolicy.Amount(quantity,price),FuenteActiva=productActive&&childActive,MotivoFuenteInactiva=productActive&&childActive?null:!productActive?"PRODUCTO_SERVICIO_INACTIVO":"IDENTIDAD_ESPECIFICA_INACTIVA",Activo=Convert.ToBoolean(r["Activo"]),FechaArchivado=NullableDate(r["FechaArchivado"])});}}
        detail.Componentes=items;KitComponentResponse[] active=items.Where(x=>x.Activo).ToArray();detail.NumeroComponentes=active.Length;detail.ValorComponentes=KitsPolicy.Money(active.Sum(x=>x.Importe));detail.Diferencia=KitsPolicy.Money(detail.PrecioKit-detail.ValorComponentes);List<string> reasons=new();if(!detail.Activo)reasons.Add("KIT_INACTIVO");if(active.Length==0)reasons.Add("KIT_SIN_COMPONENTES");if(active.Any(x=>!x.FuenteActiva))reasons.Add("FUENTE_INACTIVA");if(active.Any(x=>!ComponentFits(detail.Tipo,x.TipoIdentidad)))reasons.Add("COMPONENTE_INCOMPATIBLE_TIPO");detail.MotivosNoUtilizable=reasons;detail.EsUtilizable=reasons.Count==0;detail.TipoNombre=KitTypeName(detail.Tipo);return detail;
    }

    private async Task<KitDetailResponse> RequireKitAsync(SqlConnection c,SqlTransaction tx,Guid company,Guid id,bool requireActive,CancellationToken ct){KitDetailResponse kit=await LoadDetailAsync(c,tx,company,id,true,ct)??throw new KitsDomainException("KIT_NO_ENCONTRADO","El Kit no existe.");if(requireActive&&!kit.Activo)throw new KitsDomainException("KIT_INACTIVO","El Kit está archivado.");return kit;}
    private async Task<KitDetailResponse?> LoadByIdentityKeyAsync(SqlConnection c,SqlTransaction tx,Guid company,Guid key,CancellationToken ct){using SqlCommand cmd=new("SELECT id FROM dbo.Kits WITH (UPDLOCK,HOLDLOCK) WHERE idEmpresa=@Empresa AND identityKey=@Key",c,tx);Add(cmd,"@Empresa",company);Add(cmd,"@Key",key);object? id=await cmd.ExecuteScalarAsync(ct);return id is Guid value?await LoadDetailAsync(c,tx,company,value,true,ct):null;}
    private async Task<KitComponentResponse?> LoadComponentByIdentityKeyAsync(SqlConnection c,SqlTransaction tx,Guid company,Guid kit,Guid key,CancellationToken ct){KitDetailResponse d=await RequireKitAsync(c,tx,company,kit,false,ct);return d.Componentes.SingleOrDefault(x=>x.IdentityKey==key);}
    private async Task EnsureNameAvailableAsync(SqlConnection c,SqlTransaction tx,Guid company,string name,Guid? exclude,CancellationToken ct){using SqlCommand cmd=new("SELECT COUNT_BIG(1) FROM dbo.Kits WITH (UPDLOCK,HOLDLOCK) WHERE idEmpresa=@Empresa AND Nombre=@Nombre AND Activo=1 AND FechaArchivado IS NULL AND (@Exclude IS NULL OR id<>@Exclude)",c,tx);Add(cmd,"@Empresa",company);Add(cmd,"@Nombre",name);AddNullable(cmd,"@Exclude",exclude);if(Convert.ToInt64(await cmd.ExecuteScalarAsync(ct))>0)throw new KitsDomainException("KIT_NOMBRE_DUPLICADO","Ya existe un Kit activo con ese nombre.");}
    private async Task EnsureComponentAvailableAsync(SqlConnection c,SqlTransaction tx,Guid company,Guid kit,KitIdentity i,Guid? exclude,CancellationToken ct){using SqlCommand cmd=new(@"SELECT COUNT_BIG(1) FROM dbo.KitsComponentes WITH (UPDLOCK,HOLDLOCK) WHERE idEmpresa=@Empresa AND idKit=@Kit AND Activo=1 AND FechaArchivado IS NULL AND (@Exclude IS NULL OR id<>@Exclude) AND TipoIdentidad=@Tipo AND idProductoServicio=@Producto AND ISNULL(idVariante,'00000000-0000-0000-0000-000000000000')=ISNULL(@Variante,'00000000-0000-0000-0000-000000000000') AND ISNULL(idPresentacionVenta,'00000000-0000-0000-0000-000000000000')=ISNULL(@Presentacion,'00000000-0000-0000-0000-000000000000')",c,tx);Add(cmd,"@Empresa",company);Add(cmd,"@Kit",kit);AddNullable(cmd,"@Exclude",exclude);BindIdentity(cmd,i);if(Convert.ToInt64(await cmd.ExecuteScalarAsync(ct))>0)throw new KitsDomainException("COMPONENTE_DUPLICADO","La identidad ya es un componente activo del Kit.");}

    private async Task InsertComponentAsync(SqlConnection c,SqlTransaction tx,KitActor actor,Guid kit,Guid id,Guid identityKey,PreparedComponent item,CancellationToken ct){using SqlCommand cmd=new(@"INSERT dbo.KitsComponentes(id,idEmpresa,identityKey,idKit,TipoIdentidad,idProductoServicio,idVariante,idPresentacionVenta,Cantidad,PrecioUnitario,Activo,FechaCreacion,FechaActualizacion,idUsuarioCreacion,idUsuarioActualizacion) VALUES(@Id,@Empresa,@Identity,@Kit,@Tipo,@Producto,@Variante,@Presentacion,@Cantidad,@Precio,1,SYSUTCDATETIME(),SYSUTCDATETIME(),@Usuario,@Usuario)",c,tx);Add(cmd,"@Id",id);Add(cmd,"@Empresa",actor.IdEmpresa);Add(cmd,"@Identity",identityKey);Add(cmd,"@Kit",kit);BindIdentity(cmd,item.Identity);Add(cmd,"@Cantidad",item.Request.Cantidad);Add(cmd,"@Precio",item.Price);AddNullable(cmd,"@Usuario",actor.IdUsuario);await cmd.ExecuteNonQueryAsync(ct);}
    private static async Task InsertHistoryAsync(SqlConnection c,SqlTransaction tx,KitActor actor,Guid kit,Guid? component,byte entity,string operation,object? before,object? after,Guid correlation,CancellationToken ct){using SqlCommand cmd=new(@"INSERT dbo.KitsHistorial(id,idEmpresa,idKit,idKitComponente,TipoEntidad,Operacion,DatosAntes,DatosDespues,idUsuario,CorrelationId,FechaUtc) VALUES(NEWID(),@Empresa,@Kit,@Componente,@Entidad,@Operacion,@Antes,@Despues,@Usuario,@Correlation,SYSUTCDATETIME())",c,tx);Add(cmd,"@Empresa",actor.IdEmpresa);Add(cmd,"@Kit",kit);AddNullable(cmd,"@Componente",component);Add(cmd,"@Entidad",entity);Add(cmd,"@Operacion",operation);AddNullable(cmd,"@Antes",before==null?null:JsonSerializer.Serialize(before,JsonOptions));AddNullable(cmd,"@Despues",after==null?null:JsonSerializer.Serialize(after,JsonOptions));AddNullable(cmd,"@Usuario",actor.IdUsuario);Add(cmd,"@Correlation",correlation);await cmd.ExecuteNonQueryAsync(ct);}

    private async Task<SqlConnection> OpenAsync(CancellationToken ct){SqlConnection c=_connections.CreateConnection(_descriptor);await c.OpenAsync(ct);return c;}
    private static KitIdentity Identity(byte type,Guid product,Guid? variant,Guid? presentation){KitIdentity value=new(type,product,variant,presentation);KitsPolicy.ValidateIdentityShape(value);return value;}
    private static bool ComponentFits(byte kitType,byte identityType)=>kitType==3||(kitType==1&&identityType!=2)||(kitType==2&&identityType==2);
    private static string TypeIdentityName(byte value)=>value switch{1=>"Producto base",2=>"Servicio",3=>"Variante",4=>"Presentación venta",_=>"Desconocido"};
    private static string KitTypeName(byte value)=>value switch{1=>"Producto",2=>"Servicio",3=>"Mixto",_=>"Desconocido"};
    private static string RequireOperationKey(string? key){string v=(key??string.Empty).Trim();if(v.Length is <1 or >120)throw new KitsDomainException("OPERATION_KEY_INVALIDA","OperationKey es obligatoria y admite hasta 120 caracteres.");return v;}
    public static Guid OperationIdentity(Guid company,string operationKey){byte[] hash=SHA256.HashData(Encoding.UTF8.GetBytes($"KITS-V1|{company:D}|{operationKey.Trim()}"));byte[] bytes=hash[..16];bytes[6]=(byte)((bytes[6]&0x0F)|0x50);bytes[8]=(byte)((bytes[8]&0x3F)|0x80);return new Guid(bytes);}
    private static void EnsureNoDuplicates(IEnumerable<KitIdentity> identities){if(identities.GroupBy(x=>x).Any(g=>g.Count()>1))throw new KitsDomainException("COMPONENTE_DUPLICADO","La solicitud contiene componentes duplicados.");}
    private static void EnsureCreateReplayMatches(KitDetailResponse d,string name,KitCreateRequest r,IReadOnlyList<PreparedComponent> p){bool same=d.Nombre==name&&d.Tipo==r.Tipo&&d.PrecioKit==KitsPolicy.Money(r.PrecioKit)&&d.Activo==r.Activo&&d.Componentes.Count==p.Count&&p.All(x=>d.Componentes.Any(c=>c.TipoIdentidad==x.Identity.TipoIdentidad&&c.IdProductoServicio==x.Identity.IdProductoServicio&&c.IdVariante==x.Identity.IdVariante&&c.IdPresentacionVenta==x.Identity.IdPresentacionVenta&&c.Cantidad==x.Request.Cantidad&&c.PrecioUnitario==x.Price));if(!same)throw new KitsDomainException("IDEMPOTENCY_CONFLICT","OperationKey ya fue usada con otra carga.");}
    private static void ValidateImage(string? url,string? name){if((url?.Trim().Length??0)>1000||(name?.Trim().Length??0)>255)throw new KitsDomainException("IMAGEN_INVALIDA","Los metadatos de imagen exceden la longitud permitida.");}
    private static object KitSnapshot(string name,byte type,decimal price,string? url,string? image,bool active)=>new{Nombre=name,Tipo=type,PrecioKit=KitsPolicy.Money(price),ImagenUrl=Clean(url),ImagenNombre=Clean(image),Activo=active};
    private static object ComponentSnapshot(PreparedComponent p,bool active)=>ComponentSnapshot(p.Identity,p.Request.Cantidad,p.Price,p.Source,active);
    private static object ComponentSnapshot(KitIdentity i,decimal quantity,decimal price,KitSource source,bool active)=>new{i.TipoIdentidad,i.IdProductoServicio,i.IdVariante,i.IdPresentacionVenta,Cantidad=quantity,PrecioUnitario=KitsPolicy.Money(price),source.Codigo,source.Nombre,source.IdentidadVisible,Activo=active};
    private static object ComponentSnapshot(KitComponentResponse c)=>new{c.TipoIdentidad,c.IdProductoServicio,c.IdVariante,c.IdPresentacionVenta,c.Cantidad,c.PrecioUnitario,c.Codigo,c.Nombre,c.IdentidadVisible,c.Activo};
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private static string? NullableString(object value)=>value==DBNull.Value?null:Convert.ToString(value);
    private static Guid? NullableGuid(object value)=>value==DBNull.Value?null:(Guid)value;
    private static DateTime? NullableDate(object value)=>value==DBNull.Value?null:Convert.ToDateTime(value);
    private static void Add(SqlCommand c,string name,object value)=>c.Parameters.AddWithValue(name,value);
    private static void AddNullable(SqlCommand c,string name,object? value)=>c.Parameters.AddWithValue(name,value??DBNull.Value);
    private static void BindIdentity(SqlCommand c,KitIdentity i){Add(c,"@Tipo",i.TipoIdentidad);Add(c,"@Producto",i.IdProductoServicio);AddNullable(c,"@Variante",i.IdVariante);AddNullable(c,"@Presentacion",i.IdPresentacionVenta);}
    private static KitOperationResponse Success(Guid id,Guid correlation,bool replay,string message)=>new(){Id=id,CorrelationId=correlation,Replayed=replay,Message=message};
    private static KitsDomainException Duplicate(SqlException _)=>new("DUPLICADO","La operación entra en conflicto con un Kit o componente activo existente.");
    private static void SafeRollback(SqlTransaction tx){try{tx.Rollback();}catch{}}
    private sealed record PreparedComponent(KitComponentCreateRequest Request,KitIdentity Identity,KitSource Source,decimal Price);
}

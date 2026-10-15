using System.Data;
using System.Data.SqlClient;
using checklistWs.Models.Kits;
using checklistWs.Services.Tenant;
using Xunit;
using Xunit.Abstractions;

namespace checklistWs.Tests.Services.Tenant;

public sealed class KitsBackendV1SqlIntegrationTests
{
    private readonly ITestOutputHelper _output;
    public KitsBackendV1SqlIntegrationTests(ITestOutputHelper output)=>_output=output;

    [Fact]
    public async Task CheckAppErp_RealAggregateCrudIdempotencyHistoryRollbackAndCleanup()
    {
        string? cs=Environment.GetEnvironmentVariable("MOKA_CHECKAPPERP_QA_CONNECTION");
        if(string.IsNullOrWhiteSpace(cs))return;
        Guid company=Guid.NewGuid();
        await SeedCatalogAsync(cs,company);
        TenantDatabaseDescriptor descriptor=new(){EmpresaKey="CHECKAPPERP",IdEmpresa=company,ConnectionString=cs};
        TenantSqlConnectionFactory connections=new();
        KitsService service=new(connections,descriptor,new ListaPreciosService(new SqlListaPreciosRepository(connections,descriptor)));
        KitActor actor=new(company,null);string prefix=$"KITS02-{Guid.NewGuid():N}";
        try
        {
            IReadOnlyList<KitOptionResponse> options=await service.OptionsAsync(actor,string.Empty,KitsConstants.TipoMixto);
            KitOptionResponse product=Assert.Single(options.Where(x=>x.TipoIdentidad==KitsConstants.IdentidadProducto).Take(1));
            KitOptionResponse serviceOption=Assert.Single(options.Where(x=>x.TipoIdentidad==KitsConstants.IdentidadServicio).Take(1));
            KitOptionResponse variant=Assert.Single(options.Where(x=>x.TipoIdentidad==KitsConstants.IdentidadVariante).Take(1));
            KitOptionResponse presentation=Assert.Single(options.Where(x=>x.TipoIdentidad==KitsConstants.IdentidadPresentacionVenta).Take(1));

            KitPriceResolutionResponse productPrice=await service.ResolvePriceAsync(actor,PriceRequest(product));
            KitPriceResolutionResponse servicePrice=await service.ResolvePriceAsync(actor,PriceRequest(serviceOption));
            Assert.True(productPrice.Resuelto,$"Product price: {productPrice.Code}");Assert.True(servicePrice.Resuelto,$"Service price: {servicePrice.Code}");
            Assert.True((await service.ResolvePriceAsync(actor,PriceRequest(variant))).Resuelto);
            Assert.True((await service.ResolvePriceAsync(actor,PriceRequest(presentation))).Resuelto);

            List<KitComponentCreateRequest> components=new()
            {
                Component("c-product",product,productPrice.PrecioPropuesto!.Value),
                Component("c-service",serviceOption,servicePrice.PrecioPropuesto!.Value)
            };
            components[1].Cantidad=1.25m;
            components.Add(Component("c-variant",variant,(await service.ResolvePriceAsync(actor,PriceRequest(variant))).PrecioPropuesto!.Value));
            components.Add(Component("c-presentation",presentation,(await service.ResolvePriceAsync(actor,PriceRequest(presentation))).PrecioPropuesto!.Value));
            foreach(KitComponentCreateRequest c in components)c.OperationKey=$"{prefix}-{c.OperationKey}";
            KitCreateRequest create=new(){OperationKey=$"{prefix}-create",Nombre=$"{prefix}-MIXTO",Tipo=KitsConstants.TipoMixto,PrecioKit=123.45m,Componentes=components};
            KitOperationResponse first=await service.CreateAsync(actor,create);KitOperationResponse retry=await service.CreateAsync(actor,create);
            Assert.False(first.Replayed);Assert.True(retry.Replayed);Assert.Equal(first.Id,retry.Id);
            KitCreateRequest inactiveEmpty=new(){OperationKey=$"{prefix}-inactive-empty",Nombre=$"{prefix}-INACTIVE-EMPTY",Tipo=KitsConstants.TipoProducto,PrecioKit=0m,Activo=false,Componentes=new()};
            KitOperationResponse inactiveCreated=await service.CreateAsync(actor,inactiveEmpty);
            KitDetailResponse inactiveDetail=await service.GetAsync(actor,inactiveCreated.Id!.Value);
            Assert.False(inactiveDetail.Activo);Assert.Empty(inactiveDetail.Componentes);Assert.Contains("KIT_INACTIVO",inactiveDetail.MotivosNoUtilizable);
            KitCreateRequest concurrent=CreateRequest($"{prefix}-CONCURRENT",$"{prefix}-concurrent",product,productPrice.PrecioPropuesto.Value);
            KitOperationResponse[] concurrentResults=await Task.WhenAll(service.CreateAsync(actor,concurrent),service.CreateAsync(actor,concurrent));
            Assert.Single(concurrentResults,x=>!x.Replayed);Assert.Single(concurrentResults,x=>x.Replayed);Assert.Single(concurrentResults.Select(x=>x.Id).Distinct());
            KitCreateRequest serviceKit=new(){OperationKey=$"{prefix}-service",Nombre=$"{prefix}-SERVICE",Tipo=KitsConstants.TipoServicio,PrecioKit=40m,Componentes=new(){Component($"{prefix}-service-component",serviceOption,servicePrice.PrecioPropuesto.Value)}};
            serviceKit.Componentes[0].Cantidad=1.5m;KitOperationResponse serviceCreated=await service.CreateAsync(actor,serviceKit);
            KitOperationResponse typeChanged=await service.UpdateAsync(actor,serviceCreated.Id!.Value,new KitUpdateRequest{Nombre=serviceKit.Nombre,Tipo=KitsConstants.TipoMixto,PrecioKit=40m});
            Assert.Contains(await service.HistoryAsync(actor,serviceCreated.Id.Value),x=>x.Operacion=="CAMBIO_TIPO"&&x.CorrelationId==typeChanged.CorrelationId);
            KitCreateRequest collisionA=CreateRequest($"{prefix}-COLLISION",$"{prefix}-collision-a",product,productPrice.PrecioPropuesto.Value);
            KitCreateRequest collisionB=CreateRequest($"{prefix}-COLLISION",$"{prefix}-collision-b",product,productPrice.PrecioPropuesto.Value);
            bool[] collisions=await Task.WhenAll(TryCreateAsync(service,actor,collisionA),TryCreateAsync(service,actor,collisionB));
            Assert.Single(collisions,x=>x);Assert.Single(collisions,x=>!x);
            Guid idKit=first.Id!.Value;KitDetailResponse detail=await service.GetAsync(actor,idKit);
            Assert.Equal(components.Count,detail.NumeroComponentes);Assert.True(detail.EsUtilizable);Assert.Equal(KitsPolicy.Money(detail.PrecioKit-detail.ValorComponentes),detail.Diferencia);
            Assert.Contains(detail.Componentes,x=>x.TipoIdentidad==1);Assert.Contains(detail.Componentes,x=>x.TipoIdentidad==2);
            Assert.Contains(detail.Componentes,x=>x.TipoIdentidad==3);
            Assert.Contains(detail.Componentes,x=>x.TipoIdentidad==4);

            await ExecuteAsync(cs,"UPDATE dbo.ProductosServicios SET Activo=0 WHERE idEmpresa=@Empresa AND id=@Product",company,product.IdProductoServicio);
            KitDetailResponse unusable=await service.GetAsync(actor,idKit);Assert.False(unusable.EsUtilizable);Assert.Contains("FUENTE_INACTIVA",unusable.MotivosNoUtilizable);
            KitComponentCreateRequest inactiveAttempt=Component($"{prefix}-inactive",product,productPrice.PrecioPropuesto.Value);
            Assert.Equal("FUENTE_INACTIVA",(await Assert.ThrowsAsync<KitsDomainException>(()=>service.AddComponentAsync(actor,idKit,inactiveAttempt))).Code);
            await ExecuteAsync(cs,"UPDATE dbo.ProductosServicios SET Activo=1 WHERE idEmpresa=@Empresa AND id=@Product",company,product.IdProductoServicio);

            KitComponentCreateRequest mismatched=Component($"{prefix}-mismatch",variant,1m);mismatched.IdProductoServicio=product.IdProductoServicio;
            Assert.Equal("FUENTE_NO_ENCONTRADA",(await Assert.ThrowsAsync<KitsDomainException>(()=>service.AddComponentAsync(actor,idKit,mismatched))).Code);

            KitsDomainException typeConflict=await Assert.ThrowsAsync<KitsDomainException>(()=>service.UpdateAsync(actor,idKit,new KitUpdateRequest{Nombre=create.Nombre,Tipo=KitsConstants.TipoProducto,PrecioKit=130m}));
            Assert.Equal("CAMBIO_TIPO_CONFLICTO",typeConflict.Code);
            KitComponentResponse editable=detail.Componentes.First();
            await service.UpdateComponentAsync(actor,idKit,editable.Id,new KitComponentUpdateRequest{TipoIdentidad=editable.TipoIdentidad,IdProductoServicio=editable.IdProductoServicio,IdVariante=editable.IdVariante,IdPresentacionVenta=editable.IdPresentacionVenta,Cantidad=editable.UnidadPermiteDecimales?1.25m:2m,PrecioUnitario=editable.PrecioUnitario+1m});
            await Assert.ThrowsAsync<KitsDomainException>(()=>service.AddComponentAsync(actor,idKit,Component($"{prefix}-duplicate",product,productPrice.PrecioPropuesto.Value)));

            KitOperationResponse archived=await service.ArchiveAsync(actor,idKit);Assert.False(archived.Replayed);Assert.True((await service.ArchiveAsync(actor,idKit)).Replayed);
            foreach(KitComponentResponse item in (await service.GetAsync(actor,idKit)).Componentes.Where(x=>x.Activo))await service.ArchiveComponentAsync(actor,idKit,item.Id);
            Assert.Equal("KIT_SIN_COMPONENTES",(await Assert.ThrowsAsync<KitsDomainException>(()=>service.ReactivateAsync(actor,idKit))).Code);
            foreach(KitComponentResponse item in (await service.GetAsync(actor,idKit)).Componentes)await service.ReactivateComponentAsync(actor,idKit,item.Id);
            Assert.False((await service.ReactivateAsync(actor,idKit)).Replayed);

            IReadOnlyList<KitHistoryResponse> history=await service.HistoryAsync(actor,idKit);
            Assert.Contains(history,x=>x.Operacion=="ALTA"&&x.DatosDespues!=null);
            Assert.Contains(history,x=>x.Operacion=="AGREGAR_COMPONENTE");Assert.Contains(history,x=>x.Operacion=="BAJA");Assert.Contains(history,x=>x.Operacion=="REACTIVAR");
            Assert.All(history,x=>Assert.NotEqual(Guid.Empty,x.CorrelationId));
            Assert.Equal("KIT_NO_ENCONTRADO",(await Assert.ThrowsAsync<KitsDomainException>(()=>service.GetAsync(new KitActor(Guid.NewGuid(),null),idKit))).Code);

            KitsService unresolvedService=new(connections,descriptor,new UnresolvedPriceService());
            KitPriceResolutionResponse unresolved=await unresolvedService.ResolvePriceAsync(actor,PriceRequest(product));
            Assert.False(unresolved.Resuelto);Assert.Equal(ListaPreciosResolutionCodes.SinPrecioResoluble,unresolved.Code);
            Assert.Equal("PRECIO_NO_RESOLUBLE",(await Assert.ThrowsAsync<KitsDomainException>(()=>unresolvedService.CreateAsync(actor,CreateRequest($"{prefix}-NO-PRICE",$"{prefix}-no-price",product,1m)))).Code);

            if(components.Count>=2)
            {
                KitCreateRequest rollback=new(){OperationKey=$"{prefix}-rollback",Nombre=$"{prefix}-ROLLBACK",Tipo=3,PrecioKit=1m,Componentes=new(){Clone(components[0],$"{prefix}-same"),Clone(components[1],$"{prefix}-same")}};
                await Assert.ThrowsAsync<KitsDomainException>(()=>service.CreateAsync(actor,rollback));
                Assert.DoesNotContain(await service.ListAsync(actor,new KitListQuery{Busqueda=$"{prefix}-ROLLBACK",Activo=null}),x=>x.Nombre==$"{prefix}-ROLLBACK");
            }
            _output.WriteLine($"CHECKAPPERP|Empresa={company}|Kit={idKit}|Components={components.Count}|History={history.Count}|Idempotency=PASS|Rollback=PASS");
        }
        finally{await CleanupAsync(cs,company,prefix);}
        Assert.Equal((0L,0L,0L),await FixtureCountsAsync(cs,company,prefix));
        Assert.Equal(0L,await CatalogFixtureCountAsync(cs,company));
    }

    private static KitPriceResolutionRequest PriceRequest(KitOptionResponse x)=>new(){TipoIdentidad=x.TipoIdentidad,IdProductoServicio=x.IdProductoServicio,IdVariante=x.IdVariante,IdPresentacionVenta=x.IdPresentacionVenta};
    private static KitComponentCreateRequest Component(string key,KitOptionResponse x,decimal price)=>new(){OperationKey=key,TipoIdentidad=x.TipoIdentidad,IdProductoServicio=x.IdProductoServicio,IdVariante=x.IdVariante,IdPresentacionVenta=x.IdPresentacionVenta,Cantidad=1m,PrecioUnitario=price};
    private static KitComponentCreateRequest Clone(KitComponentCreateRequest x,string key)=>new(){OperationKey=key,TipoIdentidad=x.TipoIdentidad,IdProductoServicio=x.IdProductoServicio,IdVariante=x.IdVariante,IdPresentacionVenta=x.IdPresentacionVenta,Cantidad=x.Cantidad,PrecioUnitario=x.PrecioUnitario};
    private static KitCreateRequest CreateRequest(string name,string operation,KitOptionResponse source,decimal price)=>new(){OperationKey=operation,Nombre=name,Tipo=KitsConstants.TipoProducto,PrecioKit=price,Componentes=new(){Component($"{operation}-component",source,price)}};
    private static async Task<bool> TryCreateAsync(KitsService service,KitActor actor,KitCreateRequest request){try{await service.CreateAsync(actor,request);return true;}catch(KitsDomainException ex) when(ex.Code=="DUPLICADO"||ex.Code=="KIT_NOMBRE_DUPLICADO"){return false;}}
    private static async Task SeedCatalogAsync(string cs,Guid company)
    {
        using SqlConnection c=new(cs);await c.OpenAsync();using SqlCommand cmd=new(@"
DECLARE @Category uniqueidentifier=NEWID(),@Unit uniqueidentifier=NEWID(),@ProductA uniqueidentifier=NEWID(),@ProductB uniqueidentifier=NEWID(),@Service uniqueidentifier=NEWID();
INSERT dbo.ProductosServiciosCategorias(id,idEmpresa,Codigo,Nombre,AplicaA) VALUES(@Category,@Empresa,N'MOKA-K02',N'MOKA KITS02',0);
INSERT dbo.ProductosServiciosUnidadesMedida(id,idEmpresa,Codigo,Nombre,Abreviatura,TipoUnidad,PermiteDecimales) VALUES(@Unit,@Empresa,N'MK02',N'Pieza MOKA',N'MK',N'ITEM',1);
INSERT dbo.ProductosServicios(id,idEmpresa,Tipo,Codigo,Nombre,idCategoria,idUnidadMedida,Costo,PrecioPublico,CausaInventario,EsProductoFisico)
VALUES(@ProductA,@Empresa,1,N'MK02-A',N'MOKA Producto A',@Category,@Unit,1,10,1,1),(@ProductB,@Empresa,1,N'MK02-B',N'MOKA Producto B',@Category,@Unit,1,20,1,1),(@Service,@Empresa,2,N'MK02-S',N'MOKA Servicio',@Category,@Unit,1,30,0,0);
INSERT dbo.ProductosServiciosVariantes(id,idEmpresa,idProductoServicio,Sku,Nombre,ClaveCombinacion,PrecioPublico) VALUES(NEWID(),@Empresa,@ProductB,N'MK02-B-V',N'MOKA Variante',N'MOKA=1',21);
INSERT dbo.ProductosServiciosPresentacionesVenta(id,idEmpresa,idProductoServicio,Nombre,EquivalenciaBase,Precio,EsPredeterminada,Orden,Activo,CantidadVenta,idUnidadVenta) VALUES(NEWID(),@Empresa,@ProductB,N'MOKA Caja',1,22,1,0,1,1,@Unit);",c);cmd.Parameters.AddWithValue("@Empresa",company);await cmd.ExecuteNonQueryAsync();
    }
    private static async Task CleanupAsync(string cs,Guid company,string prefix){using SqlConnection c=new(cs);await c.OpenAsync();using SqlTransaction tx=c.BeginTransaction();using SqlCommand cmd=new(@"DELETE h FROM dbo.KitsHistorial h INNER JOIN dbo.Kits k ON k.idEmpresa=h.idEmpresa AND k.id=h.idKit WHERE k.idEmpresa=@Empresa AND k.Nombre LIKE @Prefix;DELETE cpt FROM dbo.KitsComponentes cpt INNER JOIN dbo.Kits k ON k.idEmpresa=cpt.idEmpresa AND k.id=cpt.idKit WHERE k.idEmpresa=@Empresa AND k.Nombre LIKE @Prefix;DELETE FROM dbo.Kits WHERE idEmpresa=@Empresa AND Nombre LIKE @Prefix;DELETE FROM dbo.ProductosServiciosPresentacionesVenta WHERE idEmpresa=@Empresa;DELETE FROM dbo.ProductosServiciosVariantes WHERE idEmpresa=@Empresa;DELETE FROM dbo.ProductosServicios WHERE idEmpresa=@Empresa;DELETE FROM dbo.ProductosServiciosUnidadesMedida WHERE idEmpresa=@Empresa;DELETE FROM dbo.ProductosServiciosCategorias WHERE idEmpresa=@Empresa;",c,tx);cmd.Parameters.Add("@Empresa",SqlDbType.UniqueIdentifier).Value=company;cmd.Parameters.Add("@Prefix",SqlDbType.NVarChar,160).Value=$"{prefix}%";await cmd.ExecuteNonQueryAsync();tx.Commit();}
    private static async Task<(long,long,long)> FixtureCountsAsync(string cs,Guid company,string prefix){using SqlConnection c=new(cs);await c.OpenAsync();using SqlCommand cmd=new(@"SELECT (SELECT COUNT_BIG(*) FROM dbo.Kits WHERE idEmpresa=@Empresa AND Nombre LIKE @Prefix),(SELECT COUNT_BIG(*) FROM dbo.KitsComponentes c INNER JOIN dbo.Kits k ON k.idEmpresa=c.idEmpresa AND k.id=c.idKit WHERE k.idEmpresa=@Empresa AND k.Nombre LIKE @Prefix),(SELECT COUNT_BIG(*) FROM dbo.KitsHistorial h INNER JOIN dbo.Kits k ON k.idEmpresa=h.idEmpresa AND k.id=h.idKit WHERE k.idEmpresa=@Empresa AND k.Nombre LIKE @Prefix)",c);cmd.Parameters.AddWithValue("@Empresa",company);cmd.Parameters.AddWithValue("@Prefix",$"{prefix}%");using SqlDataReader r=await cmd.ExecuteReaderAsync();await r.ReadAsync();return(r.GetInt64(0),r.GetInt64(1),r.GetInt64(2));}
    private static async Task<long> CatalogFixtureCountAsync(string cs,Guid company){using SqlConnection c=new(cs);await c.OpenAsync();using SqlCommand cmd=new(@"SELECT (SELECT COUNT_BIG(*) FROM dbo.ProductosServicios WHERE idEmpresa=@Empresa)+(SELECT COUNT_BIG(*) FROM dbo.ProductosServiciosVariantes WHERE idEmpresa=@Empresa)+(SELECT COUNT_BIG(*) FROM dbo.ProductosServiciosPresentacionesVenta WHERE idEmpresa=@Empresa)+(SELECT COUNT_BIG(*) FROM dbo.ProductosServiciosUnidadesMedida WHERE idEmpresa=@Empresa)+(SELECT COUNT_BIG(*) FROM dbo.ProductosServiciosCategorias WHERE idEmpresa=@Empresa)",c);cmd.Parameters.AddWithValue("@Empresa",company);return Convert.ToInt64(await cmd.ExecuteScalarAsync());}
    private static async Task ExecuteAsync(string cs,string sql,Guid company,Guid product){using SqlConnection c=new(cs);await c.OpenAsync();using SqlCommand cmd=new(sql,c);cmd.Parameters.AddWithValue("@Empresa",company);cmd.Parameters.AddWithValue("@Product",product);await cmd.ExecuteNonQueryAsync();}

    private sealed class UnresolvedPriceService : IListaPreciosService
    {
        public Task<ListaPreciosResolutionResult> ResolverPrecioAsync(Guid idEmpresa,ListaPreciosResolverRequest request,CancellationToken cancellationToken=default)=>Task.FromResult(new ListaPreciosResolutionResult{Resuelto=false,CodigoResolucion=ListaPreciosResolutionCodes.SinPrecioResoluble,Origen=ListaPreciosResolutionCodes.SinPrecioResoluble,Mensaje="Sin precio controlado.",TenantId=idEmpresa,CorrelationId=Guid.NewGuid()});
        public Task<IReadOnlyList<ListaPreciosListaDto>> ObtenerListasAsync(Guid idEmpresa,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosCombosDto> ObtenerCombosAsync(Guid idEmpresa,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<IReadOnlyList<ListaPreciosConsultaRowDto>> ConsultarAsync(Guid idEmpresa,ListaPreciosConsultaRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosInventarioDetalleDto> ObtenerInventarioDetalleAsync(Guid idEmpresa,ListaPreciosResolverRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<IReadOnlyList<ListaPreciosPrecioDto>> ObtenerPreciosPorProductoAsync(Guid idEmpresa,Guid idProductoServicio,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosEditorDto> ObtenerEditorAsync(Guid idEmpresa,ListaPreciosResolverRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<Guid> EnsureListaAsync(Guid idEmpresa,int nivel,Guid? usuarioId,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<Guid> GuardarPrecioAsync(Guid idEmpresa,ListaPreciosGuardarPrecioRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task BajaPrecioAsync(Guid idEmpresa,Guid idPrecio,Guid? usuarioId,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosResolutionResult> PreviewAsync(Guid idEmpresa,ListaPreciosPreviewRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosMatrizPreviewDto> PreviewMatrizAsync(Guid idEmpresa,ListaPreciosMatrizRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosMatrizPreviewDto> GuardarMatrizAsync(Guid idEmpresa,ListaPreciosMatrizRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosAjusteMasivoResultadoDto> PreviewAjusteMasivoAsync(Guid idEmpresa,ListaPreciosAjusteMasivoRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosAjusteMasivoResultadoDto> EjecutarAjusteMasivoAsync(Guid idEmpresa,ListaPreciosAjusteMasivoRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosCopiarListaResultadoDto> PreviewCopiarListaAsync(Guid idEmpresa,ListaPreciosCopiarListaRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosCopiarListaResultadoDto> EjecutarCopiarListaAsync(Guid idEmpresa,ListaPreciosCopiarListaRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosDescuentoMarcaResultadoDto> PreviewDescuentoMarcaAsync(Guid idEmpresa,ListaPreciosDescuentoMarcaRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosDescuentoMarcaResultadoDto> EjecutarDescuentoMarcaAsync(Guid idEmpresa,ListaPreciosDescuentoMarcaRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<IReadOnlyList<ListaPreciosHistorialDto>> ObtenerHistorialAsync(Guid idEmpresa,ListaPreciosResolverRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ListaPreciosHistorialPaginaDto> ConsultarHistorialAsync(Guid idEmpresa,ListaPreciosHistorialConsultaRequest request,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
    }
}

using checklistWs.Models.Kits;
using checklistWs.Services.Tenant;
using System.Text.Json;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class KitsBackendV1Tests
{
    private const string ReadPermission = ProductosServiciosAuthorizationDefaults.KitsPermissionCode;
    private const string WritePermission = ProductosServiciosAuthorizationDefaults.KitsAdministrarPermissionCode;

    [Fact]
    public void PermissionMatrix_NoPermissions_DeniesReadAndWrite()
    {
        string json = Permissions();
        Assert.False(ProductosServiciosAuthorizationService.EvaluateStoredPermission(json, ReadPermission).IsAllowed(ProductosServiciosPermissionRequirement.Read));
        Assert.False(ProductosServiciosAuthorizationService.EvaluateStoredPermission(json, WritePermission).IsAllowed(ProductosServiciosPermissionRequirement.Write));
    }

    [Fact]
    public void PermissionMatrix_ReadOnly_AllowsReadAndDeniesEveryWrite()
    {
        string json = Permissions(read: true);
        Assert.True(ProductosServiciosAuthorizationService.EvaluateStoredPermission(json, ReadPermission).IsAllowed(ProductosServiciosPermissionRequirement.Read));
        Assert.False(ProductosServiciosAuthorizationService.EvaluateStoredPermission(json, WritePermission).IsAllowed(ProductosServiciosPermissionRequirement.Write));
    }

    [Fact]
    public void PermissionMatrix_ReadAndAdminWrite_AllowsBothContracts()
    {
        string json = Permissions(read: true, adminAccess: true, adminWrite: true);
        Assert.True(ProductosServiciosAuthorizationService.EvaluateStoredPermission(json, ReadPermission).IsAllowed(ProductosServiciosPermissionRequirement.Read));
        Assert.True(ProductosServiciosAuthorizationService.EvaluateStoredPermission(json, WritePermission).IsAllowed(ProductosServiciosPermissionRequirement.Write));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{malformed")]
    [InlineData("")]
    [InlineData("[null]")]
    [InlineData("[{\"Opcion\":\"05000000\",\"Permisos\":null,\"Hijos\":null}]")]
    public void PermissionMatrix_MissingOrMalformed_FailsClosed(string json)
    {
        ProductosServiciosAuthorizationDecision decision = ProductosServiciosAuthorizationService.EvaluateStoredPermission(json, ReadPermission);
        Assert.False(decision.HasAccess);
        Assert.False(decision.CanWrite);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void KitTypes_AreCanonical(byte type) => KitsPolicy.ValidateKitType(type);

    [Fact]
    public void InvalidKitType_IsRejected() => Assert.Equal("TIPO_KIT_INVALIDO", Assert.Throws<KitsDomainException>(() => KitsPolicy.ValidateKitType(4)).Code);

    [Theory]
    [InlineData("1", false)]
    [InlineData("2.5000", true)]
    [InlineData("0.0001", true)]
    public void QuantityRules_AcceptCanonicalValues(string text,bool decimals)
        => KitsPolicy.ValidateQuantity(decimal.Parse(text,System.Globalization.CultureInfo.InvariantCulture),decimals);

    [Fact]
    public void QuantityRules_RejectDecimalForIntegerUnit()
        => Assert.Equal("CANTIDAD_DECIMAL_NO_PERMITIDA",Assert.Throws<KitsDomainException>(()=>KitsPolicy.ValidateQuantity(1.5m,false)).Code);

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void QuantityRules_RejectNonPositive(string text)
        => Assert.Equal("CANTIDAD_INVALIDA",Assert.Throws<KitsDomainException>(()=>KitsPolicy.ValidateQuantity(decimal.Parse(text,System.Globalization.CultureInfo.InvariantCulture),true)).Code);

    [Fact]
    public void MoneyAndTotals_UseUniformTwoDecimalAwayFromZero()
    {
        Assert.Equal(1.01m,KitsPolicy.Money(1.005m));
        Assert.Equal(3.02m,KitsPolicy.Amount(3m,1.005m));
    }

    [Fact]
    public void ServiceCannotCarryVariantOrPresentation()
    {
        KitIdentity invalid=new(KitsConstants.IdentidadServicio,Guid.NewGuid(),Guid.NewGuid(),null);
        Assert.Equal("IDENTIDAD_INVALIDA",Assert.Throws<KitsDomainException>(()=>KitsPolicy.ValidateIdentityShape(invalid)).Code);
    }

    [Fact]
    public void ProductVariantAndSalePresentationShapesAreAccepted()
    {
        Guid product=Guid.NewGuid();
        KitsPolicy.ValidateIdentityShape(new(KitsConstants.IdentidadProducto,product,null,null));
        KitsPolicy.ValidateIdentityShape(new(KitsConstants.IdentidadVariante,product,Guid.NewGuid(),null));
        KitsPolicy.ValidateIdentityShape(new(KitsConstants.IdentidadPresentacionVenta,product,null,Guid.NewGuid()));
    }

    [Fact]
    public void KitTypeCompatibility_IsFailClosed()
    {
        Guid product=Guid.NewGuid();
        KitSource service=new(new(KitsConstants.IdentidadServicio,product,null,null),KitsConstants.TipoServicio,"S","Servicio","Servicio",true,true,true);
        Assert.Equal("COMPONENTE_INCOMPATIBLE_TIPO",Assert.Throws<KitsDomainException>(()=>KitsPolicy.ValidateTypeCompatibility(KitsConstants.TipoProducto,service)).Code);
        KitsPolicy.ValidateTypeCompatibility(KitsConstants.TipoServicio,service);
        KitsPolicy.ValidateTypeCompatibility(KitsConstants.TipoMixto,service);
    }

    [Fact]
    public void OperationIdentity_IsStableTenantScopedAndRetrySafe()
    {
        Guid company=Guid.NewGuid();
        Assert.Equal(KitsService.OperationIdentity(company,"retry-1"),KitsService.OperationIdentity(company,"retry-1"));
        Assert.NotEqual(KitsService.OperationIdentity(company,"retry-1"),KitsService.OperationIdentity(Guid.NewGuid(),"retry-1"));
        Assert.NotEqual(KitsService.OperationIdentity(company,"retry-1"),KitsService.OperationIdentity(company,"retry-2"));
    }

    [Fact]
    public void BackendSource_DeclaresGateTenantPermissionsHistoryAndSerializableAtomicity()
    {
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..",".."));
        string controller=File.ReadAllText(Path.Combine(root,"checklistWs","Controllers","Kits","KitsController.cs"));
        string service=File.ReadAllText(Path.Combine(root,"checklistWs","Services","Tenant","KitsService.cs"));
        Assert.Contains("DatabaseScopes.Kits",controller);
        Assert.Contains("KitsPermissionCode",controller);
        Assert.Contains("KitsAdministrarPermissionCode",controller);
        Assert.Contains("ProductosServiciosIdentityValidation.TryValidate",controller);
        Assert.True(controller.IndexOf("PermissionCode=ReadPermission", StringComparison.Ordinal) < controller.LastIndexOf("PermissionCode=permission", StringComparison.Ordinal));
        Assert.Contains("TENANT_INPUT_MISMATCH", controller);
        Assert.Contains("IsolationLevel.Serializable",service);
        Assert.Contains("ResolverPrecioAsync",service);
        Assert.Contains("Nivel = 1",service);
        Assert.Contains("ALTA",service);
        Assert.Contains("CAMBIO_TIPO",service);
        Assert.Contains("REACTIVAR_COMPONENTE",service);
        Assert.DoesNotContain("UPDATE dbo.KitsHistorial",service,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM dbo.KitsHistorial",service,StringComparison.OrdinalIgnoreCase);
    }

    private static string Permissions(bool read = false, bool adminAccess = false, bool adminWrite = false)
        => JsonSerializer.Serialize(new[]
        {
            new
            {
                Opcion = "05000000",
                Permisos = new { Acceso = 1, Escritura = 0 },
                Hijos = new[]
                {
                    new
                    {
                        Opcion = "05001000",
                        Permisos = new { Acceso = 1, Escritura = 0 },
                        Hijos = new[]
                        {
                            new
                            {
                                Opcion = ReadPermission,
                                Permisos = new { Acceso = read ? 1 : 0, Escritura = 0 },
                                Hijos = new[]
                                {
                                    new
                                    {
                                        Opcion = WritePermission,
                                        Permisos = new { Acceso = adminAccess ? 1 : 0, Escritura = adminWrite ? 1 : 0 },
                                        Hijos = Array.Empty<object>()
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });
}

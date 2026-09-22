using System.Text.RegularExpressions;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class ProductosServiciosTenantIsolationTests
    {
        private static readonly string ControllerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "checklistWs", "Controllers", "ProductosServicios", "ProductosServiciosController.cs"));
        private static readonly string ControllerSource = File.ReadAllText(ControllerPath);

        [Fact]
        public void AllProductosServiciosEndpointsResolveServerSideContext()
        {
            string[] actionNames = Regex.Matches(ControllerSource, @"\[(?:HttpGet|HttpPost)\([^\]]+\)\]\s+public\s+async\s+Task<IActionResult>\s+(\w+)\s*\(", RegexOptions.Multiline)
                .Select(match => match.Groups[1].Value)
                .Where(name => !name.StartsWith("Subir", StringComparison.Ordinal))
                .ToArray();

            Assert.Equal(49, actionNames.Length);
            Dictionary<string, string> protectedDelegates = new(StringComparer.Ordinal)
            {
                ["ExportarProductosServicios"] = "ObtenerProductosServicios",
                ["ExportarCategoriasProductosServicios"] = "ObtenerCategoriasProductosServicios",
                ["ExportarMarcasProductosServicios"] = "ObtenerMarcasProductosServicios",
                ["ExportarUnidadesMedidaProductosServicios"] = "ObtenerUnidadesMedidaProductosServicios"
            };

            foreach (string action in actionNames)
            {
                string body = ExtractMethodBody(action);
                bool directlyProtected = body.Contains("TryResolveRequestContextAsync", StringComparison.Ordinal);
                bool delegatesToProtectedAction = protectedDelegates.TryGetValue(action, out string? protectedAction) &&
                    body.Contains(protectedAction, StringComparison.Ordinal) &&
                    ExtractMethodBody(protectedAction).Contains("TryResolveRequestContextAsync", StringComparison.Ordinal);
                Assert.True(directlyProtected || delegatesToProtectedAction, $"{action} no resuelve contexto server-side ni delega a una acción protegida.");
            }
        }

        [Fact]
        public void RequestContextExecutesT20GateBeforeCreatingSqlContext()
        {
            string body = ExtractMethodBody("TryResolveRequestContextAsync");

            Assert.Contains("TryResolveEmpresaId", body);
            Assert.Contains("_tenantDatabaseResolver.ResolveAsync", body);
            Assert.Contains("_compatibilityGate.EvaluateAsync", body);
            Assert.Contains("ToCompatibilityError", body);
            Assert.True(body.IndexOf("_compatibilityGate.EvaluateAsync", StringComparison.Ordinal) < body.IndexOf("context = new RequestContext", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("UPDATE dbo.ProductosServiciosExistencias")]
        [InlineData("UPDATE dbo.ProductosServiciosMovimientosInventario")]
        [InlineData("UPDATE dbo.ProductosServiciosMultimedia")]
        [InlineData("UPDATE dbo.ProductosServiciosPresentacionesVenta")]
        [InlineData("UPDATE dbo.ProductosServicios")]
        public void SensitiveUpdatesRequireIdEmpresaInWhere(string updateStart)
        {
            foreach (string statement in ExtractSqlStatements(updateStart))
            {
                string where = statement[(statement.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase) + 5)..];
                Assert.Contains("idEmpresa", where, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Theory]
        [InlineData("DELETE FROM dbo.ProductosServiciosExistencias")]
        [InlineData("DELETE FROM dbo.ProductosServiciosOpcionesVariante")]
        [InlineData("DELETE FROM dbo.ProductosServiciosVariantes")]
        [InlineData("DELETE FROM dbo.ProductosServiciosProductoTags")]
        [InlineData("DELETE FROM dbo.ProductosServiciosProductoAtributos")]
        public void DeletesRemainScopedByIdEmpresa(string deleteStart)
        {
            foreach (string statement in ExtractSqlStatements(deleteStart))
            {
                string where = statement[(statement.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase) + 5)..];
                Assert.Contains("idEmpresa", where, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Theory]
        [InlineData("ON cat.idEmpresa = ps.idEmpresa AND cat.id = ps.idCategoria")]
        [InlineData("ON um.idEmpresa = ps.idEmpresa AND um.id = ps.idUnidadMedida")]
        [InlineData("ON vv.idEmpresa = pv.idEmpresa AND vv.idVariante = pv.id")]
        [InlineData("ON ovv.idEmpresa = ov.idEmpresa AND ovv.idOpcionVariante = ov.id")]
        [InlineData("ON ppa.idEmpresa = pav.idEmpresa AND ppa.id = pav.idProductoAtributo")]
        public void JoinsPreserveTenantPredicate(string expectedJoin)
        {
            Assert.Contains(expectedJoin, ControllerSource);
        }

        [Fact]
        public void NestedClientIdsAreValidatedBeforeRelationshipSynchronization()
        {
            Assert.Contains("ValidateProductoAtributoRequestIdsAsync", ControllerSource);
            Assert.Contains("ValidateProductoOpcionVarianteRequestIdsAsync", ControllerSource);
            Assert.Contains("Se detectó una variante inválida para el producto", ControllerSource);
            Assert.Contains("ExistingItemIds", ControllerSource);
            Assert.Contains("Se detectó una evidencia multimedia inválida para el producto", ControllerSource);
            Assert.Contains("throw new ProductoServicioValidationException(\"Se detectó un valor de opción inválido para una variante.\")", ControllerSource);
        }

        [Theory]
        [InlineData("AddProductoServicioParameters", "command.Parameters.AddWithValue(\"@IdEmpresa\", idEmpresa);")]
        [InlineData("SynchronizeProductoMultimediaAsync", "insert.Parameters.AddWithValue(\"@IdEmpresa\", idEmpresa);")]
        [InlineData("SynchronizeProductoVariantesAsync", "insert.Parameters.AddWithValue(\"@IdEmpresa\", context.IdEmpresa);")]
        public void InsertsUseServerResolvedEmpresa(string methodName, string expectedParameter)
        {
            string body = ExtractMethodBody(methodName);
            Assert.Contains(expectedParameter, body);
        }

        [Fact]
        public void LegacyInventoryMutationEndpointFailsClosedForInventarioV1()
        {
            string body = ExtractMethodBody("RegistrarMovimientoInventarioAsync");

            Assert.Contains("ValidateMovimientoRequest(request, effectiveEmpresaId)", body);
            Assert.Contains("InventarioV1MovimientoBloqueadoMensaje", body);
            Assert.DoesNotContain("INSERT INTO dbo.ProductosServiciosMovimientosInventario", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UPDATE dbo.ProductosServiciosExistencias", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ClientIdEmpresaIsComparedWithEffectiveServerEmpresa()
        {
            string body = ExtractMethodBody("TryResolveRequestContextAsync");

            Assert.Contains("clientEmpresaId", body);
            Assert.Contains("effectiveEmpresaId", body);
            Assert.Contains("StatusCode(403", body);
            Assert.DoesNotContain("context.IdEmpresa = clientEmpresaId", ControllerSource, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> ExtractSqlStatements(string startsWith)
        {
            foreach (Match match in Regex.Matches(ControllerSource, Regex.Escape(startsWith) + @"(?<sql>.*?)(?:\"",\s*connection|\"", connection|\"",\s*conn)", RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                string statement = startsWith + match.Groups["sql"].Value;
                if (statement.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
                {
                    yield return statement;
                }
            }
        }

        private static string ExtractMethodBody(string methodName)
        {
            Match signature = Regex.Match(ControllerSource, @"(?:public|private)(?:\s+static)?\s+(?:async\s+)?[\w<>?]+\s+" + Regex.Escape(methodName) + @"\s*\([^)]*\)\s*\{", RegexOptions.Singleline);
            Assert.True(signature.Success, $"No se encontró el método {methodName}.");
            int start = signature.Index + signature.Length;
            int depth = 1;
            for (int i = start; i < ControllerSource.Length; i++)
            {
                if (ControllerSource[i] == '{') depth++;
                if (ControllerSource[i] == '}') depth--;
                if (depth == 0) return ControllerSource[start..i];
            }

            throw new InvalidOperationException($"No se pudo extraer el método {methodName}.");
        }
    }
}

using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class ListaPreciosHeaderReactivationTests
    {
        private static readonly Guid Empresa = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid OtraEmpresa = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid Header = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private static string RepositorySource => File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "../../../../checklistWs/Services/Tenant/ListaPreciosService.cs"));

        [Fact]
        public void LpListaReact01_ActivaExistente_ReutilizaMismoId()
        {
            ListaPreciosHeaderDecision result = Resolve(Active());
            Assert.Equal(ListaPreciosHeaderAction.Reuse, result.Action);
            Assert.Equal(Header, result.Id);
        }

        [Fact]
        public void LpListaReact02_ArchivadaExistente_ReactivaMismoId()
        {
            ListaPreciosHeaderDecision result = Resolve(Archived());
            Assert.Equal(ListaPreciosHeaderAction.Reactivate, result.Action);
            Assert.Equal(Header, result.Id);
        }

        [Fact]
        public void LpListaReact03_SegundaLlamada_ReutilizaSinNuevoId()
        {
            ListaPreciosHeaderDecision first = Resolve(Archived());
            ListaPreciosHeaderDecision second = Resolve(Active(first.Id!.Value));
            Assert.Equal(first.Id, second.Id);
            Assert.Equal(ListaPreciosHeaderAction.Reuse, second.Action);
        }

        [Fact]
        public void LpListaReact04_Inexistente_CreaUna()
        {
            ListaPreciosHeaderDecision result = Resolve();
            Assert.Equal(ListaPreciosHeaderAction.Create, result.Action);
            Assert.Null(result.Id);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public void LpListaReact05Y06_NivelCanonico_Pass(int nivel)
        {
            ListaPreciosHeaderDecision result = ListaPreciosHeaderPolicy.Resolve(Empresa, nivel, Array.Empty<ListaPreciosHeaderCandidate>());
            Assert.Equal(ListaPreciosHeaderAction.Create, result.Action);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(11)]
        public void LpListaReact07Y08_NivelFueraDeRango_Reject(int nivel)
        {
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
                ListaPreciosHeaderPolicy.Resolve(Empresa, nivel, Array.Empty<ListaPreciosHeaderCandidate>()));
            Assert.Equal(ListaPreciosResolutionCodes.ListaInvalida, error.Message);
        }

        [Fact]
        public void LpListaReact09_CrossTenant_NoReutilizaCabeceraAjena()
        {
            ListaPreciosHeaderDecision result = ListaPreciosHeaderPolicy.Resolve(Empresa, 1, new[] { Active(Header, OtraEmpresa) });
            Assert.Equal(ListaPreciosHeaderAction.Create, result.Action);
            Assert.Null(result.Id);
        }

        [Fact]
        public void LpListaReact10_Preview_NoInvocaEnsureLista()
        {
            string preview = Slice("public async Task<ListaPreciosResolutionResult> PreviewAsync", "public async Task<ListaPreciosAjusteMasivoResultadoDto> PreviewAjusteMasivoAsync");
            Assert.DoesNotContain("EnsureListaAsync", preview);
            Assert.DoesNotContain("GuardarPrecioAsync", preview);
        }

        [Fact]
        public void LpListaReact11_PrecioCero_SiguePermitido()
        {
            Assert.Contains("request.Precio < 0", RepositorySource);
            Assert.DoesNotContain("request.Precio <= 0", RepositorySource);
        }

        [Fact]
        public void LpListaReact12_Concurrencia_UsaSerializableRangeLockYRerelectura()
        {
            Assert.Contains("IsolationLevel.Serializable", RepositorySource);
            Assert.Contains("WITH (UPDLOCK, HOLDLOCK)", RepositorySource);
            Assert.Contains("ex.Number is 2601 or 2627", RepositorySource);
            Assert.Contains("ReadHeaderDecisionAsync", RepositorySource);
        }

        [Fact]
        public void LpListaReact13_MultiplesCandidatas_FailClosed()
        {
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => Resolve(Active(), Archived(Guid.NewGuid())));
            Assert.Equal(ListaPreciosHeaderPolicy.InconsistentHeader, error.Message);
        }

        [Fact]
        public void EstadoFisicoInconsistente_FailClosed()
        {
            ListaPreciosHeaderCandidate invalid = new(Header, Empresa, 1, true, DateTime.UtcNow);
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => Resolve(invalid));
            Assert.Equal(ListaPreciosHeaderPolicy.InconsistentHeader, error.Message);
        }

        private static ListaPreciosHeaderDecision Resolve(params ListaPreciosHeaderCandidate[] candidates)
            => ListaPreciosHeaderPolicy.Resolve(Empresa, 1, candidates);

        private static ListaPreciosHeaderCandidate Active(Guid? id = null, Guid? empresa = null)
            => new(id ?? Header, empresa ?? Empresa, 1, true, null);

        private static ListaPreciosHeaderCandidate Archived(Guid? id = null)
            => new(id ?? Header, Empresa, 1, false, DateTime.UtcNow.AddDays(-1));

        private static string Slice(string start, string end)
        {
            string source = RepositorySource;
            int startIndex = source.IndexOf(start, StringComparison.Ordinal);
            int endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
            return source[startIndex..endIndex];
        }
    }
}

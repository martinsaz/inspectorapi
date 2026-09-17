using System.Reflection;
using checklistWs.Controllers.ProductosServicios;
using Xunit;

namespace checklistWs.Tests.Controllers.ProductosServicios
{
    public sealed class ProductosServiciosRichTextSanitizerTests
    {
        [Fact]
        public void SanitizeRichTextHtml_KeepsAllowedFormatting()
        {
            string sanitized = Sanitize("<p>Texto <strong>importante</strong></p><ul><li>Uno</li></ul>");

            Assert.Contains("<p>", sanitized);
            Assert.Contains("<strong>", sanitized);
            Assert.Contains("<ul>", sanitized);
            Assert.Contains("<li>", sanitized);
        }

        [Fact]
        public void SanitizeRichTextHtml_RemovesScriptsAndEventHandlers()
        {
            string sanitized = Sanitize("<p onclick=\"alert(1)\">Hola</p><script>alert(2)</script><img src=x onerror=alert(3)>");

            Assert.DoesNotContain("script", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onclick", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onerror", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<img", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<p>", sanitized);
        }

        [Fact]
        public void SanitizeRichTextHtml_RemovesUnsafeHrefButKeepsSafeLinks()
        {
            string unsafeLink = Sanitize("<a href=\"javascript:alert(1)\" title=\"x\">mal</a>");
            string safeLink = Sanitize("<a href=\"https://checkapp.local\" target=\"_blank\">bien</a>");

            Assert.DoesNotContain("javascript:", unsafeLink, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("href=", unsafeLink, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("https://checkapp.local", safeLink);
            Assert.Contains("rel=\"noopener noreferrer\"", safeLink);
        }

        private static string Sanitize(string value)
        {
            MethodInfo? method = typeof(ProductosServiciosController).GetMethod(
                "SanitizeRichTextHtml",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(method);
            return Assert.IsType<string>(method!.Invoke(null, new object?[] { value }));
        }
    }
}

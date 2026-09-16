using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace checklistWs.Services.Tenant;

// Validates the existing HMAC proxy protocol or a principal authenticated by server middleware.
// Does not read request parameters, resolve a connection, authorize a role or open SQL.
public static class ProductosServiciosIdentityValidation
{
    private const string Prefix = "X-ProductosServicios-Proxy-";
    private static readonly string[] IdClaims = { "idEmpresa", "empresaId", "tenantId", "companyId", "tenant", "idempresa" };
    private static readonly string[] KeyClaims = { "empresa", "empresaNombre", "tenantName", "companyName", "nombreEmpresa" };
    private static readonly string[] UserClaims = { ClaimTypes.NameIdentifier, "sub", "idUsuario", "userid", "uid" };

    public static bool TryValidate(ClaimsPrincipal principal, IHeaderDictionary headers, string secret,
        DateTimeOffset now, out TenantDatabaseContext context, out string userId)
    {
        context = null!; userId = string.Empty;
        string? id = null, key = null, user = null;
        bool authenticated = principal.Identity?.IsAuthenticated == true;
        if (principal.Claims.Any() && !authenticated) return false;
        if (authenticated)
        {
            if (!SingleClaim(principal, IdClaims, out id, true) ||
                !SingleClaim(principal, KeyClaims, out key) ||
                !SingleClaim(principal, UserClaims, out user, false, true)) return false;
            foreach (var expiration in principal.FindAll("exp"))
                if (!long.TryParse(expiration.Value, out long seconds) || seconds <= now.ToUnixTimeSeconds()) return false;
        }
        bool hasProxy = headers.Keys.Any(k => k.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase));
        if (hasProxy)
        {
            if (string.IsNullOrWhiteSpace(secret) ||
                !Header(headers, "EmpresaId", out var proxyId) || !Header(headers, "Empresa", out var proxyKey) ||
                !Header(headers, "UsuarioId", out var proxyUser) || !Header(headers, "Timestamp", out var timestamp) ||
                !Header(headers, "Signature", out var signature) ||
                !Guid.TryParse(proxyId, out var guid) || guid == Guid.Empty ||
                !DateTimeOffset.TryParseExact(timestamp, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var issued) ||
                (now - issued.ToUniversalTime()).Duration() > TimeSpan.FromMinutes(5)) return false;
            string payload = string.Join('\n', proxyId.Trim(), proxyKey.Trim().ToUpperInvariant(), proxyUser.Trim(), timestamp.Trim());
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            byte[] actual;
            try { actual = Convert.FromBase64String(signature); } catch (FormatException) { return false; }
            if (!CryptographicOperations.FixedTimeEquals(expected, actual)) return false;
            if (authenticated && (Guid.Parse(id!) != guid || !string.Equals(key, proxyKey, StringComparison.OrdinalIgnoreCase) || user != proxyUser)) return false;
            id = guid.ToString(); key = proxyKey; user = proxyUser;
        }
        if ((!authenticated && !hasProxy) || !Guid.TryParse(id, out var company) || company == Guid.Empty || !Safe(key) || !Safe(user)) return false;
        context = new TenantDatabaseContext { IdEmpresa = company, EmpresaKey = key!.Trim().ToUpperInvariant() };
        userId = user!;
        return true;
    }

    private static bool SingleClaim(ClaimsPrincipal principal, string[] names, out string? value, bool guid = false, bool caseSensitive = false)
    {
        value = null;
        var values = principal.Claims.Where(c => names.Contains(c.Type, StringComparer.Ordinal)).Select(c => c.Value.Trim()).ToArray();
        if (values.Length == 0 || values.Any(v => !Safe(v))) return false;
        if (guid)
        {
            if (values.Any(v => !Guid.TryParse(v, out var id) || id == Guid.Empty)) return false;
            var ids = values.Select(Guid.Parse).Distinct().ToArray();
            if (ids.Length != 1) return false;
            value = ids[0].ToString(); return true;
        }
        if (values.Distinct(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase).Count() != 1) return false;
        value = values[0]; return true;
    }
    private static bool Header(IHeaderDictionary headers, string suffix, out string value)
    {
        value = string.Empty;
        if (!headers.TryGetValue(Prefix + suffix, out var values) || values.Count != 1) return false;
        value = values[0]?.Trim() ?? string.Empty;
        return Safe(value);
    }
    private static bool Safe(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && !value.Any(char.IsControl);
}

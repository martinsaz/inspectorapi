using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using checklistWs.Models.Kits;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Storage;

namespace checklistWs.Services.Tenant;

public sealed record KitPreparedImage(string Url, string OriginalName, string FinalFolder, string FinalStoredName, string TemporaryFolder, string TemporaryStoredName);

/// <summary>Pipeline de imagen aislado para Kits; conserva validación, temporal, promoción y compensación del patrón ProductosServicios.</summary>
public sealed class KitsImageStorage
{
    private const long MaxBytes = 10L * 1024L * 1024L;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(6);
    private static readonly string[] Extensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] MimeTypes = ["image/jpeg", "image/png", "image/webp"];
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    public KitsImageStorage(IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<KitImageTemporaryResponse> UploadTemporaryAsync(string companyKey, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length <= 0) throw Error("IMAGEN_REQUERIDA", "Selecciona una imagen válida para cargar.");
        if (file.Length > MaxBytes) throw Error("IMAGEN_TAMANO_INVALIDO", "La imagen excede el tamaño máximo permitido de 10 MB.");
        string extension = (Path.GetExtension(file.FileName) ?? string.Empty).Trim().ToLowerInvariant();
        string mime = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        if (!Extensions.Contains(extension) || !MimeTypes.Contains(mime)) throw Error("IMAGEN_FORMATO_INVALIDO", "La imagen debe estar en formato JPG, PNG o WEBP.");
        await using MemoryStream stream = new();
        await file.CopyToAsync(stream, cancellationToken);
        byte[] bytes = stream.ToArray();
        ValidateSignature(extension, bytes);
        string folder = $"{SafeCompany(companyKey)}/Kits/Temporal/Imagen";
        string stored = $"{Guid.NewGuid():N}{NormalizeExtension(extension, mime)}";
        string url = await UploadAsync(folder, stored, bytes);
        TemporaryPayload payload = new(SafeCompany(companyKey), folder, stored, CleanName(file.FileName), mime, file.Length, DateTime.UtcNow.Add(TokenLifetime));
        return new KitImageTemporaryResponse { Message = "La imagen temporal fue cargada.", TemporalToken = Protect(payload), Name = payload.OriginalName, Url = url };
    }

    public async Task<KitPreparedImage> PromoteAsync(string companyKey, Guid kitId, string token)
    {
        TemporaryPayload payload = Unprotect(token);
        string safeCompany = SafeCompany(companyKey);
        if (!string.Equals(payload.CompanyKey, safeCompany, StringComparison.OrdinalIgnoreCase) || !payload.Folder.StartsWith($"{safeCompany}/Kits/Temporal/", StringComparison.Ordinal))
            throw Error("IMAGEN_TOKEN_INVALIDO", "La imagen temporal no pertenece a la empresa activa.");
        byte[] bytes = await DownloadAsync(payload.Folder, payload.StoredName);
        ValidateSignature(Path.GetExtension(payload.OriginalName).ToLowerInvariant(), bytes);
        string finalFolder = $"{safeCompany}/Kits/{kitId:N}/Imagen";
        string finalStored = $"{Guid.NewGuid():N}{NormalizeExtension(Path.GetExtension(payload.OriginalName), payload.MimeType)}";
        string url = await UploadAsync(finalFolder, finalStored, bytes);
        return new KitPreparedImage(url, payload.OriginalName, finalFolder, finalStored, payload.Folder, payload.StoredName);
    }

    public async Task CleanupTemporaryAsync(string companyKey, string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        TemporaryPayload payload = Unprotect(token);
        string safeCompany = SafeCompany(companyKey);
        if (!string.Equals(payload.CompanyKey, safeCompany, StringComparison.OrdinalIgnoreCase)) return;
        await DeleteQuietlyAsync(payload.Folder, payload.StoredName);
    }

    public Task DeleteFinalQuietlyAsync(KitPreparedImage image) => DeleteQuietlyAsync(image.FinalFolder, image.FinalStoredName);

    private async Task<string> UploadAsync(string folder, string storedName, byte[] bytes)
    {
        (FirebaseAuthClient auth, string token) = await AuthenticateAsync();
        try
        {
            using MemoryStream stream = new(bytes);
            FirebaseStorage storage = Storage(token);
            return await storage.Child(folder).Child(storedName).PutAsync(stream);
        }
        finally { auth.SignOut(); }
    }

    private async Task<byte[]> DownloadAsync(string folder, string storedName)
    {
        (FirebaseAuthClient auth, string token) = await AuthenticateAsync();
        try
        {
            string url = await Storage(token).Child(folder).Child(storedName).GetDownloadUrlAsync();
            using HttpClient client = new();
            return await client.GetByteArrayAsync(url);
        }
        finally { auth.SignOut(); }
    }

    private async Task DeleteQuietlyAsync(string folder, string storedName)
    {
        try
        {
            (FirebaseAuthClient auth, string token) = await AuthenticateAsync();
            try { await Storage(token).Child(folder).Child(storedName).DeleteAsync(); }
            finally { auth.SignOut(); }
        }
        catch (Exception ex) { _logger.LogError(ex, "No fue posible limpiar una imagen de Kits en Firebase."); }
    }

    private async Task<(FirebaseAuthClient Auth, string Token)> AuthenticateAsync()
    {
        FirebaseAuthConfig config = new()
        {
            ApiKey = _configuration.GetValue<string>("fireBdata:fireApiKey"),
            AuthDomain = _configuration.GetValue<string>("fireBdata:fireAuthDomain"),
            Providers = [new EmailProvider()]
        };
        FirebaseAuthClient auth = new(config);
        UserCredential credential = await auth.SignInWithEmailAndPasswordAsync(
            _configuration.GetValue<string>("fireBdata:fireUser"),
            _configuration.GetValue<string>("fireBdata:fireClave"));
        return (auth, await credential.User.GetIdTokenAsync());
    }

    private FirebaseStorage Storage(string token) => new(
        _configuration.GetValue<string>("fireBdata:fireStorage"),
        new FirebaseStorageOptions { AuthTokenAsyncFactory = () => Task.FromResult(token), ThrowOnCancel = true });

    private string Protect(TemporaryPayload payload)
    {
        string data = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        using HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(_configuration["fireBdata:fireClave"] ?? string.Empty));
        string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
        return $"{data}.{signature}";
    }

    private TemporaryPayload Unprotect(string token)
    {
        try
        {
            string[] parts = (token ?? string.Empty).Split('.', 2);
            if (parts.Length != 2) throw new InvalidOperationException();
            using HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(_configuration["fireBdata:fireClave"] ?? string.Empty));
            byte[] expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(parts[0]));
            byte[] actual = Convert.FromBase64String(parts[1]);
            if (!CryptographicOperations.FixedTimeEquals(expected, actual)) throw new InvalidOperationException();
            TemporaryPayload? payload = JsonSerializer.Deserialize<TemporaryPayload>(Encoding.UTF8.GetString(Convert.FromBase64String(parts[0])));
            if (payload == null || payload.ExpiresUtc < DateTime.UtcNow) throw new InvalidOperationException();
            return payload;
        }
        catch { throw Error("IMAGEN_TOKEN_INVALIDO", "La imagen temporal expiró o no es válida."); }
    }

    private static void ValidateSignature(string extension, byte[] bytes)
    {
        if (bytes.Length < 12) throw Error("IMAGEN_FIRMA_INVALIDA", "La imagen cargada no contiene una firma válida.");
        bool jpeg = bytes[0] == 0xFF && bytes[1] == 0xD8;
        bool png = bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
        bool webp = Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP";
        if (extension is ".jpg" or ".jpeg" ? !jpeg : extension == ".png" ? !png : extension == ".webp" ? !webp : true)
            throw Error("IMAGEN_FIRMA_INVALIDA", "La firma del archivo no corresponde al formato indicado.");
    }

    private static string NormalizeExtension(string? extension, string mime) => (extension ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => ".jpg", ".png" => ".png", ".webp" => ".webp",
        _ => mime == "image/png" ? ".png" : mime == "image/webp" ? ".webp" : ".jpg"
    };
    private static string SafeCompany(string value) => string.Concat((value ?? string.Empty).Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')).Trim();
    private static string CleanName(string name) { string value = Path.GetFileName(name ?? string.Empty).Trim(); return value.Length > 255 ? value[..255] : value; }
    private static KitsDomainException Error(string code, string message) => new(code, message);
    private sealed record TemporaryPayload(string CompanyKey, string Folder, string StoredName, string OriginalName, string MimeType, long Length, DateTime ExpiresUtc);
}

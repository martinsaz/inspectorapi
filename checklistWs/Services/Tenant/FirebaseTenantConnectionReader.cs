using System.Collections.Concurrent;
using checklistWs.Models.Firebase;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using Firebase.Database.Query;

namespace checklistWs.Services.Tenant
{
    public sealed class FirebaseTenantConnectionReader : ITenantConnectionReader, ITenantCatalogReader
    {
        private static readonly TimeSpan ConnectionCacheTtl = TimeSpan.FromMinutes(5);
        private static readonly ConcurrentDictionary<string, CacheEntry> ConnectionCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> ConnectionLocks = new(StringComparer.OrdinalIgnoreCase);
        private readonly IConfiguration _configuration;

        public FirebaseTenantConnectionReader(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<TenantConnectionDescriptor?> GetConnectionAsync(string empresaKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(empresaKey))
            {
                return null;
            }

            string normalizedEmpresaKey = empresaKey.Trim().ToUpperInvariant();
            if (TryGetCachedConnection(normalizedEmpresaKey, out TenantConnectionDescriptor? cached))
            {
                return cached;
            }

            SemaphoreSlim gate = ConnectionLocks.GetOrAdd(normalizedEmpresaKey, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (TryGetCachedConnection(normalizedEmpresaKey, out cached))
                {
                    return cached;
                }

                TenantConnectionDescriptor? connection = await ReadConnectionFromFirebaseAsync(normalizedEmpresaKey, cancellationToken);
                if (connection != null)
                {
                    ConnectionCache[normalizedEmpresaKey] = new CacheEntry(Clone(connection), DateTimeOffset.UtcNow.Add(ConnectionCacheTtl));
                }

                return Clone(connection);
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task<TenantConnectionDescriptor?> ReadConnectionFromFirebaseAsync(string empresaKey, CancellationToken cancellationToken)
        {
            FirebaseAuthClient authClient = CreateAuthClient();
            global::Firebase.Auth.UserCredential credential = await authClient.SignInWithEmailAndPasswordAsync(
                _configuration.GetValue<string>("fireBdata:fireUser"),
                _configuration.GetValue<string>("fireBdata:fireClave"));

            try
            {
                FirebaseClient firebaseClient = new FirebaseClient(
                    _configuration.GetValue<string>("fireBdata:fireDatabaseUrl"),
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(credential.User.Credential.IdToken)
                    });

                FireBconn? connection = await firebaseClient
                    .Child("Conexiones")
                    .Child(empresaKey.Trim().ToUpperInvariant())
                    .OnceSingleAsync<FireBconn>();

                cancellationToken.ThrowIfCancellationRequested();

                if (connection == null)
                {
                    return null;
                }

                return new TenantConnectionDescriptor
                {
                    EmpresaKey = empresaKey,
                    IdEmpresa = Guid.TryParse(connection.IdEmpresa, out Guid parsedIdEmpresa) ? parsedIdEmpresa : Guid.Empty,
                    Status = connection.Status?.Trim() ?? string.Empty,
                    ConnectionString = connection.Cadena?.Trim() ?? string.Empty
                };
            }
            finally
            {
                authClient.SignOut();
            }
        }

        public async Task<IReadOnlyCollection<TenantConnectionDescriptor>> GetConnectionsAsync(CancellationToken cancellationToken = default)
        {
            FirebaseAuthClient authClient = CreateAuthClient();
            global::Firebase.Auth.UserCredential credential = await authClient.SignInWithEmailAndPasswordAsync(
                _configuration.GetValue<string>("fireBdata:fireUser"),
                _configuration.GetValue<string>("fireBdata:fireClave"));

            try
            {
                FirebaseClient firebaseClient = new FirebaseClient(
                    _configuration.GetValue<string>("fireBdata:fireDatabaseUrl"),
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(credential.User.Credential.IdToken)
                    });

                IReadOnlyCollection<FirebaseObject<FireBconn>> connections = await firebaseClient
                    .Child("Conexiones")
                    .OnceAsync<FireBconn>();

                cancellationToken.ThrowIfCancellationRequested();

                return connections
                    .Where(item => item.Object != null)
                    .Select(item =>
                    {
                        var descriptor = new TenantConnectionDescriptor
                        {
                            EmpresaKey = item.Key.Trim().ToUpperInvariant(),
                            IdEmpresa = Guid.TryParse(item.Object.IdEmpresa, out Guid parsedIdEmpresa) ? parsedIdEmpresa : Guid.Empty,
                            Status = item.Object.Status?.Trim() ?? string.Empty,
                            ConnectionString = item.Object.Cadena?.Trim() ?? string.Empty
                        };
                        if (!string.IsNullOrWhiteSpace(descriptor.EmpresaKey))
                        {
                            ConnectionCache[descriptor.EmpresaKey] = new CacheEntry(Clone(descriptor), DateTimeOffset.UtcNow.Add(ConnectionCacheTtl));
                        }

                        return descriptor;
                    })
                    .ToArray();
            }
            finally
            {
                authClient.SignOut();
            }
        }

        private FirebaseAuthClient CreateAuthClient()
        {
            return new FirebaseAuthClient(new FirebaseAuthConfig
            {
                ApiKey = _configuration.GetValue<string>("fireBdata:fireApiKey"),
                AuthDomain = _configuration.GetValue<string>("fireBdata:fireAuthDomain"),
                Providers = new FirebaseAuthProvider[] { new EmailProvider() }
            });
        }

        private static bool TryGetCachedConnection(string empresaKey, out TenantConnectionDescriptor? descriptor)
        {
            descriptor = null;
            if (!ConnectionCache.TryGetValue(empresaKey, out CacheEntry? entry))
            {
                return false;
            }

            if (entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                ConnectionCache.TryRemove(empresaKey, out _);
                return false;
            }

            descriptor = Clone(entry.Descriptor);
            return true;
        }

        private static TenantConnectionDescriptor? Clone(TenantConnectionDescriptor? descriptor)
        {
            if (descriptor == null)
            {
                return null;
            }

            return new TenantConnectionDescriptor
            {
                EmpresaKey = descriptor.EmpresaKey,
                IdEmpresa = descriptor.IdEmpresa,
                Status = descriptor.Status,
                ConnectionString = descriptor.ConnectionString
            };
        }

        private sealed record CacheEntry(TenantConnectionDescriptor Descriptor, DateTimeOffset ExpiresAtUtc);
    }
}

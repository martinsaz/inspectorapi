using checklistWs.Models.Firebase;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using Firebase.Database.Query;

namespace checklistWs.Services.Tenant
{
    public sealed class FirebaseTenantConnectionReader : ITenantConnectionReader, ITenantCatalogReader
    {
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
                    EmpresaKey = empresaKey.Trim().ToUpperInvariant(),
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
                    .Select(item => new TenantConnectionDescriptor
                    {
                        EmpresaKey = item.Key.Trim().ToUpperInvariant(),
                        IdEmpresa = Guid.TryParse(item.Object.IdEmpresa, out Guid parsedIdEmpresa) ? parsedIdEmpresa : Guid.Empty,
                        Status = item.Object.Status?.Trim() ?? string.Empty,
                        ConnectionString = item.Object.Cadena?.Trim() ?? string.Empty
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
    }
}
